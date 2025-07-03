using FluentValidation;
using SimpleLedgerApi.Models;
using SimpleLedgerApi.Models.Requests;

namespace SimpleLedgerApi.Validators;
public class NewTransactionRequestValidator : AbstractValidator<NewTransactionRequest>
{
    public NewTransactionRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid transaction type specified.");
        
        RuleFor(x => x.Type)
            .NotNull().WithMessage("Transaction type is required.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        RuleFor(x => x.Description)
            .NotEmpty().When(x => x.Type == TransactionType.Withdrawal).WithMessage("Description is required for withdrawals.");
    }
}