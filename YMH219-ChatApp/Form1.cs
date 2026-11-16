using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace YMH219_ChatApp
{
    public partial class Form1 : Form
    {
        private readonly ChatServer _server;
        private readonly ChatClientService _clientService;

        private TabControl _tabControl = null!;

        // İstemci Sekmesi Bileşenleri
        private TextBox _txtServerIp = null!;
        private TextBox _txtClientPort = null!;
        private TextBox _txtUsername = null!;
        private Button _btnConnect = null!;
        private Button _btnDisconnect = null!;
        private Label _lblClientStatus = null!;
        private RichTextBox _rtbChatHistory = null!;
        private TextBox _txtMessage = null!;
        private Button _btnSend = null!;
        private Button _btnSaveChat = null!;

        // Sunucu Sekmesi Bileşenleri
        private TextBox _txtServerPort = null!;
        private Button _btnStartServer = null!;
        private Button _btnStopServer = null!;
        private Label _lblServerStatus = null!;
        private DataGridView _dgvClients = null!;
        private RichTextBox _rtbServerLogs = null!;

        public Form1()
        {
            InitializeComponent();
            _server = new ChatServer();
            _clientService = new ChatClientService();

            SetupCustomUI();
            BindEvents();
        }

        private void SetupCustomUI()
        {
            this.Text = "YMH 219 - TCP/IP İstemci-Sunucu Chat Uygulaması";
            this.Size = new Size(940, 620);
            this.MinimumSize = new Size(860, 540);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            var tabClient = new TabPage("İstemci (Sohbet Odası)")
            {
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(12)
            };

            var tabServer = new TabPage("Sunucu Yönetimi")
            {
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(12)
            };

            BuildClientTab(tabClient);
            BuildServerTab(tabServer);

            _tabControl.TabPages.Add(tabClient);
            _tabControl.TabPages.Add(tabServer);

            this.Controls.Add(_tabControl);
        }

        private void BuildClientTab(TabPage tab)
        {
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            var lblIp = new Label { Text = "Sunucu IP:", Location = new Point(12, 15), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            _txtServerIp = new TextBox { Text = "127.0.0.1", Location = new Point(92, 12), Size = new Size(115, 25), Font = new Font("Segoe UI", 9.5F) };

            var lblPort = new Label { Text = "Port:", Location = new Point(220, 15), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            _txtClientPort = new TextBox { Text = "5000", Location = new Point(262, 12), Size = new Size(65, 25), Font = new Font("Segoe UI", 9.5F) };

            var lblUser = new Label { Text = "Kullanıcı Adı:", Location = new Point(340, 15), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            _txtUsername = new TextBox { Text = "Tugra", Location = new Point(435, 12), Size = new Size(135, 25), Font = new Font("Segoe UI", 9.5F) };

            _btnConnect = CreateButton("Bağlan", new Point(585, 9), new Size(110, 32), Color.FromArgb(22, 163, 74));
            _btnDisconnect = CreateButton("Bağlantıyı Kes", new Point(705, 9), new Size(155, 32), Color.FromArgb(220, 38, 38));
            _btnDisconnect.Enabled = false;

            _lblClientStatus = new Label
            {
                Text = "● Durum: Bağlı Değil",
                Location = new Point(12, 56),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38)
            };

            _btnSaveChat = CreateButton("Geçmişi Kaydet (.txt)", new Point(650, 50), new Size(210, 32), Color.FromArgb(37, 99, 235));

            topPanel.Controls.AddRange(new Control[]
            {
                lblIp, _txtServerIp, lblPort, _txtClientPort, lblUser, _txtUsername,
                _btnConnect, _btnDisconnect, _lblClientStatus, _btnSaveChat
            });

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            _txtMessage = new TextBox
            {
                Location = new Point(12, 14),
                Size = new Size(715, 28),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Mesajınızı yazın ve Enter'a basın...",
                Enabled = false
            };

            _btnSend = CreateButton("Mesaj Gönder", new Point(738, 11), new Size(145, 34), Color.FromArgb(13, 148, 136));
            _btnSend.Enabled = false;

            bottomPanel.Controls.Add(_txtMessage);
            bottomPanel.Controls.Add(_btnSend);

            _rtbChatHistory = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 10F),
                Padding = new Padding(10)
            };

            tab.Controls.Add(_rtbChatHistory);
            tab.Controls.Add(bottomPanel);
            tab.Controls.Add(topPanel);
        }

        private void BuildServerTab(TabPage tab)
        {
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            var lblPort = new Label { Text = "Dinleme Portu:", Location = new Point(14, 21), AutoSize = true, Font = new Font("Segoe UI", 9.5F) };
            _txtServerPort = new TextBox { Text = "5000", Location = new Point(125, 18), Size = new Size(75, 26), Font = new Font("Segoe UI", 9.5F) };

            _btnStartServer = CreateButton("Sunucuyu Başlat", new Point(215, 15), new Size(175, 34), Color.FromArgb(22, 163, 74));
            _btnStopServer = CreateButton("Sunucuyu Durdur", new Point(400, 15), new Size(175, 34), Color.FromArgb(220, 38, 38));
            _btnStopServer.Enabled = false;

            _lblServerStatus = new Label
            {
                Text = "● Sunucu Kapalı",
                Location = new Point(590, 21),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38)
            };

            topPanel.Controls.AddRange(new Control[]
            {
                lblPort, _txtServerPort, _btnStartServer, _btnStopServer, _lblServerStatus
            });

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 210
            };

            var lblClientsTitle = new Label
            {
                Text = "Bağlı İstemciler Listesi",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            _dgvClients = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 32
            };

            _dgvClients.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            _dgvClients.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dgvClients.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            _dgvClients.Columns.Add("Id", "Oturum ID");
            _dgvClients.Columns.Add("Username", "Kullanıcı Adı");
            _dgvClients.Columns.Add("EndPoint", "IP / Port Bilgisi");
            _dgvClients.Columns.Add("ConnectedAt", "Bağlanma Zamanı");

            splitContainer.Panel1.Controls.Add(_dgvClients);
            splitContainer.Panel1.Controls.Add(lblClientsTitle);

            var lblLogsTitle = new Label
            {
                Text = "Sunucu Trafik ve Olay Kayıtları",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            _rtbServerLogs = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9.5F)
            };

            splitContainer.Panel2.Controls.Add(_rtbServerLogs);
            splitContainer.Panel2.Controls.Add(lblLogsTitle);

            tab.Controls.Add(splitContainer);
            tab.Controls.Add(topPanel);
        }

        private Button CreateButton(string text, Point location, Size size, Color backColor)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private void BindEvents()
        {
            // İstemci Butonları ve Olayları
            _btnConnect.Click += BtnConnect_Click;
            _btnDisconnect.Click += BtnDisconnect_Click;
            _btnSend.Click += BtnSend_Click;
            _btnSaveChat.Click += BtnSaveChat_Click;
            _txtMessage.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnSend_Click(s, EventArgs.Empty);
                }
            };

            _clientService.OnMessageReceived += msg =>
            {
                BeginInvoke(() => AppendChatMessage(msg));
            };

            _clientService.OnDisconnected += reason =>
            {
                BeginInvoke(() =>
                {
                    SetClientConnectedUI(false);
                    AppendChatMessage(new ChatMessage("Sistem", reason, true));
                });
            };

            // Sunucu Butonları ve Olayları
            _btnStartServer.Click += BtnStartServer_Click;
            _btnStopServer.Click += BtnStopServer_Click;

            _server.OnServerLog += log =>
            {
                BeginInvoke(() =>
                {
                    _rtbServerLogs.AppendText(log + Environment.NewLine);
                    _rtbServerLogs.ScrollToCaret();
                });
            };

            _server.OnClientListChanged += clients =>
            {
                BeginInvoke(() => RefreshClientGrid(clients));
            };

            this.FormClosing += (s, e) =>
            {
                _clientService.Disconnect();
                _server.Stop();
            };
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            string ip = _txtServerIp.Text.Trim();
            string username = _txtUsername.Text.Trim();

            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Lütfen Sunucu IP ve Kullanıcı Adı alanlarını doldurun.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(_txtClientPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Lütfen geçerli bir port numarası girin (1 - 65535).", "Geçersiz Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _btnConnect.Enabled = false;
                _lblClientStatus.Text = "● Durum: Bağlanılıyor...";
                _lblClientStatus.ForeColor = Color.FromArgb(234, 88, 12);

                await _clientService.ConnectAsync(ip, port, username);
                SetClientConnectedUI(true);
            }
            catch (Exception ex)
            {
                SetClientConnectedUI(false);
                MessageBox.Show($"Sunucuya bağlanılamadı:\n{ex.Message}", "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            _clientService.Disconnect();
            SetClientConnectedUI(false);
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtMessage.Text)) return;

            try
            {
                string text = _txtMessage.Text;
                _txtMessage.Clear();
                await _clientService.SendMessageAsync(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mesaj gönderilemedi: {ex.Message}", "İletim Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSaveChat_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_rtbChatHistory.Text))
            {
                MessageBox.Show("Kaydedilecek sohbet geçmişi bulunmuyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Metin Dosyası (*.txt)|*.txt",
                FileName = $"ChatGecmisi_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                Title = "Sohbet Geçmişini Kaydet"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, _rtbChatHistory.Text, Encoding.UTF8);
                MessageBox.Show("Sohbet geçmişi başarıyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnStartServer_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(_txtServerPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Lütfen geçerli bir port numarası girin (1 - 65535).", "Geçersiz Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _server.Start(port);
                _btnStartServer.Enabled = false;
                _txtServerPort.Enabled = false;
                _btnStopServer.Enabled = true;
                _lblServerStatus.Text = $"● Sunucu Aktif (Port: {port})";
                _lblServerStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sunucu başlatılamadı:\n{ex.Message}", "Sunucu Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStopServer_Click(object? sender, EventArgs e)
        {
            _server.Stop();
            _btnStartServer.Enabled = true;
            _txtServerPort.Enabled = true;
            _btnStopServer.Enabled = false;
            _lblServerStatus.Text = "● Sunucu Kapalı";
            _lblServerStatus.ForeColor = Color.FromArgb(220, 38, 38);
        }

        private void SetClientConnectedUI(bool connected)
        {
            _btnConnect.Enabled = !connected;
            _txtServerIp.Enabled = !connected;
            _txtClientPort.Enabled = !connected;
            _txtUsername.Enabled = !connected;

            _btnDisconnect.Enabled = connected;
            _txtMessage.Enabled = connected;
            _btnSend.Enabled = connected;

            if (connected)
            {
                _lblClientStatus.Text = $"● Durum: Bağlı ({_clientService.Username} @ {_txtServerIp.Text}:{_txtClientPort.Text})";
                _lblClientStatus.ForeColor = Color.FromArgb(22, 163, 74);
                _txtMessage.Focus();
            }
            else
            {
                _lblClientStatus.Text = "● Durum: Bağlı Değil";
                _lblClientStatus.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }

        private void AppendChatMessage(ChatMessage msg)
        {
            _rtbChatHistory.SelectionStart = _rtbChatHistory.TextLength;
            _rtbChatHistory.SelectionLength = 0;

            if (msg.IsSystemMessage)
            {
                _rtbChatHistory.SelectionColor = Color.FromArgb(100, 116, 139);
                _rtbChatHistory.SelectionFont = new Font("Consolas", 9.5F, FontStyle.Italic);
            }
            else if (msg.Sender == _clientService.Username)
            {
                _rtbChatHistory.SelectionColor = Color.FromArgb(22, 101, 52);
                _rtbChatHistory.SelectionFont = new Font("Consolas", 10F, FontStyle.Bold);
            }
            else
            {
                _rtbChatHistory.SelectionColor = Color.FromArgb(30, 58, 138);
                _rtbChatHistory.SelectionFont = new Font("Consolas", 10F, FontStyle.Regular);
            }

            _rtbChatHistory.AppendText(msg.ToDisplayString() + Environment.NewLine);
            _rtbChatHistory.ScrollToCaret();
        }

        private void RefreshClientGrid(List<ConnectedClientInfo> clients)
        {
            _dgvClients.Rows.Clear();
            foreach (var c in clients)
            {
                _dgvClients.Rows.Add(c.ClientId, c.Username, c.RemoteEndPoint, c.ConnectedAt.ToString("HH:mm:ss"));
            }
        }
    }
}