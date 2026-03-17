namespace LicensingServer.Models;

public class License
{
    public int Id { get; set; }

    public string LicenseKey { get; set; } = "";

    public string Plan { get; set; } = "";

    public int MaxActivations { get; set; }

    public bool Revoked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}