using Moq; 
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; 

using SimpleLedgerApi.Controllers;
using SimpleLedgerApi.Models;
using SimpleLedgerApi.Services.Interfaces;

namespace SimpleLedgerApi.Tests.Controllers
{
    public class TransactionsControllerTests : IDisposable 
    {
        private readonly TransactionsController _controller;
        private readonly Mock<ILedgerService> _mockLedgerService;
        private const string UnexpectedErrorMessage = "An unexpected error occurred. Please try again later.";

        public TransactionsControllerTests()
        {
            _mockLedgerService = new Mock<ILedgerService>();
            _controller = new TransactionsController(_mockLedgerService.Object);
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
        public void GetTransactionHistory_ShouldThrowInternalServerError()
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
    }
}