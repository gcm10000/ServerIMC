using System;
using System.Net.Sockets;

namespace ServerIMC
{
    class Credentials
    {
        private int id;
        private string email;
        private string name;
        private string password;
        private DateTime date;
        private bool isLogged;
        private string picture;
        private string friendList;
        private Socket client;

        public int Id { get => id; set => id = value; }
        public string Email { get => email; set => email = value; }
        public string Name { get => name; set => name = value; }
        public string Password { get => password; set => password = value; }
        public DateTime Date { get => date; set => date = value; }
        public bool IsLogged { get => isLogged; set => isLogged = value; }
        public string Picture { get => picture; set => picture = value; }
        public string FriendList { get => friendList; set => friendList = value; }
        public Socket Client { get => client; set => client = value; }
    }
    class Friendship
    {
        private string email;
        public string Email { get => email; set => email = value; }
    }
}
