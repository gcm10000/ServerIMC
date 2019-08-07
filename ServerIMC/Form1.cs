using System;
using System.Windows.Forms;

namespace ServerIMC
{
    public partial class Form1 : Form
    {
        ServerSocket serverSocket;

        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            serverSocket = new ServerSocket(comboBox1.Text, textBox1.Text);
            serverSocket.StatusEventHandler += new ServerSocket.StatusHandler(ServerSocket_StatusEventHandler);
            serverSocket.StartLog();
            serverSocket.InitializeDataBase();
        }

        private delegate void RichTextBoxCallBack(string text);

        private void AppendText(string text)
        {
            this.richTextBox1.AppendText(text);
        }

        private void ServerSocket_StatusEventHandler(object sender, StatusEventArgs e)
        {
            if (richTextBox1.InvokeRequired)
            {
                RichTextBoxCallBack d = new RichTextBoxCallBack(AppendText);
                this.richTextBox1.Invoke(d, new object[] { e.Status });
            }
            else
                richTextBox1.AppendText(e.Status);
        }
    }
}
