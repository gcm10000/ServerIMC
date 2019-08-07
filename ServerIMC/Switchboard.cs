using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections;

namespace ServerIMC
{
    class SwitchBoard
    {

        private static string folder = @"C:\Soursop\ServerIM\";

        public SwitchBoard()
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        public static void DataWriter(int[] id, string archivejson)
        {
            string archive = id[0] + "-" + id[1];
            string path = folder + archive +".json";

            
            using (StreamWriter streamWriter = new StreamWriter(path, true, System.Text.Encoding.UTF8))
            {
                streamWriter.WriteLine(archivejson);
                streamWriter.Flush();
            }
        }

        public static string[] DataReader(string archivejson)
        {
            string path = folder + archivejson + ".json";

            if (!File.Exists(path))
                return new string[0];

            ArrayList lines = new ArrayList();
            using (StreamReader streamReader = new StreamReader(path, System.Text.Encoding.UTF8, true))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();
                    Chat chat = JsonConvert.DeserializeObject<Chat>(line);
                    if(chat.IsSend != true)
                    {
                        chat.IsSend = true;
                        string newline = JsonConvert.SerializeObject(chat);
                        lines.Add(newline);
                    }
                }
            }

            //Update object bool IsSend to true
            foreach (string line in lines)
            {
                using (StreamWriter streamWriter = new StreamWriter(path, false, System.Text.Encoding.UTF8))
                {
                    streamWriter.WriteLine(line);
                    streamWriter.Flush();
                }
            }
            return lines.ToArray(typeof(string)) as string[];
        }
    }
    class Chat
    {
        private string fromEmail;
        private string toEmail;
        private ServerSocket.Command command;
        private string message;
        private DateTime date;
        private bool isSend;

        public string FromEmail { get => fromEmail; set => fromEmail = value; }
        public string ToEmail { get => toEmail; set => toEmail = value; }
        public string Message { get => message; set => message = value; }
        public ServerSocket.Command Command { get => command; set => command = value; }
        public DateTime Date { get => date; set => date = value; }
        public bool IsSend { get => isSend; set => isSend = value; }
    }
}
