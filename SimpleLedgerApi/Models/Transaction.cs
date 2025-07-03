namespace SimpleLedgerApi.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; }
}