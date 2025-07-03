namespace SimpleLedgerApi.Models.Requests;

public class NewTransactionRequest
{
    public decimal Amount { get; set; }
    public TransactionType? Type { get; set; }
    public string Description { get; set; }
}