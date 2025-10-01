using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatClientWinForms
{
    public class ClientCore
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;

        public event Action<string> OnLog;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string[]> OnClientListChanged;
        public event Action<string, bool> OnTypingState; // (username, isTyping)


        public string CurrentUsername { get; private set; }

        public ClientCore()
        {
            _tcpClient = new TcpClient();
        }

        public async Task<bool> ConnectAsync(string host, int port, string username)
        {
            try
            {
                await _tcpClient.ConnectAsync(host, port);
                _stream = _tcpClient.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                // Kirimkan perintah JOIN untuk bergabung dengan server
                await _writer.WriteLineAsync($"__JOIN__:{username}");

                // Simpan username
                CurrentUsername = username;

                OnLog?.Invoke("Connected to server.");
                OnConnected?.Invoke();

                // Mulai mendengarkan server
                _ = Task.Run(() => ListenForMessages());

                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Failed to connect: {ex.Message}");
                return false;
            }
        }

        public async Task Disconnect()
        {
            try
            {
                await _writer.WriteLineAsync("DISCONNECT");
                _tcpClient.Close();
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Failed to disconnect: {ex.Message}");
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (_tcpClient.Connected)
            {
                await _writer.WriteLineAsync(message);
            }
        }

        public async Task SendPrivateMessage(string targetUsername, string message)
        {
            if (_tcpClient.Connected)
            {
                string currentTime = DateTime.Now.ToString("HH:mm:ss");  // Get current time
                string privateMessage = $"[{currentTime}] /w {targetUsername} {message}";
                await _writer.WriteLineAsync(privateMessage);
            }
        }

        private async Task ListenForMessages()
        {
            try
            {
                while (_tcpClient.Connected)
                {
                    string message = await _reader.ReadLineAsync();
                    if (message == null) break;
                    if (message.StartsWith("[SYS] TYPING "))
                    {
                        // Example: [HH:mm:ss] [SYS] TYPING Alice on
                        // Strip optional timestamp prefix if present
                        string m = message;
                        // Optional TS prefix: [HH:mm:ss] [SYS] ...
                        if (m.StartsWith("[") && m.Length > 10)
                        {
                            int firstClose = m.IndexOf(']');
                            if (firstClose >= 0 && firstClose + 2 < m.Length && m[firstClose + 2] == '[')
                            {
                                // remove leading "[HH:mm:ss] "
                                m = m.Substring(firstClose + 2);
                            }
                        }

                        // Now m like: [SYS] TYPING Alice on
                        var parts = m.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        // parts: ["[SYS]", "TYPING", "<username>", "on|off"]
                        if (parts.Length >= 4)
                        {
                            string uname = parts[2];
                            string state = parts[3].ToLowerInvariant();
                            bool isTyping = state == "on";

                            // Raise event for UI
                            OnTypingState?.Invoke(uname, isTyping);
                        }
                        continue;
                    }

                    if (message.StartsWith("[SYS] USERS "))
                    {
                        // Update daftar pengguna
                        string userList = message.Substring(12); // Ambil daftar pengguna setelah "USERS "
                        string[] users = userList.Split(',');
                        OnClientListChanged?.Invoke(users);
                    }
                    else
                    {
                        // If it's a private message, format it with the current time
                        if (message.StartsWith("/w"))
                        {
                            string currentTime = DateTime.Now.ToString("HH:mm:ss");
                            message = $"[{currentTime}] {message}";
                        }

                        // Log the message (including time if it's a private message)
                        OnLog?.Invoke(message);
                    }
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error while listening for messages: {ex.Message}");
            }
        }
        public async Task SendTypingAsync(bool isTyping)
        {
            if (_tcpClient?.Connected == true && _writer != null && isTyping)  // Only send "on"
            {
                try
                {
                    await _writer.WriteLineAsync($"__TYPING__:on");  // Send only "on"
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"Failed to send typing state: {ex.Message}");
                }
            }
        }

    }
}
