using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ContainelDll
{
    public class Container
    {
        /// <summary>
        /// 数据事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void NewLpnDelegate(object sender, NewLpnEventArgs e);        
        public event NewLpnDelegate NewLpnEvent;
        public delegate void UpdateLpnDelegate(object sender, UpdateLpnEventArgs e);
        public event UpdateLpnDelegate UpdateLpnEvent;
        public delegate void ConNumDelegate(object sender, ConNumEventArgs e);
        public event ConNumDelegate ConNumEvent;

        public NewLpnEventArgs NewLpnArgs = new NewLpnEventArgs();
        public UpdateLpnEventArgs UpdateLpnArgs = new UpdateLpnEventArgs();
        public ConNumEventArgs ConNumArgs = new ConNumEventArgs();

        /// <summary>
        /// 处理信息委托
        /// </summary>
        public Action<string> MessageAction = null;

        /// <summary>
        /// 定时重连
        /// </summary>
        private System.Threading.Timer _Timer = null;
#pragma warning disable IDE0044 // 添加只读修饰符
        private IPEndPoint IPE = null;
#pragma warning restore IDE0044 // 添加只读修饰符
        private Socket Client = null;

        /// <summary>
        /// 初始化自动连接
        /// </summary>
        public Container(string Ip, int Port)
        {
            IPE = new IPEndPoint(IPAddress.Parse(Ip), Port);
            _Timer = new System.Threading.Timer(AsyncConect2server, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// 异步链接服务器
        /// </summary>
        public void AsyncConect2server(object state)
        {
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult ar = Client.BeginConnect(IPE, new AsyncCallback(ConnectCallBack), Client);
            MessageAction?.Invoke("start link to server");
            ar.AsyncWaitHandle.WaitOne();
        }

        /// <summary>
        /// 链接回调
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectCallBack(IAsyncResult ar)
        {
            try
            {
                Client = (Socket)ar.AsyncState;
                Client.EndConnect(ar);
                AsyncReceive(Client);
                _Timer.Change(-1, -1);//停止定时器
                MessageAction?.Invoke(string.Format("Connect server：{0} \r\n", Client.RemoteEndPoint.ToString()));
            }
            catch (SocketException ex)
            {
                Client.Close();
                MessageAction?.Invoke(string.Format("An error occurred when attempting to access the socket：{0}\r\n", ex.ToString()));
            }
            catch (ObjectDisposedException ex)
            {
                Client.Close();
                MessageAction?.Invoke(string.Format("The Socket has been closed：{0}\r\n", ex.ToString()));
            }
        }

        public static int SIZE = 4096;
        public byte[] buffer = new byte[SIZE];

        /// <summary>
        /// 异步接收数据
        /// </summary>
        /// <param name="Client"></param>
        public void AsyncReceive(Socket Client)
        {
            try
            {
                Client.BeginReceive(buffer, 0, Container.SIZE, 0, new AsyncCallback(ReceiveCallBack), Client);
            }
            catch (Exception ex)
            {
                Client.Close();
                MessageAction?.Invoke(string.Format("link error：{0}\r\n", ex.ToString()));
            }
        }

        /// <summary>
        /// 异步接收回调
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                Client = (Socket)ar.AsyncState;
                int DataSize = Client.EndReceive(ar);
                string str = System.Text.Encoding.ASCII.GetString(buffer, 0, DataSize).Trim();

                while (str.Length > 10)//循环处理所有接收到的数据数据
                {
                    if (str.StartsWith("[C") || str.StartsWith("[U") || str.StartsWith("[N"))//判断 【箱号|重车牌|空车牌】 结果
                    {
                        int index = str.IndexOf("]") + 1;//截取符合数据量，索引和实际数量差一
                        string tmpData = str.Substring(0, index);
                        str = str.Remove(0, index);

                        MessageAction?.Invoke(string.Format("Get Date：{0}", tmpData));
                        SplitData(tmpData);//分割数据
                    }
                    else//删除第一位，重新校验
                    {
                        str = str.Remove(0, 1);
                    }
                }

                if (DataSize > 0)//收到数据,循环接收数据。
                {
                    Client.BeginReceive(buffer, 0, Container.SIZE, 0, new AsyncCallback(ReceiveCallBack), Client);
                }
                else
                {
                    Client.Close();
                    _Timer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                    MessageAction?.Invoke("link of close \r\n");
                }
            }
            catch (Exception ex)
            {
                Client.Close();
                _Timer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                MessageAction?.Invoke(ex.ToString());
            }
        }

        /// <summary>
        /// 分割数据
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public void SplitData(string str)
        {
            string tmp = string.Empty;
            string[] tmpString = str.Split('|');
            tmpString[tmpString.Length - 1] = tmpString[tmpString.Length - 1].Split(']')[0];
            if (tmpString[0] == "[C")
            {
                ContainerNum(tmpString);
            }
            else if (tmpString[0] == "[U")
            {
                UpdateLpn(tmpString);
            }
            else if (tmpString[0] == "[N")
            {
                NewLpn(tmpString);
            }
            else
            {
                ;//预留
            }
        }

        //public Dictionary<string, string> dict = new Dictionary<string, string>();

        /// <summary>
        /// 空车车牌
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public void NewLpn(string[] str)
        {
            //dict["TriggerTime"] = str[1];
            //dict["LaneNum"] = str[2];
            //dict["Lpn"] = str[3];
            //dict["Color"] = str[4];

            if(NewLpnEvent!=null)
            {
                NewLpnArgs.TriggerTime = DateTime.ParseExact(str[1], "yyyyMMddHHmmss",System.Globalization.CultureInfo.CurrentCulture);
                NewLpnArgs.LaneNum = int.Parse(str[2]);
                NewLpnArgs.Lpn = str[3];
                NewLpnArgs.Color = int.Parse(str[4]);
                NewLpnEvent(this, NewLpnArgs);//触发空车牌事件
            }
            //string jsonStr = JsonConvert.SerializeObject(dict);
            //return jsonStr;            
        }

        /// <summary>
        /// 重车车牌
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public void UpdateLpn(string[] str)
        {
            //dict["TriggerTime"] = str[1];
            //dict["LaneNum"] = str[2];
            //dict["Lpn"] = str[3];
            //dict["Color"] = str[4];

            if (UpdateLpnEvent != null)
            {
                UpdateLpnArgs.TriggerTime = DateTime.ParseExact(str[1], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                UpdateLpnArgs.LaneNum = int.Parse(str[2]);
                UpdateLpnArgs.Lpn = str[3];
                UpdateLpnArgs.Color = int.Parse(str[4]);
                UpdateLpnEvent(this, UpdateLpnArgs);//触发重车牌事件
            }
            //string jsonStr = JsonConvert.SerializeObject(dict);
            //return jsonStr;
        }

        /// <summary>
        /// 集装箱
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public void ContainerNum(string[] str)
        {
            //dict["TriggerTime"] = str[1];
            //dict["LaneNum"] = str[2];
            //dict["ContainerType"] = str[3];
            //dict["ContainerNum1"] = str[4];
            //dict["CheckNum1"] = str[5];
            //if (str.Length == 7)//单箱
            //{
            //    dict["ISO1"] = str[6];
            //}
            //else//双箱==9
            //{
            //    dict["ContainerNum2"] = str[6];
            //    dict["CheckNum2"] = str[7];
            //    dict["ISO1"] = str[8];
            //    dict["ISO2"] = str[9];
            //}

            if (ConNumEvent != null)
            {
                ConNumArgs.TriggerTime = DateTime.ParseExact(str[1], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                ConNumArgs.LaneNum = int.Parse(str[2]);
                ConNumArgs.ContainerType = int.Parse(str[3]);
                ConNumArgs.ContainerNum1 = str[4];
                ConNumArgs.CheckNum1 = str[5];
                if (str.Length == 7)//单箱
                {
                    ConNumArgs.ISO1 = str[6];
                }
                else//双箱==9
                {
                    ConNumArgs.ContainerNum2 = str[6];
                    ConNumArgs.CheckNum2 = str[7];
                    ConNumArgs.ISO1 = str[8];
                    ConNumArgs.ISO2 = str[9];
                }
                ConNumEvent(this, ConNumArgs);//触发箱号事件
            }
            //string jsonStr = JsonConvert.SerializeObject(dict);
            //return jsonStr;
        }
    }
}

//        class DATA
//        {
//            public static int SIZE = 4096;
//            public static byte[] buffer = new byte[SIZE];
//            public static Dictionary<string, string> dict = new Dictionary<string, string>();


//            /// <summary>
//            /// 分解数据
//            /// </summary>
//            /// <param name="str"></param>
//            public static string SplitData(string str)
//            {
//                string tmp = string.Empty;
//                string[] tmpString = str.Split('|');
//                tmpString[tmpString.Length - 1] = tmpString[tmpString.Length - 1].Split(']')[0];
//                if (tmpString[0] == "[C")
//                {
//                    tmp = ContainerNum(tmpString)["ContainerNum1"];
//                }
//                else if (tmpString[0] == "[U")
//                {
//                    tmp = UpdateLpn(tmpString)["Lpn"];
//                }
//                else if (tmpString[0] == "[N")
//                {
//                    tmp = NewLpn(tmpString)["Lpn"];
//                }
//                else
//                {
//                    ;//预留
//                }
//                return tmp;
//            }


//            /// <summary>
//            /// 空车车牌
//            /// </summary>
//            /// <param name="str"></param>
//            /// <returns></returns>
//            public static Dictionary<string, string> NewLpn(string[] str)
//            {
//                dict["TriggerTime"] = str[1];
//                dict["LaneNum"] = str[2];
//                dict["Lpn"] = str[3];
//                dict["Color"] = str[4];

//                return dict;
//                //string jsonStr = JsonConvert.SerializeObject(dict);
//                //return jsonStr;            
//            }

//            /// <summary>
//            /// 重车车牌
//            /// </summary>
//            /// <param name="str"></param>
//            /// <returns></returns>
//            public static Dictionary<string, string> UpdateLpn(string[] str)
//            {
//                dict["TriggerTime"] = str[1];
//                dict["LaneNum"] = str[2];
//                dict["Lpn"] = str[3];
//                dict["Color"] = str[4];

//                return dict;
//                //string jsonStr = JsonConvert.SerializeObject(dict);
//                //return jsonStr;
//            }

//            /// <summary>
//            /// 集装箱
//            /// </summary>
//            /// <param name="str"></param>
//            /// <returns></returns>
//            public static Dictionary<string, string> ContainerNum(string[] str)
//            {
//                dict["TriggerTime"] = str[1];
//                dict["LaneNum"] = str[2];
//                dict["ContainerType"] = str[3];
//                dict["ContainerNum1"] = str[4];
//                dict["CheckNum1"] = str[5];
//                if (str.Length == 7)//单箱
//                {
//                    dict["ISO1"] = str[6];
//                }
//                else//双箱==9
//                {
//                    dict["ContainerNum2"] = str[6];
//                    dict["CheckNum2"] = str[7];
//                    dict["ISO1"] = str[8];
//                    dict["ISO2"] = str[9];
//                }

//                return dict;
//                //string jsonStr = JsonConvert.SerializeObject(dict);
//                //return jsonStr;
//            }
//        }
//    }
//}
