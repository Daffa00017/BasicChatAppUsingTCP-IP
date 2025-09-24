// ChatServerWinForms/ServerCore.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServerWinForms
{
    public sealed class ServerCore
    {
        public event Action<string> OnLog;
        public event Action<string[]> OnClientListChanged;

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private TcpListener _listener;
        private CancellationTokenSource _cts;
        public sealed class ClientInfo
        {
            public string Id { get; private set; }          
            public string Username { get; private set; }    
            public TcpClient Client { get; private set; }

            public NetworkStream Stream { get { return Client.GetStream(); } }

            public ClientInfo(string id, string username, TcpClient client)
            {
                Id = id;
                Username = username;
                Client = client;
            }

            public void CloseQuietly()
            {
                try { Client.Close(); } catch { }
            }
        }

        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new ConcurrentDictionary<string, ClientInfo>();

        private static readonly Random _rnd = new Random();
        private static readonly object _rndLock = new object();

        public void Start(int port)
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            SafeLog("Server started on port " + port);
            Task.Run(() => AcceptLoop(_cts.Token));
        }

        public void Stop()
        {
            try
            {
                if (_cts != null) _cts.Cancel();
                if (_listener != null) _listener.Stop();
            }
            catch (Exception ex)
            {
                SafeLog("Stop error: " + ex.Message);
            }

            foreach (KeyValuePair<string, ClientInfo> kv in _clients)
            {
                try { kv.Value.CloseQuietly(); } catch { }
            }
            _clients.Clear();
            FireClientListChanged();

            SafeLog("Server stopped");
        }

        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcp = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    SafeLog("Incoming connection from " + tcp.Client.RemoteEndPoint);
                    _ = Task.Run(() => HandleClient(tcp, token));
                }
                catch (ObjectDisposedException)
                {
                    break; // listener ditutup
                }
                catch (Exception ex)
                {
                    SafeLog("Accept error: " + ex.Message);
                }
            }
        }

        private async Task HandleClient(TcpClient tcp, CancellationToken token)
        {
            string clientId = null;
            string username = "Guest";

            try
            {
                NetworkStream ns = tcp.GetStream();
                StreamReader reader = new StreamReader(ns, Encoding.UTF8);

                // === Baris pertama: harapannya __JOIN__:<username> ===
                string firstLine = await reader.ReadLineAsync().ConfigureAwait(false);
                if (firstLine != null && firstLine.StartsWith("__JOIN__:", StringComparison.Ordinal))
                {
                    string parsed = firstLine.Substring("__JOIN__:".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(parsed)) username = parsed;
                }
                // else: kalau bukan JOIN, tetap daftarkan sebagai Guest; nanti firstLine (kalau ada) diperlakukan chat pertama

                // === Registrasi ke server ===
                clientId = GenerateUserIdFromName(username);
                ClientInfo info = new ClientInfo(clientId, username, tcp);

                if (!_clients.TryAdd(clientId, info))
                {
                    // antisipasi tabrakan (sangat jarang)
                    int tries = 1;
                    string baseId = clientId;
                    while (!_clients.TryAdd(clientId, info))
                    {
                        string suffix = Guid.NewGuid().ToString("N").Substring(0, 4);
                        clientId = baseId + "_" + suffix + "_" + tries.ToString();
                        info = new ClientInfo(clientId, username, tcp);
                        tries++;
                    }
                }

                SafeLog("User connected: " + username + " (id=" + clientId + ")");
                FireClientListChanged();

                // 1) kirim daftar user penuh ke client BARU
                string usersCsv = string.Join(",", _clients.Values.Select(c => c.Username));
                SendSystem(info, "USERS " + usersCsv);

                // 2) umumkan ke semua: ada yang join
                BroadcastSystem(username + " has joined "  );

                // Jika firstLine bukan JOIN tapi ada isinya, perlakukan sebagai chat pertama
                if (firstLine != null && !firstLine.StartsWith("__JOIN__:", StringComparison.Ordinal))
                {
                    BroadcastChat(username, firstLine);
                }

                // === Loop baca chat ===
                while (!token.IsCancellationRequested && tcp.Connected)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break; // disconnect

                    // (opsional) validasi command di sini (mis. /w)
                    BroadcastChat(username, line);
                }
            }
            catch (IOException)
            {
                // socket closed by client; silent
            }
            catch (Exception ex)
            {
                SafeLog("HandleClient error (" + (clientId ?? "unknown") + "): " + ex.Message);
            }
            finally
            {
                // cleanup
                if (!string.IsNullOrEmpty(clientId))
                {
                    ClientInfo removed;
                    if (_clients.TryRemove(clientId, out removed))
                    {
                        BroadcastSystem("LEAVE " + removed.Username);   // <— ganti
                        removed.CloseQuietly();
                    }
                }
                else
                {
                    try { tcp.Close(); } catch { }
                }

                SafeLog("Client " + (clientId ?? "unknown") + " disconnected");
                FireClientListChanged();
            }
        }

        private void BroadcastChat(string fromUsername, string text)
        {
            string ts = DateTime.Now.ToString("HH:mm:ss");
            string line = "[" + ts + "] [" + fromUsername + "] " + text;
            foreach (var ci in _clients.Values)
            {
                var w = new StreamWriter(ci.Stream, Utf8NoBom) { AutoFlush = true };
                w.WriteLine(line);
            }
            SafeLog(line);
        }

        private void BroadcastSystem(string text)
        {
            string ts = DateTime.Now.ToString("HH:mm:ss");
            string line = "[" + ts + "] [SYS] " + text;
            foreach (ClientInfo ci in _clients.Values)
            {
                try
                {
                    var w = new StreamWriter(ci.Stream, Utf8NoBom) { AutoFlush = true };
                    w.AutoFlush = true;
                    w.WriteLine(line);
                }
                catch { }

            }
            SafeLog(line);
        }

        private void SafeLog(string msg)
        {
            var h = OnLog;
            if (h != null) h(msg);
        }

        private void FireClientListChanged()
        {
            var h = OnClientListChanged;
            if (h != null)
            {
                string[] names = _clients.Values.Select(c => c.Username).ToArray();
                h(names);
            }
        }
        private static string GenerateUserIdFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "User";
            string cleaned = new string(name.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "User";

            char[] chars = cleaned.ToCharArray();

            lock (_rndLock)
            {
                for (int i = chars.Length - 1; i > 0; i--)
                {
                    int j = _rnd.Next(i + 1);
                    char tmp = chars[i];
                    chars[i] = chars[j];
                    chars[j] = tmp;
                }
            }

            const string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] suffix = new char[4];
            lock (_rndLock)
            {
                for (int i = 0; i < 4; i++)
                    suffix[i] = pool[_rnd.Next(pool.Length)];
            }

            return new string(chars) + "-" + new string(suffix);
        }

        private void SendSystem(ClientInfo target, string text)
        {
            try
            {
                string line = "[SYS] " + text;
                var w = new StreamWriter(target.Stream, Utf8NoBom) { AutoFlush = true };
                w.WriteLine(line);
                SafeLog(line);
            }
            catch { }
        }
    }
}
