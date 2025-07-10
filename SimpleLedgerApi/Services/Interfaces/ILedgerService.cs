using SimpleLedgerApi.Models;
using SimpleLedgerApi.Models.Requests;

namespace SimpleLedgerApi.Services.Interfaces;

public interface ILedgerService
{
        public Transaction RecordTransaction(NewTransactionRequest request); 
}