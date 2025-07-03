using Moq; 
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; 

using SimpleLedgerApi.Controllers;
using SimpleLedgerApi.Models;
using SimpleLedgerApi.Models.Requests;
using SimpleLedgerApi.Models.Responses;
using SimpleLedgerApi.Services.Interfaces;
using SimpleLedgerApi.Validators;

namespace SimpleLedgerApi.Tests.Controllers
{
    public class TransactionsControllerTests : IDisposable
    {
        private readonly TransactionsController _controller;
        private readonly Mock<ILedgerService> _mockLedgerService;
        
        private const string UnexpectedErrorMessage = "An unexpected error occurred. Please try again later.";
        private const string MissingTransactionErrorMessage = "Transaction type is required.";
        private const string NegativeAmountErrorMessage = "Amount must be greater than zero.";
        private const string InsufficientFundsErrorMessage = "nsufficient funds for withdrawal.";
        private const string DescriptionRequiredErrorMessage = "Description is required for withdrawals.";
        private const string DescriptionTooLongErrorMessage = "Description cannot exceed 500 characters.";

        public TransactionsControllerTests()
        {
            _mockLedgerService = new Mock<ILedgerService>();
            _controller = new TransactionsController(_mockLedgerService.Object);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        public void Dispose()
        {
        }

        [Fact] 
        public void GetTransactionHistory_ShouldReturnOkWithListOfTransactions()
        {
            // ARRANGE
            var expectedTransactions = new List<Transaction>
            {
                new Transaction { Id = Guid.NewGuid(), Amount = 100, Type = TransactionType.Deposit, Description = "Salary", Timestamp = DateTime.UtcNow },
                new Transaction { Id = Guid.NewGuid(), Amount = 50, Type = TransactionType.Withdrawal, Description = "Rent", Timestamp = DateTime.UtcNow.AddHours(-1) }
            };

            _mockLedgerService.Setup(s => s.GetTransactionHistory())
                              .Returns(expectedTransactions);

            // ACT
            var result = _controller.GetTransactionHistory();

            // ASSERT
            result.Should().BeOfType<ActionResult<IEnumerable<Transaction>>>();

            var okResult = result.Result.Should().BeOfType<ObjectResult>().Subject;

            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

            var transactions = okResult.Value.Should().BeAssignableTo<IEnumerable<Transaction>>().Subject;

            var enumerable = transactions.ToList();
            enumerable.Should().HaveCount(2);
            enumerable.Should().Contain(expectedTransactions);
            
            _mockLedgerService.Verify(s => s.GetTransactionHistory(), Times.Once());
        }
        
        [Fact] 
        public void GetTransactionHistory_ShouldReturn500InternalServerError_WhenServiceThrowsUnexpectedException()
        {
            // ARRANGE
            _mockLedgerService.Setup(s => s.GetTransactionHistory())
                .Throws(new Exception(UnexpectedErrorMessage));

            // ACT & ASSERT
            FluentActions.Invoking(() => _controller.GetTransactionHistory())
                .Should().Throw<Exception>()
                .WithMessage(UnexpectedErrorMessage);

            _mockLedgerService.Verify(s => s.GetTransactionHistory(), Times.Once());
        }
        
        [Fact]
        public void GetCurrentBalance_ShouldReturnOkWithBalance()
        {
            // ARRANGE
            var response = new BalanceResponse()
            {
                Balance = 345.98m,
                Date = DateTime.UtcNow
            };
            
            _mockLedgerService.Setup(s => s.GetCurrentBalance())
                .Returns(response.Balance);

            // ACT
            var result = _controller.GetCurrentBalance();

            // ASSERT
            var okResult = result.Result.Should().BeOfType<ObjectResult>().Subject;

            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

            var balanceResponse = okResult.Value.Should().BeAssignableTo<BalanceResponse>().Subject;

            balanceResponse.Balance.Should().Be(response.Balance);
            balanceResponse.Date.Should().BeCloseTo(response.Date, 100.Milliseconds());
            
            _mockLedgerService.Verify(s => s.GetCurrentBalance(), Times.Once());
        }
        
        [Fact] 
        public void GetCurrentBalance_ShouldReturn500InternalServerError_WhenServiceThrowsUnexpectedException()
        {
            // ARRANGE
            _mockLedgerService.Setup(s => s.GetCurrentBalance())
                .Throws(new Exception(UnexpectedErrorMessage));

            // ACT & ASSERT
            FluentActions.Invoking(() => _controller.GetCurrentBalance())
                .Should().Throw<Exception>()
                .WithMessage(UnexpectedErrorMessage);

            _mockLedgerService.Verify(s => s.GetCurrentBalance(), Times.Once());
        }
        
        [Fact]
        public void RecordTransaction_ShouldReturn201Created_WhenDepositIsSuccessful()
        {
            // ARRANGE
            var deposit = new NewTransactionRequest()
            {
                Amount = 500.7m,
                Description = "Income tax restitution.",
                Type = TransactionType.Deposit
            };
            
            var transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                Amount = deposit.Amount,
                Description = deposit.Description,
                Type = deposit.Type.Value,
                Timestamp = DateTime.UtcNow
            };
            
            _mockLedgerService.Setup(s => s.RecordTransaction(deposit))
                .Returns(transaction);

            // ACT
            var result = _controller.RecordTransaction(deposit);

            // ASSERT
            var okResult = result.Should().BeOfType<ObjectResult>().Subject;

            okResult.StatusCode.Should().Be(StatusCodes.Status201Created);

            var depositResponse = okResult.Value.Should().BeAssignableTo<Transaction>().Subject;

            depositResponse.Id.Should().Be(transaction.Id);
            depositResponse.Amount.Should().Be(deposit.Amount);
            depositResponse.Description.Should().Be(deposit.Description);
            depositResponse.Type.Should().Be(TransactionType.Deposit);
            depositResponse.Timestamp.Should().BeCloseTo(transaction.Timestamp, 100.Milliseconds());
            
            _mockLedgerService.Verify(s => s.RecordTransaction(deposit), Times.Once());
        }
        
