namespace FindAirConditioners.Web.Core.Options;

public sealed class NotificationOptions
{
    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 1025;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool UseSsl { get; set; }

    public string FromAddress { get; set; } = "no-reply@local.test";

    public string FromName { get; set; } = "Find Air Conditioners";
}
