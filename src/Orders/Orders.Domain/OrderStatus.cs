namespace Orders.Domain;

/// <summary>
/// Submitted ──► AwaitingStock ──► Confirmed
///     │               │
///     └───────────────┴──► Rejected
/// </summary>
public enum OrderStatus
{
    Submitted,
    AwaitingStock,
    Confirmed,
    Rejected,
}
