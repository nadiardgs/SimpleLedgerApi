using SimpleLedgerApi.Models;
using SimpleLedgerApi.Models.Requests;
using SimpleLedgerApi.Services.Interfaces;

namespace SimpleLedgerApi.Services;

public class LedgerService : ILedgerService
{
    private static readonly List<Transaction> _transactions = new List<Transaction>();
    private static readonly List<Account> _accounts = new List<Account>();
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

    public void RecordTransfer(Account sender, Account receiver, decimal amount)
    {
        //var senderTransactions = _accounts.GetById(sender.Id).Transactions;

        var balance = GetCurrentBalance();

        var senderAccount = new Account
        {
            Balance = balance
        };

        if (balance < amount)
        {
            throw new InvalidOperationException("Insufficient funds for transfer.");
        }

        lock (_lock)
        {
            var newBalance = balance - amount;
            senderAccount.Balance = newBalance;

            var receiverBalance = GetCurrentBalance();
            var receiverAccount = new Account
            {
                Balance = receiverBalance + amount
            };
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