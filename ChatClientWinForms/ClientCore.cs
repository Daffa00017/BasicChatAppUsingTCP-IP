//ClientCore
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ChatClientWinForms
{
    public class ClientCore
    {
        private TcpClient tcp;
        private StreamReader reader;
        private StreamWriter writer;
        private CancellationTokenSource cts;

        public bool IsConnected { get; private set; }

        // events
        public event Action<string> OnLog; // raw event for UI logs/messages
        public event Action OnDisconnected;
        public event Action OnConnected;

        public async Task<bool> ConnectAsync(string host, int port, string username)
        {
            try
            {
                tcp = new TcpClient();
                await tcp.ConnectAsync(host, port).ConfigureAwait(false);
                var stream = tcp.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };
                IsConnected = true;

                // send join line immediately
                try
                {
                    if (string.IsNullOrWhiteSpace(username)) username = "Guest";
                    writer.WriteLine("__JOIN__:" + username);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"Write join error: {ex.Message}");
                }

                cts = new CancellationTokenSource();
                _ = Task.Run(() => ListenLoop(cts.Token));

                OnConnected?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Connect error: {ex.Message}");
                DisconnectInternal();
                return false;
            }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && tcp != null && tcp.Connected)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;
                    OnLog?.Invoke(line);
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Listen error: {ex.Message}");
            }
            finally
            {
                DisconnectInternal();
                OnDisconnected?.Invoke();
            }
        }

        public void Send(string text)
        {
            if (!IsConnected) return;
            try
            {
                // send plain text newline-terminated
                writer.WriteLine(text);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Send error: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            DisconnectInternal();
            OnDisconnected?.Invoke();
        }

        private void DisconnectInternal()
        {
            IsConnected = false;
            try
            {
                if (cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                    cts = null;
                }
            }
            catch { }

            try
            {
                if (writer != null)
                {
                    writer.Close();
                    writer = null;
                }
            }
            catch { }

            try
            {
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
            }
            catch { }

            try
            {
                if (tcp != null)
                {
                    tcp.Close();
                    tcp = null;
                }
            }
            catch { }
        }
    }
}
