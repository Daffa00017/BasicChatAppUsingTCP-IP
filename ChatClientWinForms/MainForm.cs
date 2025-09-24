//ChatClientWinForms.MainForm

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace ChatClientWinForms
{
    public partial class MainForm : Form
    {
        private ClientCore client;
        private readonly HashSet<string> knownUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Regex senderRegex = new Regex(@"^\s*\[([^\]]+)\]\s*:?\s*(.*)$", RegexOptions.Compiled);
        private string _selfName = "";
        public MainForm()
        {
            InitializeComponent();

            // === aktifkan logger ke file ===
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener("debug.log"));
            Trace.AutoFlush = true;

            client = new ClientCore();
            client.OnLog += Client_OnLog;
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
        }

        private void DebugLine(string s)
        {
            lstMessages.Items.Add("[DEBUG] " + s);
            lstMessages.TopIndex = lstMessages.Items.Count - 1;
        }

        private static string NormalizeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = Regex.Replace(s, @"\s+", " ");
            s = s.Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "").Replace("\uFEFF", "");
            return s.Trim().ToLowerInvariant();
        }


        private void Client_OnConnected()
        {
            if (InvokeRequired) { Invoke((Action)Client_OnConnected); return; }
            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            btnSend.Enabled = true;
            AddSystemMessage("Connected to server.");

            // ⬇️ TAMBAHKAN BARIS INI
            AddSystemMessage("ME = '" + client.CurrentUsername + "'");
        }

        private void Client_OnDisconnected()
        {
            if (InvokeRequired) { Invoke((Action)Client_OnDisconnected); return; }
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnSend.Enabled = false;
            _selfName = ""; // <-- reset
            AddSystemMessage("Disconnected from server.");
        }

        private void Client_OnLog(string text)
        {
            if (InvokeRequired) { Invoke(new Action<string>(Client_OnLog), text); return; }
            if (string.IsNullOrEmpty(text)) return;

            // --- buang BOM & whitespace di depan ---
            text = text.TrimStart('\uFEFF', '\u200B', '\u200C', '\u200D', ' ', '\t', '\r', '\n');

            // Pesan sistem: tampilkan apa adanya
            if (text.StartsWith("[SYS]"))
            {
                // strip prefix "[SYS]" + spasi
                string payload = text.Length > 5 ? text.Substring(5).Trim() : "";

                if (payload.StartsWith("USERS ", StringComparison.OrdinalIgnoreCase))
                {
                    // USERS name1,name2,...
                    string csv = payload.Substring("USERS ".Length);
                    var names = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim());
                    ReplaceOnlineUsers(names);
                    // opsional: tampilkan satu baris info
                    lstMessages.Items.Add("[SYS] Users synced");
                }
                else if (payload.StartsWith("JOIN ", StringComparison.OrdinalIgnoreCase))
                {
                    string name = payload.Substring("JOIN ".Length).Trim();
                    if (!string.IsNullOrEmpty(name) && knownUsers.Add(name))
                        lstOnline.Items.Add(name);
                    lstMessages.Items.Add("[SYS] " + name + " joined");
                }
                else if (payload.StartsWith("LEAVE ", StringComparison.OrdinalIgnoreCase))
                {
                    string name = payload.Substring("LEAVE ".Length).Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        // remove from set & listbox
                        if (knownUsers.Remove(name))
                        {
                            for (int i = 0; i < lstOnline.Items.Count; i++)
                            {
                                if (string.Equals(lstOnline.Items[i].ToString(), name, StringComparison.OrdinalIgnoreCase))
                                {
                                    lstOnline.Items.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                    lstMessages.Items.Add("[SYS] " + name + " left");
                }
                else
                {
                    // fallback sys message
                    lstMessages.Items.Add(text);
                }

                lstMessages.TopIndex = lstMessages.Items.Count - 1;
                return;
            }

            // === PARSER MANUAL: cari '[' pertama & ']' sesudahnya ===
            int open = text.IndexOf('[');
            int close = (open >= 0) ? text.IndexOf(']', open + 1) : -1;

            if (open == 0 && close > open + 1)
            {
                string sender = text.Substring(open + 1, close - open - 1).Trim();

                string body = (close + 1 < text.Length) ? text.Substring(close + 1) : "";
                // buang spasi/titik dua di depan body
                int i = 0;
                while (i < body.Length && (body[i] == ' ' || body[i] == ':')) i++;
                if (i > 0) body = body.Substring(i);

                bool fromMe = NormalizeName(sender) == NormalizeName(client.CurrentUsername);

                //DebugLine($"RAW='{text}' | SENDER='{sender}' | ME='{client.CurrentUsername}' | fromMe={fromMe}");

                string line = fromMe ? $"[{sender}] [you] : {body}"
                                     : $"[{sender}] : {body}";
                lstMessages.Items.Add(line);
                lstMessages.TopIndex = lstMessages.Items.Count - 1;

                if (!knownUsers.Contains(sender))
                {
                    knownUsers.Add(sender);
                    lstOnline.Items.Add(sender);
                }
                return;
            }

            // Fallback: format tak dikenal (debugkan juga biar keliatan)
            DebugLine($"RAW(no-parse)='{text}'");
            lstMessages.Items.Add(text);
            lstMessages.TopIndex = lstMessages.Items.Count - 1;
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            string host = txtServer.Text.Trim();
            int port = 9000;
            int.TryParse(txtPort.Text.Trim(), out port);
            string username = txtUsername.Text.Trim();

            _selfName = username; // <-- T A M B A H K A N  I N I

            bool ok = await client.ConnectAsync(host, port, username).ConfigureAwait(false);
            if (!ok)
            {
                if (InvokeRequired) { Invoke((Action)(() => btnConnect.Enabled = true)); }
                else btnConnect.Enabled = true;
                _selfName = ""; // rollback kalau gagal
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            client.Disconnect();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessageFromBox();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendMessageFromBox();
            }
        }

        private void SendMessageFromBox()
        {
            var text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            client.Send(text);      // kirim RAW ke server (tanpa prefix)
            txtMessage.Clear();     // TIDAK menambah "[you]" di sini
        }

        private void AddSystemMessage(string msg)
        {
            lstMessages.Items.Add($"[SYS] {msg}");
            lstMessages.TopIndex = lstMessages.Items.Count - 1;
        }
        private void ReplaceOnlineUsers(IEnumerable<string> names)
        {
            knownUsers.Clear();
            lstOnline.Items.Clear();
            foreach (var n in names)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                var nn = n.Trim();
                if (knownUsers.Add(nn)) lstOnline.Items.Add(nn);
            }
        }
    }
}
