using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Server_Inventory.Security;

internal sealed class CsrfValidationFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public const string RequestTokenHeader = "X-CSRF-TOKEN";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        if (HttpMethods.IsGet(request.Method) ||
            HttpMethods.IsHead(request.Method) ||
            HttpMethods.IsOptions(request.Method) ||
            HttpMethods.IsTrace(request.Method))
        {
            return await next(context);
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated == true &&
            request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return await next(context);
        }

        if (!request.Headers.TryGetValue(RequestTokenHeader, out var requestToken) ||
            string.IsNullOrWhiteSpace(requestToken))
        {
            return TypedResults.Problem(
                "A valid CSRF token is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return TypedResults.Problem(
                "A valid CSRF token is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }
}
