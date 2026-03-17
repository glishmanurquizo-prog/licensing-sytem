using System.ComponentModel.DataAnnotations.Schema;

namespace LicensingServer.Models;

public class Activation
{
    public int Id { get; set; }

    public int LicenseId { get; set; }

    public required string HwidHash { get; set; }

    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("LicenseId")]
    public License? License { get; set; }
}