using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClientWinForms
{
    // item yang ditampilkan di ListBox chat (biar DrawItem gampang)
    internal sealed class ChatItem
    {
        public string Time { get; set; }   // "HH:mm:ss" atau ""
        public string Tag { get; set; }    // "SYS" atau nama pengirim
        public string Body { get; set; }   // isi pesan
        public bool IsMe { get; set; }     // true kalau Tag == CurrentUsername
        public bool IsSys { get; set; }    // true kalau Tag == "SYS"

        public override string ToString()
            => (string.IsNullOrEmpty(Time) ? "" : $"[{Time}] ")
               + $"[{Tag}] {Body}";
    }

    public partial class MainForm : Form
    {
        private ClientCore _client;

        public MainForm()
        {
            InitializeComponent();

            // init client & event
            _client = new ClientCore();
            _client.OnLog += Client_OnLog;
            _client.OnConnected += Client_OnConnected;
            _client.OnDisconnected += Client_OnDisconnected;
            _client.OnClientListChanged += Client_OnClientListChanged;
        }

        // ===== Handlers dari ClientCore =====

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

            lstOnline.Items.Clear();
            AddSystemMessage("Disconnected from server.");
        }

        private void Client_OnLog(string line)
        {
            if (InvokeRequired) { Invoke(new Action<string>(Client_OnLog), line); return; }
            if (string.IsNullOrWhiteSpace(line)) return;

            // server sudah memformat sebagian besar line seperti:
            // [HH:mm:ss] [SYS] text
            // atau [HH:mm:ss] [Nama] text
            ParseAndAddLine(line);
        }

        private void Client_OnClientListChanged(string[] users)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string[]>(Client_OnClientListChanged), new object[] { users });
                return;
            }

            lstOnline.BeginUpdate();
            lstOnline.Items.Clear();
            foreach (var u in users ?? Array.Empty<string>())
                if (!string.IsNullOrWhiteSpace(u))
                    lstOnline.Items.Add(u);
            lstOnline.EndUpdate();
        }

        // ===== UI events =====

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;

            string host = txtServer.Text.Trim();
            int port = 9000;
            int.TryParse(txtPort.Text.Trim(), out port);
            string username = txtUsername.Text.Trim();

            bool ok = await _client.ConnectAsync(host, port, username).ConfigureAwait(false);
            if (!ok)
            {
                // balikkan status tombol di thread UI
                if (InvokeRequired) Invoke((Action)(() => btnConnect.Enabled = true));
                else btnConnect.Enabled = true;

                AddSystemMessage("Connect failed.");
            }
        }

        private async void btnDisconnect_Click(object sender, EventArgs e)
        {
            // CS4014 selesai: menunggu operasi async
            await _client.Disconnect();
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            await SendMessageFromBox();
        }

        private async void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendMessageFromBox();
            }
        }

        // ===== Helper UI =====

        private void AddSystemMessage(string msg)
        {
            var item = new ChatItem
            {
                Time = "",
                Tag = "SYS",
                Body = msg,
                IsSys = true,
                IsMe = false
            };
            lstMessages.Items.Add(item);
            lstMessages.TopIndex = lstMessages.Items.Count - 1;
        }

        private async Task SendMessageFromBox()
        {
            var text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (text.StartsWith("/w ", StringComparison.Ordinal))
            {
                // format: /w <username> <pesan>
                var parts = text.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    AddSystemMessage("Format salah. Gunakan: /w <username> <pesan>");
                }
                else
                {
                    string target = parts[1];
                    string body = parts[2];
                    await _client.SendPrivateMessage(target, body);
                }
            }
            else
            {
                await _client.SendMessageAsync(text);
            }

            txtMessage.Clear();
        }

        // ===== Owner draw untuk ListBox pesan =====

        private void lstMessages_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            // handle dua kemungkinan: ChatItem / string
            if (!(lstMessages.Items[e.Index] is ChatItem it))
            {
                string s = lstMessages.Items[e.Index]?.ToString() ?? "";
                e.Graphics.DrawString(s, e.Font, SystemBrushes.WindowText, e.Bounds.Location);
                e.DrawFocusRectangle();
                return;
            }

            // warna
            Brush brTs = SystemBrushes.WindowText;
            Brush brSys = Brushes.DarkOrange;
            Brush brName = it.IsMe ? Brushes.Green : Brushes.DarkBlue;
            Brush brBody = SystemBrushes.WindowText;
            Brush brSep = SystemBrushes.WindowText;

            float x = e.Bounds.Left + 2, y = e.Bounds.Top + 1;

            void Draw(string t, Brush b)
            {
                if (string.IsNullOrEmpty(t)) return;
                e.Graphics.DrawString(t, e.Font, b, x, y);
                x += e.Graphics.MeasureString(t, e.Font).Width;
            }

            if (!string.IsNullOrEmpty(it.Time)) Draw($"[{it.Time}] ", brTs);
            Draw($"[{it.Tag}]", it.IsSys ? brSys : brName);
            Draw(" ", brSep);
            if (it.IsMe) Draw("[you] ", brName);
            Draw(": ", brSep);
            Draw(it.Body, brBody);

            e.DrawFocusRectangle();
        }

        // parsing ringan agar log dari server tampil rapi jadi ChatItem
        private void ParseAndAddLine(string raw)
        {
            // contoh raw: [03:14:22] [SYS] text...
            // atau       [03:14:22] [Nama] text...
            string ts = "", tag = "?", body = "";
            int p = 0;

            if (p < raw.Length && raw[p] == '[')
            {
                int q = raw.IndexOf(']', p + 1);
                if (q > p)
                {
                    string maybeTs = raw.Substring(p + 1, q - p - 1);
                    if (maybeTs.Length == 8 && maybeTs[2] == ':' && maybeTs[5] == ':')
                    {
                        ts = maybeTs;
                        p = q + 1;
                        while (p < raw.Length && raw[p] == ' ') p++;
                    }
                }
            }

            if (p < raw.Length && raw[p] == '[')
            {
                int q = raw.IndexOf(']', p + 1);
                if (q > p)
                {
                    tag = raw.Substring(p + 1, q - p - 1).Trim();
                    p = q + 1;
                    while (p < raw.Length && (raw[p] == ' ' || raw[p] == ':')) p++;
                }
            }

            body = p < raw.Length ? raw.Substring(p) : "";

            bool isSys = string.Equals(tag, "SYS", StringComparison.OrdinalIgnoreCase);
            bool isMe = !isSys &&
                        !string.IsNullOrEmpty(_client?.CurrentUsername) &&
                        string.Equals(tag, _client.CurrentUsername, StringComparison.OrdinalIgnoreCase);

            var item = new ChatItem { Time = ts, Tag = tag, Body = body, IsSys = isSys, IsMe = isMe };
            lstMessages.Items.Add(item);
            lstMessages.TopIndex = lstMessages.Items.Count - 1;
        }
    }
}