        [Fact]
        public void RecordTransaction_ShouldReturn400BadRequest_WhenTransactionTypeIsMissing()
        {
            // ARRANGE
            var invalidDepositRequest = new NewTransactionRequest
            {
                Amount = 100m,
                Type = null,
                Description = "Test deposit without type"
            };
            
            var validator = new NewTransactionRequestValidator();
            var validationResult = validator.Validate(invalidDepositRequest);

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    _controller.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }
            
                // ACT
                var result = _controller.RecordTransaction(invalidDepositRequest);

                // ASSERT
                result.Should().BeOfType<BadRequestObjectResult>();
                var badRequestResult = result.As<BadRequestObjectResult>();
                badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

                badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();

                var problemDetails = badRequestResult.Value as ValidationProblemDetails;

                problemDetails.Should().NotBeNull();

                problemDetails.Errors.Should().NotBeNullOrEmpty();
                problemDetails.Errors.Should().ContainKey("Type");
                problemDetails.Errors["Type"].Should().Contain(MissingTransactionErrorMessage);
                
                _mockLedgerService.Verify(s => s.RecordTransaction(It.IsAny<NewTransactionRequest>()), Times.Never());
            }
        
        [Fact]
        public void RecordTransaction_ShouldReturn400BadRequest_WhenDepositAmountIsNegative()
        {
            // ARRANGE
            var invalidDepositRequest = new NewTransactionRequest
            {
                Amount = -7,
                Type = TransactionType.Deposit,
                Description = "Test deposit with negative amount"
            };
            
            var validator = new NewTransactionRequestValidator();
            var validationResult = validator.Validate(invalidDepositRequest);

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    _controller.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }
            
            // ACT
            var result = _controller.RecordTransaction(invalidDepositRequest);

            // ASSERT
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();

            var problemDetails = badRequestResult.Value as ValidationProblemDetails;

            problemDetails.Should().NotBeNull();

            problemDetails.Errors.Should().NotBeNullOrEmpty();
            problemDetails.Errors.Should().ContainKey("Amount");
            problemDetails.Errors["Amount"].Should().Contain(NegativeAmountErrorMessage);
            
