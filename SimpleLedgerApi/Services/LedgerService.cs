using SimpleLedgerApi.Models;
using SimpleLedgerApi.Models.Requests;
using SimpleLedgerApi.Services.Interfaces;

namespace SimpleLedgerApi.Services;

public class LedgerService : ILedgerService
{
    private static readonly List<Transaction> _transactions = new List<Transaction>();
    private static readonly object _lock = new object();

    public decimal GetCurrentBalance()
    {
        lock (_lock)
        {
            return _transactions.Sum(t => t.Type == TransactionType.Deposit ? t.Amount : -t.Amount);
        }
    }
    
    public IEnumerable<Transaction> GetTransactionHistory()
    {
        lock (_lock)
        {
            return _transactions.OrderByDescending(t => t.Timestamp).ToList();
        }
    }
    
    public Transaction RecordTransaction(NewTransactionRequest request)
    {
        if (request.Type == TransactionType.Withdrawal)
        {
            decimal currentBalance;
            lock (_lock) 
            {
                currentBalance = _transactions.Sum(t => t.Type == TransactionType.Deposit ? t.Amount : -t.Amount);
            }

            if (currentBalance < request.Amount)
            {
                throw new InvalidOperationException("Insufficient funds for withdrawal.");
            }
        }

        var newTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            Timestamp = DateTime.UtcNow,
            Type = request.Type.Value,
            Description = request.Description
        };

        lock (_lock) 
        {
            _transactions.Add(newTransaction);
        }

        return newTransaction; 
    }
}