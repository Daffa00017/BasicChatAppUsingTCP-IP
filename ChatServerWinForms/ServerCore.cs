//ServerCore
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServerWinForms
{
    public class ServerCore
    {
        private TcpListener listener;
        private CancellationTokenSource cts;
        private readonly ConcurrentDictionary<string, TcpClient> clients = new ConcurrentDictionary<string, TcpClient>();

        public event Action<string> OnLog;
        public event Action<string[]> OnClientListChanged;

        // shared Random for id generation (lock to avoid same-seed issues)
        private static readonly Random _rnd = new Random();
        private static readonly object _rndLock = new object();

        public void Start(int port)
        {
            cts = new CancellationTokenSource();
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            OnLog?.Invoke($"Server started on port {port}");
            Task.Run(() => AcceptLoop(cts.Token));
        }

        public void Stop()
        {
            try
            {
                cts?.Cancel();
                listener?.Stop();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Stop error: {ex.Message}");
            }

            foreach (var kv in clients)
            {
                try { kv.Value.Close(); } catch { }
            }
            clients.Clear();
            OnClientListChanged?.Invoke(Array.Empty<string>());

            OnLog?.Invoke("Server stopped");
        }

        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    OnLog?.Invoke($"Incoming connection from {tcpClient.Client.RemoteEndPoint}");
                    // Start handler; handler will register client after reading join line
                    _ = Task.Run(() => HandleClient(tcpClient, token));
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"Accept error: {ex.Message}");
                }
            }
        }

        private async Task HandleClient(TcpClient tcpClient, CancellationToken token)
        {
            string clientId = null;
            try
            {
                var stream = tcpClient.GetStream();
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    // read first line expecting join: "__JOIN__:username"
                    string firstLine = await reader.ReadLineAsync().ConfigureAwait(false);
                    string username = null;
                    if (firstLine != null && firstLine.StartsWith("__JOIN__:", StringComparison.Ordinal))
                    {
                        username = firstLine.Substring("__JOIN__:".Length).Trim();
                        if (string.IsNullOrWhiteSpace(username)) username = "Guest";
                    }
                    else
                    {
                        // no proper join, fallback to Guest
                        username = "Guest";
                        // If firstLine is actual chat text, we'll treat it as first chat message after registration.
                    }

                    // generate human-friendly clientId based on username
                    clientId = GenerateUserIdFromName(username);

                    if (!clients.TryAdd(clientId, tcpClient))
                    {
                        // If collision (unlikely), append suffix until success
                        int tries = 1;
                        string baseId = clientId;
                        while (!clients.TryAdd(clientId, tcpClient))
                        {
                            clientId = baseId + tries.ToString();
                            tries++;
                        }
                    }

                    OnLog?.Invoke($"User connected: {clientId} (name='{username}')");
                    OnClientListChanged?.Invoke(clients.Keys.ToArray());

                    // If firstLine wasn't a join (i.e., it was a chat), handle it as first chat
                    if (firstLine != null && !firstLine.StartsWith("__JOIN__:", StringComparison.Ordinal))
                    {
                        OnLog?.Invoke($"[{clientId}] {firstLine}");
                        Broadcast($"{clientId}: {firstLine}");
                    }

                    // continue reading the rest of messages
                    while (!token.IsCancellationRequested && tcpClient.Connected)
                    {
                        string line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null) break;

                        OnLog?.Invoke($"[{clientId}] {line}");
                        Broadcast($"{clientId}: {line}");
                    }
                }
            }
            catch (IOException)
            {
                // connection closed by client
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"HandleClient error ({clientId ?? "unknown"}): {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(clientId))
                {
                    TcpClient removed;
                    clients.TryRemove(clientId, out removed);
                }
                try { tcpClient.Close(); } catch { }
                OnLog?.Invoke($"Client {clientId ?? "unknown"} disconnected");
                OnClientListChanged?.Invoke(clients.Keys.ToArray());
            }
        }

        private void Broadcast(string message)
        {
            foreach (var kv in clients)
            {
                try
                {
                    var writer = new StreamWriter(kv.Value.GetStream(), Encoding.UTF8) { AutoFlush = true };
                    writer.WriteLine(message);
                }
                catch { }
            }
        }

        // generate id from name: shuffle letters + "-" + 4-char suffix (letters+digits)
        private static string GenerateUserIdFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "User";

            // Normalize name: remove spaces and keep alphanumerics
            var cleaned = new string(name.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "User";

            char[] chars = cleaned.ToCharArray();

            // shuffle chars using shared Random
            lock (_rndLock)
            {
                for (int i = chars.Length - 1; i > 0; i--)
                {
                    int j = _rnd.Next(i + 1);
                    var tmp = chars[i];
                    chars[i] = chars[j];
                    chars[j] = tmp;
                }
            }

            // build suffix 4 chars (A-Z,a-z,0-9)
            const string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] suffix = new char[4];
            lock (_rndLock)
            {
                for (int i = 0; i < suffix.Length; i++)
                {
                    suffix[i] = pool[_rnd.Next(pool.Length)];
                }
            }

            return new string(chars) + "-" + new string(suffix);
        }
    }
}