            _mockLedgerService.Verify(s => s.RecordTransaction(It.IsAny<NewTransactionRequest>()), Times.Never());
        }

        [Fact]
        public void RecordTransaction_ShouldReturn201Created_WhenWithdrawalIsSuccessful()
        {
            // ARRANGE
            var withdrawal = new NewTransactionRequest()
            {
                Amount = 450.2m,
                Description = "College tuition fee",
                Type = TransactionType.Withdrawal
            };
            
            var withdrawalTransaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                Amount = withdrawal.Amount,
                Description = withdrawal.Description,
                Type = withdrawal.Type.Value,
                Timestamp = DateTime.UtcNow
            };
            
            _mockLedgerService.Setup(s => s.RecordTransaction(withdrawal))
                .Returns(withdrawalTransaction);

            // ACT
            var result = _controller.RecordTransaction(withdrawal);

            // ASSERT
            var okResult = result.Should().BeOfType<ObjectResult>().Subject;

            okResult.StatusCode.Should().Be(StatusCodes.Status201Created);

            var depositResponse = okResult.Value.Should().BeAssignableTo<Transaction>().Subject;

            depositResponse.Id.Should().Be(withdrawalTransaction.Id);
            depositResponse.Amount.Should().Be(withdrawal.Amount);
            depositResponse.Description.Should().Be(withdrawal.Description);
            depositResponse.Type.Should().Be(TransactionType.Withdrawal);
            depositResponse.Timestamp.Should().BeCloseTo(withdrawalTransaction.Timestamp, 100.Milliseconds());
            
            _mockLedgerService.Verify(s => s.RecordTransaction(withdrawal), Times.Once());
        }
        
        [Fact]
        public void RecordTransaction_ShouldReturn400BadRequest_WhenWithdrawalHasInsufficientFunds()
        {
            // ARRANGE
            var invalidWithdrawal = new NewTransactionRequest()
            {
                Amount = 1500m,
                Description = "College tuition fee",
                Type = TransactionType.Withdrawal
            };

            _mockLedgerService.Setup(s => s.RecordTransaction(It.IsAny<NewTransactionRequest>()))
                .Throws(new InvalidOperationException(InsufficientFundsErrorMessage));
            
            // ACT
            var result = _controller.RecordTransaction(invalidWithdrawal);

            // ASSERT
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            badRequestResult.Value.Should().BeOfType<ProblemDetails>();

            var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(InsufficientFundsErrorMessage);
            problemDetails.Title.Should().Be("Bad Request");
            
            _mockLedgerService.Verify(s => s.RecordTransaction(It.IsAny<NewTransactionRequest>()), Times.Once());
        }
        
        [Fact]
        public void RecordTransaction_ShouldReturn400BadRequest_WhenWithdrawalHasNoDescription()
        {
            // ARRANGE
            var invalidWithdrawal = new NewTransactionRequest()
            {
                Amount = 1500m,
                Description = null,
                Type = TransactionType.Withdrawal
            };

            var validator = new NewTransactionRequestValidator();
            var validationResult = validator.Validate(invalidWithdrawal);

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    _controller.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }
            
            // ACT
            var result = _controller.RecordTransaction(invalidWithdrawal);

            // ASSERT
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();

            var problemDetails = badRequestResult.Value as ValidationProblemDetails;

            problemDetails.Should().NotBeNull();

            problemDetails.Errors.Should().NotBeNullOrEmpty();
            problemDetails.Errors.Should().ContainKey("Description");
            problemDetails.Errors["Description"].Should().Contain(DescriptionRequiredErrorMessage);
            
            _mockLedgerService.Verify(s => s.RecordTransaction(It.IsAny<NewTransactionRequest>()), Times.Never());
        }
        
        [Fact]
        public void RecordTransaction_ShouldReturn400BadRequest_WhenDescriptionIsTooLong()
        {
            // ARRANGE
            var invalidDeposit = new NewTransactionRequest()
            {
                Amount = 2000m,
                Description = new string('a', 600),
                Type = TransactionType.Withdrawal
            };
            
            _mockLedgerService.Setup(s => s.RecordTransaction(invalidDeposit))
                .Throws(new InvalidOperationException(DescriptionTooLongErrorMessage));
            
            var validator = new NewTransactionRequestValidator();
            var validationResult = validator.Validate(invalidDeposit);
            
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    _controller.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }
            
            // ACT
            var result = _controller.RecordTransaction(invalidDeposit);

            // ASSERT
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();

            var problemDetails = badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
            
            problemDetails.Errors.Should().NotBeNullOrEmpty();
            problemDetails.Errors.Should().ContainKey("Description");
            problemDetails.Errors["Description"].Should().Contain(DescriptionTooLongErrorMessage);
            
            _mockLedgerService.Verify(s => s.RecordTransaction(It.IsAny<NewTransactionRequest>()), Times.Never());
        }
    }
}