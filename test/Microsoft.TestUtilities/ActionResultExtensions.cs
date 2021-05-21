using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.TestUtilities
{
    public static class ActionResultExtensions
    {
        public static T GetValue<T>(this IActionResult actionResult) where T : class
        {
            return (T) (actionResult as OkObjectResult)?.Value;
        }

        public static HttpStatusCode GetStatusCode(this IActionResult actionResult)
        {
            return (HttpStatusCode) (actionResult?.GetType()
                .GetProperty("StatusCode")?
                .GetValue(actionResult, null) ?? throw new NullReferenceException(""));
        }
    }
}
