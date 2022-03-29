using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Classlibary;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace WinFormsApp1
{

    partial class UDPCommunication : Form1
    {   //宣告類別變數
        Dictionary<string, Ball> dicClient;//連線的客戶端集合
        bool sendingFlag = true;
        bool receiveingFlag = true;
        IPEndPoint iep_Receive = null;
        Socket socketClient = null;
        Socket socketServer = null;
        byte[] byteSendingArray = null;
        byte[] byteReceiveArray = null;
        private Form1 form;
        Thread thSending;
        Thread thReveive;
        ManualResetEvent _pause;

        //從 Form1 把 UI 控制權傳到函數裡面
        public void pause(Form1 c)//暫停 (備註:但socket會持續接收:)
        {
            form = c;
            c.listBox1.Items.Add("pause");
            _pause.Reset();
        }
        public void Resume(Form1 c) //恢復
        {
            form = c;
            c.listBox1.Items.Add("Resume");
            _pause.Set();
            OpenSendAndReceiveThread();
        }
        public void Start(Form1 u)
        {
            form = u;
            form.listBox1.Items.Add("---非同步通訊，A---");
            dicClient= new Dictionary<string, Ball>();
            OpenSendAndReceiveThread();
        }
        //從 Form1 把 UI 控制權傳到函數裡面




        /// 分別開啟“接收”與“傳送”執行緒
        private void OpenSendAndReceiveThread()
        {
            _pause = new ManualResetEvent(true); //用來插入event 操作
            thReveive = new Thread(ReceiveData);
            thReveive.Start();
        }
        private delegate void UPDATE_UI(string s); //委派函數，可以在不同執行緒上操作主緒的UI
        //private delegate void UPDATE_BALL(Ball s); 如果改共用參數衝突發生只能走委派

        //傳入:要在server印出的message
        private void AddMessage(string sMessage)
        {
            if (this.form.listBox1.InvokeRequired) // 若非同執行緒
            {
                UPDATE_UI del = new UPDATE_UI(AddMessage); //利用委派執行
                this.form.listBox1.Invoke(del, sMessage);//從操作主緒的UI
            }
            else // 同執行緒
            {
                this.form.listBox1.Items.Add(sMessage);
            }
        }
        //傳入: Ball 的參考 , ID 號碼
        //傳回false: 如果不存在用戶
        //傳回true : get 現在狀態，返回true
        private bool Get_current(ref Ball b, string id)
        {
            if (dicClient.ContainsKey(id) != true) return false;
            b = dicClient[id];//將參考的BALL狀態指定給dict中的ID
            return true;
        }


        //傳入: Ball的參數 開始傳送data
        private void SendingData(Ball entry)//Sendingdata 會在新增client 的時候自動再開一個thread
        {
            int little_balls_number = 100;//一百個小點點
            string ID = entry.s.ToString(); //初次進入thread 先將ID 取出
            Thread.Sleep(500);//休眠0.5秒
            if (sendingFlag)
            {
                byteSendingArray = new byte[10000];
                //定義網路地址
                socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//定義client 接收樣板
                sendingFlag = false;
            }
            //傳送資料
            AddMessage("傳送");
            thReveive = new Thread(ReceiveData);
            thReveive.Start();
            Balls control = new Balls();// 要處理的動作
            control.random_little_balls(little_balls_number,ref entry) ;
            while (true)
            {
                if(_pause.WaitOne(Timeout.Infinite) ==false )break;
                {
                    try
                    {
                        Ball balls = new Ball(); //要傳的資料
                        if(!Get_current(ref balls,ID))break; //如果被刪除
                        control.Ball_move(ref balls);//如果client 端要處理就不用了，如果沒有的話把 上下左右放進來Ball (u,d,l,r)
                        control.Count_collision(ref dicClient);//如果client 端要處理就不用了，我函式再改成統合狀態就好


                        //傳送的json string
                        //很重要!!!
                        string jsonstring = JsonSerializer.Serialize(balls);
                        //位元組轉換
                        byteSendingArray = Encoding.UTF8.GetBytes(jsonstring);
                        socketClient.SendTo(byteSendingArray,entry.s);//從進來的endpoint(紀錄的Ip & port)出去
                        //傳送的json string
                        //很重要!!!
                    }
                    catch
                    {
                        AddMessage(string.Format("Cannot entry :{0}",entry.s.ToString())); //server報錯
                    }
                }
            }
            socketClient.Shutdown(SocketShutdown.Both); //關掉此執行緒 接收和傳送的socket
            socketClient.Close();
        }

        //接收執行緒的方法
        private void ReceiveData() //ReceiveData 永遠只有一個 不斷接收並更新狀態(可能會有thread 的問題要接了之後才知道)
        {
            
            if (receiveingFlag)
            {
                byteReceiveArray = new byte[10000];
                iep_Receive = new IPEndPoint(IPAddress.Any, 1001);
                socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socketServer.Bind(iep_Receive); // 將endpoint 和 local 綁在一起
                receiveingFlag = false;
            }
            EndPoint ep = (EndPoint)iep_Receive;//接受收據(樣板)

            while (true)
            {
                if (_pause.WaitOne(Timeout.Infinite) == false) break;
                //接收傳來的json string
                //很重要!!!
                int intReceiveLenght = socketServer.ReceiveFrom(byteReceiveArray, ref ep);
                string strReceiveStr = Encoding.UTF8.GetString(byteReceiveArray, 0, intReceiveLenght);
                Ball receive = JsonSerializer.Deserialize<Ball>(strReceiveStr);  //反轉序列化 必須要有一樣且可序列化的class 
                receive.s = ep;
                //接收傳來的json
                //很重要!!!
                
                if (dicClient.ContainsKey(ep.ToString())!= true)//如果用戶不存在就新增
                {
                    if (receive.Dead == true) continue; //如果用戶死亡
                    dicClient.Add(ep.ToString(), receive);
                    AddMessage(string.Format("Add {0}", receive.s));
                    thSending = new Thread(()=>SendingData(receive));
                    thSending.Start();
                    continue;
                }
                dicClient[receive.s.ToString()] = receive; //更新客戶們狀態
            }

        }

    }//class_end
}