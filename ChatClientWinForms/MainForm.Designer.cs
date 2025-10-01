using System.Windows.Forms;

namespace ChatClientWinForms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox txtServer;
        private TextBox txtPort;
        private TextBox txtUsername;
        private Button btnConnect;
        private Button btnDisconnect;
        private ListBox lstOnline;
        private ListBox lstMessages;
        private TextBox txtMessage;
        private Button btnSend;
        private Label lblServer;
        private Label lblPort;
        private Label lblUsername;
        private Label lblOnline;
        private Label lblChat;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
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
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.lblServer = new System.Windows.Forms.Label();
            this.lblPort = new System.Windows.Forms.Label();
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblOnline = new System.Windows.Forms.Label();
            this.lblChat = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtServer
            // 
            this.txtServer.Location = new System.Drawing.Point(63, 11);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(140, 20);
            this.txtServer.TabIndex = 0;
            this.txtServer.Text = "127.0.0.1";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(241, 11);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(60, 20);
            this.txtPort.TabIndex = 1;
            this.txtPort.Text = "9000";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(345, 11);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(130, 20);
            this.txtUsername.TabIndex = 2;
            this.txtUsername.Text = "Me";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(496, 9);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(577, 9);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnDisconnect.TabIndex = 4;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // lstOnline
            // 
            this.lstOnline.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lstOnline.FormattingEnabled = true;
            this.lstOnline.Location = new System.Drawing.Point(538, 62);
            this.lstOnline.Name = "lstOnline";
            this.lstOnline.Size = new System.Drawing.Size(174, 264);
            this.lstOnline.TabIndex = 8;
            // 
            // lstMessages
            // 
            this.lstMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lstMessages.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lstMessages.FormattingEnabled = true;
            this.lstMessages.IntegralHeight = false;
            this.lstMessages.ItemHeight = 16;
            this.lstMessages.Location = new System.Drawing.Point(12, 62);
            this.lstMessages.Name = "lstMessages";
            this.lstMessages.Size = new System.Drawing.Size(520, 264);
            this.lstMessages.TabIndex = 7;
            this.lstMessages.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lstMessages_DrawItem);
            // 
            // txtMessage
            // 
            this.txtMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMessage.Location = new System.Drawing.Point(12, 337);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(620, 20);
            this.txtMessage.TabIndex = 9;
            this.txtMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtMessage_KeyDown);
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Enabled = false;
            this.btnSend.Location = new System.Drawing.Point(638, 335);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(74, 23);
            this.btnSend.TabIndex = 10;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(13, 14);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(41, 13);
            this.lblServer.TabIndex = 11;
            this.lblServer.Text = "Server:";
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(211, 14);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(29, 13);
            this.lblPort.TabIndex = 12;
            this.lblPort.Text = "Port:";
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(309, 14);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(38, 13);
            this.lblUsername.TabIndex = 13;
            this.lblUsername.Text = "Name:";
            // 
            // lblOnline
            // 
            this.lblOnline.AutoSize = true;
            this.lblOnline.Location = new System.Drawing.Point(535, 46);
            this.lblOnline.Name = "lblOnline";
            this.lblOnline.Size = new System.Drawing.Size(69, 13);
            this.lblOnline.TabIndex = 14;
            this.lblOnline.Text = "Active Users:";
            // 
            // lblChat
            // 
            this.lblChat.AutoSize = true;
            this.lblChat.Location = new System.Drawing.Point(12, 46);
            this.lblChat.Name = "lblChat";
            this.lblChat.Size = new System.Drawing.Size(32, 13);
            this.lblChat.TabIndex = 15;
            this.lblChat.Text = "Chat:";
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(724, 371);
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
            this.MinimumSize = new System.Drawing.Size(640, 360);
            this.Name = "MainForm";
            this.Text = "Chat Client";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
