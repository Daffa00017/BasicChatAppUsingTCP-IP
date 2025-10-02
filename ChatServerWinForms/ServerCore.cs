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

                // Registrasi ke server
                clientId = GenerateUserIdFromName(username);
                ClientInfo info = new ClientInfo(clientId, username, tcp);

                if (!_clients.TryAdd(clientId, info))
                {
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

                // Kirim daftar user penuh ke client baru
                BroadcastActiveUsers();  // Kirimkan daftar pengguna terbaru ke semua klien yang terhubung

                // Umumkan ke semua: ada yang join
                BroadcastSystem(username + " has joined");

                // === Loop baca chat ===
                while (!token.IsCancellationRequested && tcp.Connected)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break; // disconnect
                    // Typing signal from client: "__TYPING__:on" or "__TYPING__:off"
                    if (line.StartsWith("__TYPING__:", StringComparison.Ordinal))
                    {
                        string state = line.Substring("__TYPING__:".Length).Trim().ToLowerInvariant();
                        bool isTyping = state == "on";
                        BroadcastTyping(username, isTyping);
                        continue;
                    }

                    // Tangani pesan pribadi jika ada
                    if (line.StartsWith("/w"))
                    {
                        HandlePrivateMessage(line, username);
                    }
                    else
                    {
                        BroadcastChat(info, line);
                    }
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
                        BroadcastSystem("User | " + removed.Username + " | Is Leaving");   // Kirim LEAVE hanya saat disconnect
                        removed.CloseQuietly();
                    }
                }
                else
                {
                    try { tcp.Close(); } catch { }
                }

                SafeLog("Client " + (clientId ?? "unknown") + " disconnected");
                FireClientListChanged();

                // Setelah ada client yang keluar, kirim pembaruan daftar pengguna
                BroadcastActiveUsers();  // Kirimkan pembaruan daftar pengguna ke semua klien
            }
        }

        private void BroadcastChat(ClientInfo fromClient, string text)
        {
            string ts = DateTime.Now.ToString("HH:mm:ss");
            string line = $"[{ts}] [{fromClient.Username}#{fromClient.Id}] {text}";
            foreach (var ci in _clients.Values)
            {
                try
                {
                    var w = new StreamWriter(ci.Stream, Utf8NoBom) { AutoFlush = true };
                    w.WriteLine(line);
                }
                catch { }
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

        private void HandlePrivateMessage(string message, string fromUsername)
        {
            var parts = message.Split('-');
            if (parts.Length < 3)
            {
                SendSystemToClient(fromUsername, "Format salah, gunakan format: '/pm [username] [message]'");
                return;
            }

            string targetUsername = parts[1];
            string privateMessage = parts[2];

            // Ambil waktu sekarang
            string currentTime = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{currentTime}] {fromUsername}: {privateMessage}";


            // Mencari klien berdasarkan username
            ClientInfo targetClient = _clients.Values.FirstOrDefault(c => c.Username.Equals(targetUsername, StringComparison.OrdinalIgnoreCase));

            if (targetClient != null)
            {
                // Jika klien ditemukan, kirim pesan pribadi ke penerima
                SendPrivateMessage(fromUsername, targetClient, privateMessage);
            }
            else
            {
                // Jika klien tidak ditemukan, beri tahu pengirim
                SendSystemToClient(fromUsername, $"Pengguna {targetUsername} tidak ditemukan.");
            }
        }

        private void SendPrivateMessage(string fromUsername, ClientInfo targetClient, string message)
        {
            string currentTime = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{currentTime}] [Whispering {targetClient.Username}] {fromUsername}: {message}";

            SendSystem(targetClient, formattedMessage);  // Kirim ke penerima (targetClient) yang benar
            SendSystemToClient(fromUsername, formattedMessage);  // Kirim pesan pribadi ke pengirim
        }

        private void SendSystemToClient(string username, string text)
        {
            var client = _clients.Values.FirstOrDefault(c => c.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (client != null)
            {
                SendSystem(client, text);
            }
        }

        // Fungsi untuk mengirim daftar pengguna aktif ke semua klien
        private void BroadcastActiveUsers()
        {
            // Ambil daftar pengguna terbaru
            string usersCsv = string.Join(",", _clients.Values.Select(c => c.Username));

            // Kirimkan daftar pengguna terbaru ke semua klien
            foreach (var client in _clients.Values)
            {
                SendSystem(client, "USERS " + usersCsv);  // Kirim daftar pengguna terbaru
            }
        }
        private void BroadcastTyping(string username, bool isTyping)
        {
            if (!isTyping) return;  // Skip broadcasting "off" events

            // Only broadcast when the user is typing
            string ts = DateTime.Now.ToString("HH:mm:ss");
            string line = $"[{ts}] [SYS] TYPING {username} on";  // Keep only "on"
            foreach (var ci in _clients.Values)
            {
                try
                {
                    var w = new StreamWriter(ci.Stream, Utf8NoBom) { AutoFlush = true };
                    w.WriteLine(line);
                }
                catch { }
            }
            SafeLog(line);
        }

    }
}
