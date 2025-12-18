namespace WebAppServer.Services
{
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public string? Username { get; set; }

        public string? Password { get; set; }

        public bool UseSsl { get; set; } = true;

        public string From { get; set; } = "noreply@example.com";
    }
}
