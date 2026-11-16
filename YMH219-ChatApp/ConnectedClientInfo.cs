using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace YMH219_ChatApp
{
    /// <summary>
    /// Sunucuya bağlı olan her bir istemcinin oturum ve ağ bilgilerini tutan sınıf.
    /// </summary>
    public class ConnectedClientInfo
    {
        public string ClientId { get; }
        public string Username { get; set; }
        public string RemoteEndPoint { get; }
        public DateTime ConnectedAt { get; }
        public TcpClient Client { get; }
        public StreamWriter Writer { get; }

        public ConnectedClientInfo(TcpClient client, string username)
        {
            ClientId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            Client = client;
            Username = string.IsNullOrWhiteSpace(username) ? $"Kullanici_{ClientId[..4]}" : username;
            RemoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Bilinmiyor";
            ConnectedAt = DateTime.Now;

            var stream = client.GetStream();
            Writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        }
    }
}