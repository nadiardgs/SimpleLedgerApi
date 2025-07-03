using SimpleLedgerApi.Models;
using SimpleLedgerApi.Models.Requests;

namespace SimpleLedgerApi.Services.Interfaces;

public interface ILedgerService
{ 
        Transaction RecordTransaction(NewTransactionRequest request); 
        decimal GetCurrentBalance();
        IEnumerable<Transaction> GetTransactionHistory();
}