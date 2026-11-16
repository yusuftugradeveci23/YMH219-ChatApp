using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YMH219_ChatApp
{
    /// <summary>
    /// TCP tabanlı çoklu istemci destekli sohbet sunucusu sınıfı.
    /// </summary>
    public class ChatServer
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly List<ConnectedClientInfo> _clients = new();
        private readonly object _lockObj = new();

        public bool IsRunning { get; private set; }
        public int Port { get; private set; }

        public event Action<string>? OnServerLog;
        public event Action<List<ConnectedClientInfo>>? OnClientListChanged;

        public void Start(int port)
        {
            if (IsRunning) return;

            Port = port;
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            IsRunning = true;

            OnServerLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] Sunucu {port} portunda başlatıldı. İstemciler bekleniyor...");
            _ = AcceptClientsLoopAsync(_cts.Token);
        }

        public void Stop()
        {
            if (!IsRunning) return;

            IsRunning = false;
            _cts?.Cancel();

            lock (_lockObj)
            {
                foreach (var clientInfo in _clients)
                {
                    try { clientInfo.Client.Close(); } catch { }
                }
                _clients.Clear();
            }

            try { _listener?.Stop(); } catch { }
            NotifyClientListChanged();
            OnServerLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] Sunucu durduruldu.");
        }

        private async Task AcceptClientsLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null)
            {
                try
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync(token);
                    _ = HandleClientAsync(tcpClient, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (IsRunning)
                        OnServerLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] Bağlantı kabul hatası: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            ConnectedClientInfo? clientInfo = null;
            try
            {
                using var reader = new StreamReader(tcpClient.GetStream(), Encoding.UTF8);

                // İlk satır istemcinin kullanıcı adı bilgisidir: JOIN|KullaniciAdi
                string? firstLine = await reader.ReadLineAsync(token);
                if (string.IsNullOrWhiteSpace(firstLine))
                {
                    tcpClient.Close();
                    return;
                }

                string username = firstLine.StartsWith("JOIN|")
                    ? firstLine.Substring(5).Trim()
                    : firstLine.Trim();

                clientInfo = new ConnectedClientInfo(tcpClient, username);

                lock (_lockObj)
                {
                    _clients.Add(clientInfo);
                }

                NotifyClientListChanged();
                OnServerLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {clientInfo.Username} ({clientInfo.RemoteEndPoint}) bağlandı.");

                var joinMsg = new ChatMessage("Sistem", $"{clientInfo.Username} sohbet odasına katıldı.", true);
                await BroadcastMessageAsync(joinMsg);

                // İstemciden gelen mesajları sürekli dinle
                while (!token.IsCancellationRequested && tcpClient.Connected)
                {
                    string? incomingLine = await reader.ReadLineAsync(token);
                    if (incomingLine == null) break;

                    var incomingMsg = ChatMessage.Deserialize(incomingLine);
                    incomingMsg.Sender = clientInfo.Username;
                    incomingMsg.Timestamp = DateTime.Now;

                    OnServerLog?.Invoke(incomingMsg.ToDisplayString());
                    await BroadcastMessageAsync(incomingMsg);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
            finally
            {
                if (clientInfo != null)
                {
                    lock (_lockObj)
                    {
                        _clients.Remove(clientInfo);
                    }
                    NotifyClientListChanged();
                    OnServerLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {clientInfo.Username} bağlantısı kesildi.");

                    if (IsRunning)
                    {
                        var leaveMsg = new ChatMessage("Sistem", $"{clientInfo.Username} sohbet odasından ayrıldı.", true);
                        await BroadcastMessageAsync(leaveMsg);
                    }
                }
                try { tcpClient.Close(); } catch { }
            }
        }

        private async Task BroadcastMessageAsync(ChatMessage message)
        {
            string serialized = message.Serialize();
            List<ConnectedClientInfo> snapshot;

            lock (_lockObj)
            {
                snapshot = new List<ConnectedClientInfo>(_clients);
            }

            foreach (var client in snapshot)
            {
                try
                {
                    if (client.Client.Connected)
                    {
                        await client.Writer.WriteLineAsync(serialized);
                    }
                }
                catch { }
            }
        }

        private void NotifyClientListChanged()
        {
            List<ConnectedClientInfo> snapshot;
            lock (_lockObj)
            {
                snapshot = new List<ConnectedClientInfo>(_clients);
            }
            OnClientListChanged?.Invoke(snapshot);
        }
    }
}