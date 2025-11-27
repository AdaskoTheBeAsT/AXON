namespace Axon.Performance;

/// <summary>
/// Medium payload model (~300 bytes per record).
/// </summary>
public sealed class Order
{
    public int OrderId { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public string ShippingCity { get; set; } = string.Empty;

    public string ShippingCountry { get; set; } = string.Empty;

    public string ShippingZip { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public int ItemCount { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? ShippedDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;
}
