namespace Firmeza.WebApplication.Models;

public class Product
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Product(string name, decimal unitPrice)
    {
        Name = (name ?? string.Empty).Trim();
        UnitPrice = unitPrice;
    }
}
