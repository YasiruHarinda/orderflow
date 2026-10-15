namespace Orders.Domain;

/// <summary>The raw data for one line of a new order, before it has been validated.</summary>
public sealed record NewOrderLine(string Sku, string ProductName, decimal UnitPrice, int Quantity);
