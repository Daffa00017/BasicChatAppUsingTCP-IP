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

namespace ChatClientWinForms
{
    public partial class MainForm : Form
    {
        private ClientCore client;
        private readonly HashSet<string> knownUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Regex senderRegex = new Regex(@"^\s*\[([^\]]+)\]\s*[:\-]\s*(.*)", RegexOptions.Compiled);

        public MainForm()
        {
            InitializeComponent();
            client = new ClientCore();
            client.OnLog += Client_OnLog;
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
        }

        private void Client_OnConnected()
        {
            // UI changes must be invoked on UI thread
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
            AddSystemMessage("Disconnected from server.");
            // clear known users if desired:
            // knownUsers.Clear(); lstOnline.Items.Clear();
        }

        private void Client_OnLog(string text)
        {
            if (InvokeRequired) { Invoke(new Action<string>(Client_OnLog), text); return; }

            // Display raw message in chat
            lstMessages.Items.Add(text);
            lstMessages.TopIndex = lstMessages.Items.Count - 1;

            // try extract sender id like "[id]: message" -> add to known users
            var m = senderRegex.Match(text);
            if (m.Success)
            {
                var sender = m.Groups[1].Value.Trim();
                if (!knownUsers.Contains(sender))
                {
                    knownUsers.Add(sender);
                    lstOnline.Items.Add(sender);
                }
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            string host = txtServer.Text.Trim();
            int port = 9000;
            int.TryParse(txtPort.Text.Trim(), out port);
            string username = txtUsername.Text.Trim();
            bool ok = await client.ConnectAsync(host, port, username).ConfigureAwait(false);
            if (!ok)
            {
                if (InvokeRequired) { Invoke((Action)(() => btnConnect.Enabled = true)); }
                else btnConnect.Enabled = true;
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

            // Optionally prefix with username or command: we'll send raw text.
            // If you want the server to see username, you could send like "[Me] msg" or use JSON later.
            string username = txtUsername.Text.Trim();
            if (!string.IsNullOrEmpty(username))
            {
                // prefix so other clients can see friendly name (not enforced by server)
                client.Send($"[{username}] {text}");
                lstMessages.Items.Add($"[you] {text}");
                lstMessages.TopIndex = lstMessages.Items.Count - 1;
            }
            else
            {
                client.Send(text);
            }

            txtMessage.Clear();
        }

        private void AddSystemMessage(string msg)
        {
            lstMessages.Items.Add($"[SYS] {msg}");
            lstMessages.TopIndex = lstMessages.Items.Count - 1;
        }
    }
}
