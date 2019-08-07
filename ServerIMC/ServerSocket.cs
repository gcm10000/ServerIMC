using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerIMC
{
    partial class ServerSocket
    {
        private Socket server;
        private EndPoint ep;
        private SqlConnection connection;
        private SqlCommand cmd;
        private List<Credentials> ListCredentials;
        private SwitchBoard switchboard;

        private byte[] ReceiveBuffer;
        private string stringEvent;

        public delegate void StatusHandler(object sender, StatusEventArgs e);
        public event StatusHandler StatusEventHandler;

        public string StringEvent
        {
            get => stringEvent;
            private set
            {
                if (value != StringEvent)
                {
                    stringEvent = value;
                    StatusEventHandler?.Invoke(this, new StatusEventArgs(stringEvent));
                }
            }
        }

        public enum Command
        {
            Invalid = 190,
            OK = 200,
            RequestFriendship = 210,
            AcceptFriendship = 220,
            RecusedFriendship = 230,
            Maintenance = 500
        }

        public ServerSocket(string ip, string port)
        {
            InitilizeServer(ip, port);
        }

        public void InitializeDataBase()
        {
            //Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename="C:\Users\Gabriel\Documents\Visual Studio 2015\Projects\ServerIMC\ServerIMC\db.mdf";Integrated Security=True;Connect Timeout=30
            string strcon = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Gabriel\Documents\Visual Studio 2015\Projects\ServerIMC\ServerIMC\db.mdf;Integrated Security=True;Connect Timeout=30;MultipleActiveResultSets=True";
            connection = new SqlConnection(strcon);
            connection.Open();
            StringEvent = "Banco de dados aberto.\n";

            string sql = String.Format(@"UPDATE Notification SET IsLogged = 0");
            cmd = new SqlCommand(sql, connection);
            SqlDataReader dr = cmd.ExecuteReader();
        }

        private bool IsSignUpDataBase(string json, Socket client, out Credentials OutCredentials)
        {
            Credentials credentials = JsonConvert.DeserializeObject<Credentials>(json);
            string sql = String.Format(@"SELECT Id, Email, Name, Password, Date, Picture, FriendList FROM Notification WHERE Email = '{0}' AND Password = '{1}' ORDER BY ID", credentials.Email, credentials.Password);
            cmd = new SqlCommand(sql, connection);
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                credentials.Id = Convert.ToInt32(dr["Id"].ToString());
                credentials.Email = dr["Email"].ToString();
                credentials.Name = dr["Name"].ToString();
                credentials.Password = dr["Password"].ToString();
                credentials.Date = Convert.ToDateTime(dr["Date"].ToString());
                credentials.Picture = dr["Picture"].ToString();
                credentials.FriendList = dr["FriendList"].ToString();
                credentials.Client = client;

                sql = String.Format(@"UPDATE Notification SET IsLogged = 1 WHERE Id = {0}", credentials.Id);
                cmd = new SqlCommand(sql, connection);
                dr = cmd.ExecuteReader();

                ListCredentials.Add(credentials);
                OutCredentials = credentials;
                return true;
            }
            OutCredentials = null;
            return false;
        }

        private void InitilizeServer(string ip, string port)
        {
            try
            {
                ReceiveBuffer = new byte[1024];

                ListCredentials = new List<Credentials>();
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                switchboard = new SwitchBoard();
                IPAddress ipAddress = new IPAddress(0);

                if (ip.Contains("Todos os Não Atribuídos"))
                {
                    ep = new IPEndPoint(IPAddress.None, Convert.ToInt32(port));
                }
                else if (IPAddress.TryParse(ip, out ipAddress))
                {
                    ep = new IPEndPoint(ipAddress, Convert.ToInt32(port));
                }

                server.Bind(ep);
                server.Listen(10);
                server.BeginAccept(new AsyncCallback(NewClient), server);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "InitializeServer()");
            }
        }

        public void StartLog()
        {
            StringEvent = "Servidor aberto.\n";
            StringEvent = "Esperando um cliente...\n";
        }

        private void NewClient(IAsyncResult AR)
        {
            Socket client = server.EndAccept(AR);

            try
            {
                byte[] buffer = new byte[1024];
                int recv = client.Receive(buffer, SocketFlags.None);
                string json = Encoding.UTF8.GetString(buffer,  0, recv);

                Credentials NewCredentials;
                if (IsSignUpDataBase(json, client, out NewCredentials))
                {
                    client.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), client);
                    SendCommand(Command.OK, client);
                    MessagesNews(NewCredentials);
                    StringEvent = "Um cliente foi conectado.\n";
                }
                else
                {
                    SendCommand(Command.Invalid, client);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "NewClient()");
            }
            finally
            {
                server.BeginAccept(new AsyncCallback(NewClient), null);
            }
        }

        private void ReceiveCallBack(IAsyncResult AR)
        {
            Socket client = AR.AsyncState as Socket;

            try
            {
                int recv = client.EndReceive(AR);
                byte[] data = new byte[recv];
                Array.Copy(ReceiveBuffer, data, recv);
                StringEvent = Encoding.UTF8.GetString(data);
                AddFriend(data, client);
                client.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), client);
            }
            catch (SocketException)
            {
                StringEvent = "Um cliente foi desconectado.\n";

                for (int i = 0; i < ListCredentials.Count; i++)
                {
                    if (ListCredentials[i].Client == client)
                    {
                        string sql = String.Format(@"UPDATE Notification SET IsLogged = 0 WHERE Id = {0}", ListCredentials[i].Id);
                        cmd = new SqlCommand(sql, connection);
                        SqlDataReader dr = cmd.ExecuteReader();
                        ListCredentials.Remove(ListCredentials[i]);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "ReceiveASync()");
            }
        }

        private void AddFriend(byte[] data, Socket client)
        {
            Friendship friendShip = JsonConvert.DeserializeObject<Friendship>(Encoding.UTF8.GetString(data));

            string[] email = new string[2];
            int[] id = new int[2];

            for (int i = 0; i < ListCredentials.Count; i++)
            {
                if (ListCredentials[i].Client == client)
                {
                    email[0] = ListCredentials[i].Email;
                    id[0] = ListCredentials[i].Id;
                    break;
                }
            }
            string sql = String.Format(@"SELECT Id FROM Notification WHERE Email = '{0}'", friendShip.Email);
            cmd = new SqlCommand(sql, connection);
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                email[1] = dr["Email"].ToString();
                id[1] = Convert.ToInt32(dr["Id"].ToString());
            }

            Chat chat = new Chat()
            {
                FromEmail = email[0],
                ToEmail = email[1],
                Command = Command.RequestFriendship,
                Date = DateTime.Now,
                Message = string.Empty
            };

            sql = String.Format("UPDATE Notification SET Chat += '{0}-{1}, ' WHERE Id IN ({0}, {1})", id[0], id[1]);
            cmd = new SqlCommand(sql, connection);
            dr = cmd.ExecuteReader();

            SwitchBoard.DataWriter(id, JsonConvert.SerializeObject(chat));
        }

        //Messages offline
        private void MessagesNews(Credentials credentials)
        {
            string sql = String.Format(@"SELECT Chat FROM Notification WHERE Id = '{0}'", credentials.Id);
            cmd = new SqlCommand(sql, connection);
            SqlDataReader dr = cmd.ExecuteReader();

            string resultreader = string.Empty;
            if (dr.Read())
                resultreader = dr["Chat"].ToString();

            string[] chat_split = resultreader.Split(new[] { ", " }, StringSplitOptions.None);
            foreach (var archive_json in chat_split)
            {
                string[] messages_offline = SwitchBoard.DataReader(archive_json);
                foreach (var msg in messages_offline)
                {
                    credentials.Client.BeginSend(Encoding.UTF8.GetBytes(msg), 0, msg.Length, SocketFlags.None, new AsyncCallback(SendCallBack), credentials.Client);
                }
            }

        }

        private void SendCallBack(IAsyncResult AR)
        {
            Socket client = AR.AsyncState as Socket;
            try
            {
                client.EndSend(AR);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "SendCallBack()");
            }
        }

        private void SendCommand(Command command, Socket socket)
        {
            try
            {
                byte[] cmd = Encoding.UTF8.GetBytes(((int)command).ToString());
                socket.BeginSend(cmd, 0, cmd.Length, SocketFlags.None, new AsyncCallback(SendCallBack), socket);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "SendCommand()");
            }
        }
    }

    class StatusEventArgs : EventArgs
    {
        public String Status;
        public StatusEventArgs(string Status)
        {
            this.Status = Status;
        }
    }
}