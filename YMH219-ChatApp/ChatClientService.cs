using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YMH219_ChatApp
{
    /// <summary>
    /// Sunucuya bağlanan ve asenkron mesaj gönderip alan istemci servisi sınıfı.
    /// </summary>
    public class ChatClientService
    {
        private TcpClient? _tcpClient;
        private StreamWriter? _writer;
        private CancellationTokenSource? _cts;

        public bool IsConnected => _tcpClient?.Connected ?? false;
        public string Username { get; private set; } = string.Empty;

        public event Action<ChatMessage>? OnMessageReceived;
        public event Action<string>? OnDisconnected;

        public async Task ConnectAsync(string ipAddress, int port, string username)
        {
            if (IsConnected) return;

            Username = username.Trim();
            _tcpClient = new TcpClient();
            _cts = new CancellationTokenSource();

            await _tcpClient.ConnectAsync(ipAddress, port);

            var stream = _tcpClient.GetStream();
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // Sunucuya kullanıcı adını bildir
            await _writer.WriteLineAsync($"JOIN|{Username}");

            // Gelen mesajları arka planda asenkron dinle
            _ = ReceiveLoopAsync(stream, _cts.Token);
        }

        public async Task SendMessageAsync(string content)
        {
            if (!IsConnected || _writer == null || string.IsNullOrWhiteSpace(content))
                return;

            var msg = new ChatMessage(Username, content.Trim());
            await _writer.WriteLineAsync(msg.Serialize());
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            try { _writer?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
            _tcpClient = null;
            _writer = null;
        }

        private async Task ReceiveLoopAsync(NetworkStream stream, CancellationToken token)
        {
            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                while (!token.IsCancellationRequested && IsConnected)
                {
                    string? line = await reader.ReadLineAsync(token);
                    if (line == null) break;

                    var msg = ChatMessage.Deserialize(line);
                    OnMessageReceived?.Invoke(msg);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
            finally
            {
                Disconnect();
                OnDisconnected?.Invoke("Sunucu ile bağlantı sonlandı.");
            }
        }
    }
}