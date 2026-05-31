namespace CarRental.Models;

public sealed class Car
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal DailyRate { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int Mileage { get; set; }
    public string Faults { get; set; } = string.Empty;
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }
    public string? ImageFileName { get; set; }
}
