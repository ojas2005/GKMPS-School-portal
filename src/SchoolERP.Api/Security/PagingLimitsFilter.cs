using Microsoft.AspNetCore.Mvc.Filters;

namespace SchoolERP.Api.Security;

/// <summary>
/// Every list endpoint takes `page`/`pageSize` from the query string. Unchecked, one request
/// could ask for every record at once (slow, and a cheap way to copy the whole database) or
/// send nonsense like a negative size. This keeps them sane for all endpoints in one place.
/// </summary>
public sealed class PagingLimitsFilter : IActionFilter
{
    public const int MaxPageSize = 100;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("page", out var page) && page is int p && p < 1)
            context.ActionArguments["page"] = 1;
        if (context.ActionArguments.TryGetValue("pageSize", out var size) && size is int s)
            context.ActionArguments["pageSize"] = Math.Clamp(s, 1, MaxPageSize);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
