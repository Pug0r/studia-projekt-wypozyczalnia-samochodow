namespace CarRental.Models;

public sealed class RentalDefect
{
    public int Id { get; set; }
    public int RentalId { get; set; }
    public DefectType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OtherPartyInsuranceNumber { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.Now;
    public byte[]? PhotoData { get; set; }
    public string? PhotoContentType { get; set; }
    public string? PhotoFileName { get; set; }

    public Rental? Rental { get; set; }
}

public enum DefectType
{
    RenterFault,
    RoadAccident,
    UndisclosedAtRentalStart
}
