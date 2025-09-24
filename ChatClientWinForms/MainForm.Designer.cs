using System.Windows.Forms;

namespace ChatClientWinForms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.ListBox lstOnline;
        private System.Windows.Forms.ListBox lstMessages;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblOnline;
        private System.Windows.Forms.Label lblChat;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) { components.Dispose(); }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtServer = new System.Windows.Forms.TextBox();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lstOnline = new System.Windows.Forms.ListBox();
            this.lstMessages = new System.Windows.Forms.ListBox();
            this.lstMessages.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lstMessages.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lstMessages_DrawItem);
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.lblServer = new System.Windows.Forms.Label();
            this.lblPort = new System.Windows.Forms.Label();
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblOnline = new System.Windows.Forms.Label();
            this.lblChat = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // === Top row controls ===
            this.txtServer.Location = new System.Drawing.Point(80, 12);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(140, 20);
            this.txtServer.TabIndex = 0;
            this.txtServer.Text = "127.0.0.1";
            this.txtServer.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            this.txtPort.Location = new System.Drawing.Point(280, 12);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(60, 20);
            this.txtPort.TabIndex = 1;
            this.txtPort.Text = "9000";
            this.txtPort.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            this.txtUsername.Location = new System.Drawing.Point(380, 12);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(120, 20);
            this.txtUsername.TabIndex = 2;
            this.txtUsername.Text = "Me";
            this.txtUsername.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            this.btnConnect.Location = new System.Drawing.Point(520, 10);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            this.btnConnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            this.btnDisconnect.Location = new System.Drawing.Point(600, 10);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnDisconnect.TabIndex = 4;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // === Online users ===
            this.lstOnline.FormattingEnabled = true;
            this.lstOnline.Location = new System.Drawing.Point(550, 70);
            this.lstOnline.Name = "lstOnline";
            this.lstOnline.Size = new System.Drawing.Size(165, 264);
            this.lstOnline.TabIndex = 5;
            this.lstOnline.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;

            // === Messages ===
            this.lstMessages.FormattingEnabled = true;
            this.lstMessages.Location = new System.Drawing.Point(10, 70);
            this.lstMessages.Name = "lstMessages";
            this.lstMessages.Size = new System.Drawing.Size(540, 264);
            this.lstMessages.TabIndex = 6;
            this.lstMessages.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // === Message box ===
            this.txtMessage.Location = new System.Drawing.Point(10, 350);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(630, 20);
            this.txtMessage.TabIndex = 7;
            this.txtMessage.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.txtMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtMessage_KeyDown);

            // === Send button ===
            this.btnSend.Location = new System.Drawing.Point(652, 348);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(60, 23);
            this.btnSend.TabIndex = 8;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            this.btnSend.Enabled = false;
            this.btnSend.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;

            // === Labels ===
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(12, 15);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(44, 13);
            this.lblServer.Text = "Server:";
            this.lblServer.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(236, 15);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(29, 13);
            this.lblPort.Text = "Port:";
            this.lblPort.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(340, 15);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(38, 13);
            this.lblUsername.Text = "Name:";
            this.lblUsername.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            this.lblOnline.AutoSize = true;
            this.lblOnline.Location = new System.Drawing.Point(550, 50);
            this.lblOnline.Name = "lblOnline";
            this.lblOnline.Size = new System.Drawing.Size(72, 13);
            this.lblOnline.Text = "Active Users:";
            this.lblOnline.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            this.lblChat.AutoSize = true;
            this.lblChat.Location = new System.Drawing.Point(10, 50);
            this.lblChat.Name = "lblChat";
            this.lblChat.Size = new System.Drawing.Size(31, 13);
            this.lblChat.Text = "Chat:";
            this.lblChat.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // === MainForm ===
            this.ClientSize = new System.Drawing.Size(724, 385);
            this.Controls.Add(this.lblChat);
            this.Controls.Add(this.lblOnline);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.lblServer);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.lstMessages);
            this.Controls.Add(this.lstOnline);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.txtUsername);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.txtServer);
            this.Name = "MainForm";
            this.Text = "Chat Client";
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
