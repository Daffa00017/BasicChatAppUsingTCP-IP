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
    class ChatItem
    {
        public string Time;        // "HH:mm:ss" or ""
        public string Tag;         // "SYS" or sender name
        public string Body;        // message text (no [you] token needed)
        public bool IsMe;        // true if Tag == CurrentUsername
        public bool IsSys;       // true if Tag == "SYS"
        public override string ToString() =>
            (string.IsNullOrEmpty(Time) ? "" : $"[{Time}] ") + $"[{Tag}] {Body}";
    }

    public partial class MainForm : Form
    {
        private ClientCore client;
        private readonly HashSet<string> knownUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Regex senderRegex = new Regex(@"^\s*\[([^\]]+)\]\s*:?\s*(.*)$", RegexOptions.Compiled);
        private string _selfName = "";


        public MainForm()
        {
            InitializeComponent();

            // === UI Colors ===
            /*this.BackColor = Color.FromArgb(30, 30, 30); // dark background
            lstMessages.BackColor = Color.Black;
            lstMessages.ForeColor = Color.DarkGray;

            lstOnline.BackColor = Color.FromArgb(40, 40, 40);
            lstOnline.ForeColor = Color.LightGreen;

            txtMessage.BackColor = Color.FromArgb(20, 20, 20);
            txtMessage.ForeColor = Color.DarkGray;

            btnSend.BackColor = Color.DodgerBlue;
            btnSend.ForeColor = Color.DarkGray;

            btnConnect.BackColor = Color.Green;
            btnConnect.ForeColor = Color.DarkGray;

            btnDisconnect.BackColor = Color.DarkRed;
            btnDisconnect.ForeColor = Color.DarkGray;

            lblServer.ForeColor = Color.DarkGray;
            lblPort.ForeColor = Color.DarkGray;
            lblUsername.ForeColor = Color.DarkGray;
            lblOnline.ForeColor = Color.DarkGray;
            lblChat.ForeColor = Color.DarkGray;*/


            // === aktifkan logger ke file ===
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener("debug.log"));
            Trace.AutoFlush = true;

            client = new ClientCore();
            client.OnLog += Client_OnLog;
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
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
        }

        private void Client_OnDisconnected()
        {
            if (InvokeRequired) { Invoke((Action)Client_OnDisconnected); return; }
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnSend.Enabled = false;
            knownUsers.Clear();
            lstOnline.Items.Clear();

            AddSystemMessage("Disconnected from server.");
        }

        private void Client_OnLog(string text)
        {
            if (InvokeRequired) { Invoke(new Action<string>(Client_OnLog), text); return; }
            if (string.IsNullOrEmpty(text)) return;

            // strip BOM/zero-width/whitespace
            text = text.TrimStart('\uFEFF', '\u200B', '\u200C', '\u200D', ' ', '\t', '\r', '\n');

            // Parse: [HH:mm:ss]? then [TAG] then body
            string ts = "", tag = "?", body = "";
            int p = 0;

            // [HH:mm:ss] optional
            if (p < text.Length && text[p] == '[')
            {
                int q = text.IndexOf(']', p + 1);
                if (q > p + 1)
                {
                    string maybeTs = text.Substring(p + 1, q - p - 1);
                    if (maybeTs.Length == 8 && maybeTs[2] == ':' && maybeTs[5] == ':')
                    { ts = maybeTs; p = q + 1; while (p < text.Length && text[p] == ' ') p++; }
                }
            }

            // [TAG] required (SYS or sender)
            if (p < text.Length && text[p] == '[')
            {
                int q = text.IndexOf(']', p + 1);
                if (q > p + 1)
                {
                    tag = text.Substring(p + 1, q - p - 1).Trim();
                    p = q + 1;
                    while (p < text.Length && (text[p] == ' ' || text[p] == ':')) p++;
                }
            }

            body = p < text.Length ? text.Substring(p) : "";

            // Handle [SYS] control messages for Active Users
            if (string.Equals(tag, "SYS", StringComparison.OrdinalIgnoreCase))
            {
                HandleSysPayload(body);                  // updates lstOnline & knownUsers
                                                         // also show the sys line in chat (as ChatItem)
                lstMessages.Items.Add(new ChatItem { Time = ts, Tag = "SYS", Body = body, IsSys = true });
                lstMessages.TopIndex = lstMessages.Items.Count - 1;
                return;
            }

            // Normal chat → create ChatItem and add
            bool isMe = string.Equals(NormalizeName(tag), NormalizeName(client.CurrentUsername), StringComparison.OrdinalIgnoreCase);
            var item = new ChatItem { Time = ts, Tag = tag, Body = body, IsMe = isMe, IsSys = false };
            lstMessages.Items.Add(item);
            lstMessages.TopIndex = lstMessages.Items.Count - 1;

            if (!string.Equals(tag, "SYS", StringComparison.OrdinalIgnoreCase) && tag != "?" && !string.IsNullOrWhiteSpace(tag))
            {
                if (!knownUsers.Contains(tag))
                {
                    knownUsers.Add(tag);
                    lstOnline.Items.Add(tag);
                }
            }

            //lstMessages.Items.Add(new ChatItem { Time = "", Tag = "SYS", Body = text, IsSys = true });
            //lstMessages.TopIndex = lstMessages.Items.Count - 1;
            return;
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

        private void lstMessages_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            // Fallback if older string items are present
            if (!(lstMessages.Items[e.Index] is ChatItem it))
            {
                string s = lstMessages.Items[e.Index]?.ToString() ?? "";
                e.Graphics.DrawString(s, e.Font, SystemBrushes.WindowText, e.Bounds.Location);
                e.DrawFocusRectangle(); return;
            }

            // Colors
            var brTs = Brushes.Black;
            var brSys = Brushes.DarkOrange;
            var brName = it.IsMe ? Brushes.Green : Brushes.DarkBlue;
            var brBody = Brushes.Purple;
            var brSep = SystemBrushes.WindowText;

            float x = e.Bounds.Left + 2, y = e.Bounds.Top + 1;
            void Draw(string t, Brush b)
            {
                if (string.IsNullOrEmpty(t)) return;
                e.Graphics.DrawString(t, e.Font, b, x, y);
                x += e.Graphics.MeasureString(t, e.Font).Width;
            }

            if (!string.IsNullOrEmpty(it.Time)) Draw("[" + it.Time + "] ", brTs);
            Draw("[" + it.Tag + "]", it.IsSys ? brSys : brName);

            // Show "[you] : " if it's me, otherwise just " : "
            Draw(" ", brSep);
            if (it.IsMe) Draw("[you] ", brName);
            Draw(": ", brSep);
            Draw(it.Body, brBody);

            e.DrawFocusRectangle();
        }


        private void HandleSysPayload(string payload)
        {
            if (payload.StartsWith("USERS ", StringComparison.OrdinalIgnoreCase))
            {
                string csv = payload.Substring("USERS ".Length);
                var names = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim());
                ReplaceOnlineUsers(names);
            }
            else if (payload.StartsWith("JOIN ", StringComparison.OrdinalIgnoreCase))
            {
                string name = payload.Substring("JOIN ".Length).Trim();
                if (!string.IsNullOrEmpty(name) && knownUsers.Add(name))
                    lstOnline.Items.Add(name);
            }
            else if (payload.StartsWith("LEAVE ", StringComparison.OrdinalIgnoreCase))
            {
                string name = payload.Substring("LEAVE ".Length).Trim();
                if (!string.IsNullOrEmpty(name))
                {
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
            }
            else
            {
            }
        }
    }
}
