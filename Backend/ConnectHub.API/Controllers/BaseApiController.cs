using System.Security.Claims;
using Ardalis.Result;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(idClaim, out var userId) ? userId : null;
    }

    protected Guid GetRequiredUserId()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("Current user is not authenticated.");
        return userId.Value;
    }

    protected ActionResult ToActionResult(Result result)
    {
        return result.Status switch
        {
            ResultStatus.Ok => Ok(),
            ResultStatus.NotFound => NotFound(CreateProblem(404, "Not Found", result.Errors)),
            ResultStatus.Unauthorized => Unauthorized(CreateProblem(401, "Unauthorized", result.Errors)),
            ResultStatus.Forbidden => StatusCode(403, CreateProblem(403, "Forbidden", result.Errors)),
            ResultStatus.Conflict => Conflict(CreateProblem(409, "Conflict", result.Errors)),
            ResultStatus.Invalid => BadRequest(CreateValidationProblem(result.ValidationErrors)),
            ResultStatus.Error => BadRequest(CreateProblem(400, "Bad Request", result.Errors)),
            _ => StatusCode(500, CreateProblem(500, "Internal Server Error", result.Errors))
        };
    }

    protected ActionResult<T> ToActionResult<T>(Result<T> result)
    {
        return result.Status switch
        {
            ResultStatus.Ok => Ok(result.Value),
            ResultStatus.NotFound => NotFound(CreateProblem(404, "Not Found", result.Errors)),
            ResultStatus.Unauthorized => Unauthorized(CreateProblem(401, "Unauthorized", result.Errors)),
            ResultStatus.Forbidden => StatusCode(403, CreateProblem(403, "Forbidden", result.Errors)),
            ResultStatus.Conflict => Conflict(CreateProblem(409, "Conflict", result.Errors)),
            ResultStatus.Invalid => BadRequest(CreateValidationProblem(result.ValidationErrors)),
            ResultStatus.Error => BadRequest(CreateProblem(400, "Bad Request", result.Errors)),
            _ => StatusCode(500, CreateProblem(500, "Internal Server Error", result.Errors))
        };
    }

    protected ActionResult<T> ToCreatedResult<T>(Result<T> result, string? uri = null)
    {
        if (result.Status == ResultStatus.Ok)
            return StatusCode(201, result.Value);

        return ToActionResult(result);
    }

    private static ProblemDetails CreateProblem(int status, string title, IEnumerable<string>? errors)
    {
        var detail = errors != null && errors.Any() ? string.Join(" ", errors) : title;
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
    }

    private static ValidationProblemDetails CreateValidationProblem(IEnumerable<ValidationError>? validationErrors)
    {
        var modelState = new Dictionary<string, string[]>();
        if (validationErrors != null)
        {
            foreach (var ve in validationErrors)
            {
                var key = string.IsNullOrWhiteSpace(ve.Identifier) ? "General" : ve.Identifier;
                if (!modelState.ContainsKey(key))
                    modelState[key] = new[] { ve.ErrorMessage };
                else
                    modelState[key] = modelState[key].Append(ve.ErrorMessage).ToArray();
            }
        }

        return new ValidationProblemDetails(modelState)
        {
            Status = 400,
            Title = "One or more validation errors occurred."
        };
    }
}
