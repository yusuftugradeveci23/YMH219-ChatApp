using System;

namespace YMH219_ChatApp
{
    /// <summary>
    /// Sohbet mesajlarını modelleyen ve ağ protokolü formatına dönüştüren sınıf.
    /// </summary>
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSystemMessage { get; set; }

        public ChatMessage(string sender, string content, bool isSystemMessage = false)
        {
            Sender = sender;
            Content = content;
            Timestamp = DateTime.Now;
            IsSystemMessage = isSystemMessage;
        }

        /// <summary>
        /// Mesajı arayüzde gösterilecek zaman damgalı metne çevirir.
        /// </summary>
        public string ToDisplayString()
        {
            return IsSystemMessage
                ? $"[{Timestamp:HH:mm:ss}] * {Content}"
                : $"[{Timestamp:HH:mm:ss}] {Sender}: {Content}";
        }

        /// <summary>
        /// TCP üzerinden tek satırda iletilecek paket formatına çevirir.
        /// </summary>
        public string Serialize()
        {
            string typeFlag = IsSystemMessage ? "SYS" : "MSG";
            string cleanContent = Content.Replace("\r", " ").Replace("\n", " ");
            return $"{typeFlag}|{Timestamp:HH:mm:ss}|{Sender}|{cleanContent}";
        }

        /// <summary>
        /// Ağdan gelen paket satırını ChatMessage nesnesine dönüştürür.
        /// </summary>
        public static ChatMessage Deserialize(string rawLine)
        {
            var parts = rawLine.Split('|', 4);
            if (parts.Length < 4)
            {
                return new ChatMessage("Sistem", rawLine, true);
            }

            bool isSys = parts[0] == "SYS";
            var msg = new ChatMessage(parts[2], parts[3], isSys);
            if (DateTime.TryParse(parts[1], out DateTime parsedTime))
            {
                msg.Timestamp = parsedTime;
            }
            return msg;
        }
    }
}