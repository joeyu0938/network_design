using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.ComponentModel;
using System.Windows.Forms;
using Classlibary;
using System.Text.Json;
namespace WFclient
{
    public partial class Form1 : Form
    {
        byte[] byteSendingArray;
        Socket socketClient = null;
        Ball b = new Ball();
        Graphics g;
        Pen p;
        SolidBrush myBrush = new SolidBrush(System.Drawing.Color.Red);
        public Form1()
        {
            InitializeComponent();
            g = this.CreateGraphics();
            button1.Location = new Point(this.Size.Width / 2 - button1.Width / 2, this.Size.Height / 2 - button1.Height / 2);
            b.x = 50;
            b.y = 50;
            b.r = 50;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            button1.Location = new Point(this.Size.Width / 2 - button1.Width / 2, this.Size.Height / 2 - button1.Height / 2);
        }
        private Socket client;
        private void button1_Click(object sender, EventArgs e)
        {
            Send();
            byteSendingArray = new byte[10000];
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            button1.Visible = false;
            client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            client.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), ((IPEndPoint)socketClient.LocalEndPoint).Port;
            BGW_recive.RunWorkerAsync();
            g.Clear(Color.Tan);
            g.FillEllipse (myBrush, b.x, b.y, b.r, b.r);
        }
        private string message;
        private void BGW_recive_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int i = 0;
            while (true)
            {
                if (i == 100) i = 0;
                EndPoint point = new IPEndPoint(IPAddress.Any, 0);//用來儲存傳送方的ip和埠號
                byte[] buffer = new byte[1024];
                int length = client.ReceiveFrom(buffer, ref point);//接收資料報
                message = Encoding.UTF8.GetString(buffer, 0, length);
                worker.ReportProgress(i++);
            }
        }
        private void BGW_recive_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            label1.Text = message;
            string[] s = message.Split(",");
            g.Clear(Color.Tan);
            g.FillEllipse(myBrush, Convert.ToInt32(s[0]), Convert.ToInt32(s[1]), Convert.ToInt32(s[2]), Convert.ToInt32(s[2]));
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Send();
        }
        private void Send()
        {
            EndPoint point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1001);
            string jsonstring = JsonSerializer.Serialize(b);
            byteSendingArray = Encoding.UTF8.GetBytes(jsonstring);
            socketClient.SendTo(byteSendingArray, point);
        } 
    }
}