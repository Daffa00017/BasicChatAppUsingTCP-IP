using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using ChatServerWinForms.Core;

namespace ChatServerWinForms
{
    public partial class MainForm : Form
    {
        private ServerCore server;

        public MainForm()
        {
            InitializeComponent();
            server = new ServerCore();
            server.OnLog += msg => Invoke((Action)(() => lstLog.Items.Add(msg)));
            server.OnClientListChanged += list => Invoke((Action)(() =>
            {
                lstClients.Items.Clear();
                lstClients.Items.AddRange(list);
            }));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            server.Start(9000);
            lstLog.Items.Add("Server started on port 9000.");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            server.Stop();
            lstLog.Items.Add("Server stopped.");
        }
    }
}
