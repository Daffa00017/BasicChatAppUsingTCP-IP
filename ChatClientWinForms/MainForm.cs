using System;
using System.Collections.Generic;
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
        private ClientCore client;
        // --- Typing indicator state ---
        private readonly System.Windows.Forms.Timer _typingUiTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _sendTypingCooldownTimer = new System.Windows.Forms.Timer();
        private readonly Dictionary<string, DateTime> _whoIsTyping = new Dictionary<string, DateTime>();
        private DateTime _lastTypingSent = DateTime.MinValue;
        private bool _lastSentWasOn = false;
        private readonly TimeSpan _typingStaleAfter = TimeSpan.FromSeconds(4);   // UI clears after 4s
        private readonly TimeSpan _typingSendCooldown = TimeSpan.FromSeconds(1); // throttle network spam
        private System.Windows.Forms.Label lblTyping;
        private readonly System.Windows.Forms.Timer _typingUpdateTimer = new System.Windows.Forms.Timer();
        private string _selfName = "";


        public MainForm()
        {
            InitializeComponent();
            // Set the initial theme
            ApplyTheme();
            // init client & event
            _client = new ClientCore();
            _client.OnLog += Client_OnLog;
            _client.OnConnected += Client_OnConnected;
            _client.OnDisconnected += Client_OnDisconnected;
            _client.OnClientListChanged += Client_OnClientListChanged;

            // If lblTyping doesn’t exist in Designer, create it:
            if (this.Controls.Find("lblTyping", true).FirstOrDefault() is not Label)
            {
                var lbl = new Label
                {
                    Name = "lblTyping",
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Visible = true,
                    Text = string.Empty
                };
                // put it near your txtMessage; adjust layout as needed
                // example: dock at bottom above send row
                lbl.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
                lbl.Location = new Point(txtMessage.Left, txtMessage.Top - 18);
                this.Controls.Add(lbl);
            }

            // timers
            _typingUiTimer.Interval = 500; // check twice a second to clear stale
            _typingUiTimer.Tick += (s, e) => RefreshTypingUi();
            _typingUiTimer.Start();  // Start the timer
            _sendTypingCooldownTimer.Interval = (int)_typingSendCooldown.TotalMilliseconds;
            _sendTypingCooldownTimer.Tick += async (s, e) =>
            {
                _sendTypingCooldownTimer.Stop();
                // cooldown finished; next keystroke can send again
                await Task.CompletedTask;
            };

            // hook client typing event
            _client.OnTypingState += Client_OnTypingState;

        }

        // dark mode

        private bool _isDarkMode = false;  // Flag to track the current mode

        private void btnDarkMode_Click(object sender, EventArgs e)
        {
            // Toggle dark mode
            _isDarkMode = !_isDarkMode;
            ApplyTheme();
        }
        private void ApplyTheme()
        {
            if (_isDarkMode)
            {
                // Dark mode settings
                this.BackColor = Color.FromArgb(30, 30, 30);  // Dark background color
                this.ForeColor = Color.White;  // White text color

                // Update the background color of controls
                lstMessages.BackColor = Color.FromArgb(40, 40, 40);  // Dark listbox background
                lstMessages.ForeColor = Color.White;  // White text in messages

                lstOnline.BackColor = Color.FromArgb(40, 40, 40);  // Dark listbox background
                lstOnline.ForeColor = Color.White;  // White text in online list

                txtMessage.BackColor = Color.FromArgb(40, 40, 40);  // Dark input box background
                txtMessage.ForeColor = Color.White;  // White text

                // Set the button styles for dark mode
                btnConnect.BackColor = Color.FromArgb(50, 50, 50);
                btnDisconnect.BackColor = Color.FromArgb(50, 50, 50);
                btnSend.BackColor = Color.FromArgb(50, 50, 50);
                btnDarkMode.BackColor = Color.FromArgb(50, 50, 50);
                btnConnect.ForeColor = Color.White;
                btnDisconnect.ForeColor = Color.White;
                btnSend.ForeColor = Color.White;
                btnDarkMode.ForeColor = Color.White;

                lblServer.ForeColor = Color.White;
                lblPort.ForeColor = Color.White;
                lblUsername.ForeColor = Color.White;
                lblOnline.ForeColor = Color.White;
                lblChat.ForeColor = Color.White;

                // Adjust label for typing indicator
                lblTyping.ForeColor = Color.Red;
            }
            else
            {
                // Light mode settings (default)
                this.BackColor = Color.White;
                this.ForeColor = Color.Black;

                // Update the background color of controls
                lstMessages.BackColor = Color.White;
                lstMessages.ForeColor = Color.Black;

                lstOnline.BackColor = Color.White;
                lstOnline.ForeColor = Color.Black;

                txtMessage.BackColor = Color.White;
                txtMessage.ForeColor = Color.Black;

                // Set the button styles for light mode
                btnConnect.BackColor = Color.FromArgb(240, 240, 240);
                btnDisconnect.BackColor = Color.FromArgb(240, 240, 240);
                btnSend.BackColor = Color.FromArgb(240, 240, 240);
                btnDarkMode.BackColor = Color.FromArgb(240, 240, 240);
                btnConnect.ForeColor = Color.Black;
                btnDisconnect.ForeColor = Color.Black;
                btnSend.ForeColor = Color.Black;
                btnDarkMode.ForeColor = Color.Black;

                lblServer.ForeColor = Color.Black;
                lblPort.ForeColor = Color.Black;
                lblUsername.ForeColor = Color.Black;
                lblOnline.ForeColor = Color.Black;
                lblChat.ForeColor = Color.Black;

                // Adjust label for typing indicator
                lblTyping.ForeColor = Color.Red;
            }
        }


        // ===== Handlers dari ClientCore =====

        private void Client_OnConnected()
        {
            if (InvokeRequired) { Invoke((Action)Client_OnConnected); return; }

            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            btnSend.Enabled = true;
            AddSystemMessage("Connected to server.");

            _typingUiTimer.Start();

        }

        private void Client_OnDisconnected()
        {
            if (InvokeRequired) { Invoke((Action)Client_OnDisconnected); return; }

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnSend.Enabled = false;

            lstOnline.Items.Clear();
            AddSystemMessage("Disconnected from server.");

            _typingUiTimer.Stop();
            _whoIsTyping.Clear();
            UpdateTypingLabel();
            _lastSentWasOn = false;

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

            // purge typing entries for users no longer online
            var online = new HashSet<string>(users ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var staleKeys = _whoIsTyping.Keys.Where(k => !online.Contains(k)).ToList();
            foreach (var k in staleKeys) _whoIsTyping.Remove(k);
            UpdateTypingLabel();
        }

        private void Client_OnTypingState(string username, bool isTyping)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, bool>(Client_OnTypingState), username, isTyping);
                return;
            }

            if (!string.IsNullOrEmpty(_client?.CurrentUsername) && username.Equals(_client.CurrentUsername, StringComparison.OrdinalIgnoreCase))
                return;

            // Update the dictionary with the typing state (true if typing, false if not)
            if (isTyping)
            {
                _whoIsTyping[username] = DateTime.UtcNow;
            }
            else
            {
                _whoIsTyping.Remove(username);
            }

            // Trigger the update on the label after a small delay
            _typingUpdateTimer.Stop();
            _typingUpdateTimer.Start(); // Restart timer to delay the update
        }







        // ===== UI events =====

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            string host = txtServer.Text.Trim();   // IP address of the server
            int port = 9000;
            int.TryParse(txtPort.Text.Trim(), out port);  // Port to connect to
            string username = txtUsername.Text.Trim();

            _selfName = username;

            bool ok = await client.ConnectAsync(host, port, username).ConfigureAwait(false);
            if (!ok)
            {
                if (InvokeRequired) { Invoke((Action)(() => btnConnect.Enabled = true)); }
                else btnConnect.Enabled = true;
                _selfName = "";
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
            // we just sent; immediately tell others I’m no longer typing
            await _client.SendTypingAsync(false);
            _lastSentWasOn = false;

        }

        // ===== Owner draw untuk ListBox pesan =====

        private void lstMessages_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            // Get the current brush based on dark/light mode
            Brush textBrush = _isDarkMode ? Brushes.White : Brushes.Black;

            // handle two possibilities: ChatItem / string
            if (!(lstMessages.Items[e.Index] is ChatItem it))
            {
                string s = lstMessages.Items[e.Index]?.ToString() ?? "";
                e.Graphics.DrawString(s, e.Font, textBrush, e.Bounds.Location);
                e.DrawFocusRectangle();
                return;
            }

            // colors for the rest of the drawing
            Brush brSys = Brushes.DarkOrange;
            Brush brName = it.IsMe ? Brushes.Green : Brushes.DarkBlue;
            Brush brBody = textBrush;  // Use the dynamic text color based on the theme
            Brush brSep = textBrush;

            float x = e.Bounds.Left + 2, y = e.Bounds.Top + 1;

            void Draw(string t, Brush b)
            {
                if (string.IsNullOrEmpty(t)) return;
                e.Graphics.DrawString(t, e.Font, b, x, y);
                x += e.Graphics.MeasureString(t, e.Font).Width;
            }

            if (!string.IsNullOrEmpty(it.Time)) Draw($"[{it.Time}] ", brSys);
            Draw($"[{it.Tag}]", it.IsSys ? brSys : brName);
            Draw(" ", brSep);
            if (it.IsMe) Draw("[you] ", brName);
            Draw(": ", brSep);
            Draw(it.Body, brBody);

            e.DrawFocusRectangle();
        }


        private void RefreshTypingUi()
        {
            var now = DateTime.UtcNow;
            var stale = _whoIsTyping.Where(kv => now - kv.Value > _typingStaleAfter)
                                    .Select(kv => kv.Key)
                                    .ToList();

            foreach (var k in stale)
            {
                _whoIsTyping.Remove(k);  // Remove users who have stopped typing for too long
            }

            UpdateTypingLabel();  // Update the label to reflect the latest typing status
        }



        /*private void UpdateTypingLabel()
        {
            // Find the lblTyping label on the form
            var lbl = this.Controls.Find("lblTyping", true).FirstOrDefault() as Label;
            if (lbl == null) return;  // If the label doesn't exist, return early

            // For testing: Display a fixed message
            lbl.Visible = true;
            lbl.Text = "Test: Typing indicator updated!";  // Directly set the text to test if the label updates
        }*/

        private void UpdateTypingLabel()
        {
            var lbl = this.Controls.Find("lblTyping", true).FirstOrDefault() as Label;
            if (lbl == null) return;

            lbl.Visible = true;

            if (_whoIsTyping.Count == 0)
            {
                lbl.Text = "";  // No one is typing
                return;
            }

            var names = _whoIsTyping.Keys.OrderBy(s => s).ToList();
            string text = names.Count switch
            {
                1 => $"{names[0]} is typing...",
                2 => $"{names[0]} and {names[1]} are typing...",
                _ => $"{names[0]}, {names[1]} and {names.Count - 2} others are typing..."
            };

            // Ensure the label text gets updated
            lbl.Text = text;

            // Debug log to check what's happening
            Console.WriteLine($"Updated label with text: {text}");
        }





        private async void txtMessage_TextChanged(object sender, EventArgs e)
        {
            bool wantTypingOn = !string.IsNullOrWhiteSpace(txtMessage.Text);

            // Send typing "on" only when user starts typing
            if (wantTypingOn)
            {
                if (!_lastSentWasOn || (DateTime.UtcNow - _lastTypingSent) >= _typingSendCooldown)
                {
                    await _client.SendTypingAsync(true);  // Notify server that typing has started
                    _lastTypingSent = DateTime.UtcNow;  // Track when we last sent the typing state
                    _lastSentWasOn = true;  // Mark that we are typing
                    _sendTypingCooldownTimer.Stop();
                    _sendTypingCooldownTimer.Start();  // Restart the cooldown timer
                }
            }
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
