using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SimpleLedgerApi.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)] 
    public class ErrorController : ControllerBase
    {
        [Route("/error")]
        public IActionResult HandleError()
        {
            var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature?.Error;

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred. Please try again later.",
            });
        }
    }
}