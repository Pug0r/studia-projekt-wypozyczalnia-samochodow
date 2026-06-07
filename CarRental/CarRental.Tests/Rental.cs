namespace CarRental.Models;

public sealed class Rental
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public int UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public decimal TotalCost { get; set; }
    public RentalStatus Status { get; set; } = RentalStatus.Active;

    public Car? Car { get; set; }
    public User? User { get; set; }
}

public enum RentalStatus
{
    Active,     
    Completed,
    Cancelled
}