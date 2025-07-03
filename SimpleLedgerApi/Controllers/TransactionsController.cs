using Microsoft.AspNetCore.Mvc;
using SimpleLedgerApi.Models;
using SimpleLedgerApi.Models.Requests;
using SimpleLedgerApi.Models.Responses;
using SimpleLedgerApi.Services.Interfaces;

namespace SimpleLedgerApi.Controllers;

[ApiController]
[Route("api/transactions")] 
public class TransactionsController : ControllerBase
{
    private readonly ILedgerService _ledgerService;
    
    public TransactionsController(ILedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }
    
    /// <summary>
    /// Returns the current balance available in the ledger.
    /// </summary>
    /// <returns>A 200 OK code with the current balance and the current UTC time.</returns>
    /// <returns>A 500 Internal Server Error code in case of unexpected exceptions.</returns>
    /// <response code="200">Returns the balance.</response>
    /// <response code="500">If an unexpected server error occurs.</response>
    [HttpGet("/api/balances")]
    public ActionResult<BalanceResponse> GetCurrentBalance()
    {
        var currentBalance = _ledgerService.GetCurrentBalance();
        var response = new BalanceResponse
        {
            Balance = currentBalance,
            Date = DateTime.UtcNow 
        };
        return StatusCode(StatusCodes.Status200OK, response);
    }
    
    /// <summary>
    /// Lists all transactions registered in the legder, from the most to the least recent.
    /// </summary>
    /// <returns>A 200 OK code with the list of transactions, containing the amount, time of transaction and description.</returns>
    /// <response code="200">Returns the list of transactions.</response>
    /// <response code="500">If an unexpected server error occurs.</response>
    [HttpGet("/api/transactions")]
    public ActionResult<IEnumerable<Transaction>> GetTransactionHistory()
    {
        var transactions = _ledgerService.GetTransactionHistory();

        return StatusCode(StatusCodes.Status200OK, transactions);
    }
    
    /// <summary>
    /// Records a new money movement (deposit or withdrawal) to the ledger.
    /// </summary>
    /// <param name="request">The transaction details, including amount, type ("Deposit" or "Withdrawal"), and description.</param>
    /// <returns>A 201 Created response containing the newly created transaction.</returns>
    /// <response code="201">Returns the newly created transaction item.</response>
    /// <response code="400">If the request is invalid (e.g., negative amount, insufficient funds for withdrawal), a misspelled transaction type.</response>
    /// <response code="500">If an unexpected server error occurs.</response>
    [HttpPost]
    public IActionResult RecordTransaction([FromBody] NewTransactionRequest request)
    {
        try
        {
            var createdTransaction = _ledgerService.RecordTransaction(request);
            
            return StatusCode(StatusCodes.Status201Created, createdTransaction); 
        }
        catch (ArgumentException ex) 
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) 
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception) 
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
        }
    }
}