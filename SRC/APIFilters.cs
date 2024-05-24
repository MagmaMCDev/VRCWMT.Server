using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace VRCWMT.Filters
{
    public class SiteOwnerAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;
            string? token = request.Cookies["AuthToken"] ?? request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(token))
            {
                context.Result = new UnauthorizedObjectResult("Unauthorized");
                return;
            }

            var user = await Github.GetUserAsync(token);
            if (user == null || !user.IsSiteOwner())
            {
                context.Result = new UnauthorizedObjectResult("Unauthorized");
                return;
            }
        }
    }
    public class LoggedInAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;
            string? token = request.Cookies["AuthToken"] ?? request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(token))
            {
                context.Result = new UnauthorizedObjectResult("Unauthorized");
                return;
            }

            var user = await Github.GetUserAsync(token);
            if (user == null)
            {
                context.Result = new UnauthorizedObjectResult("Unauthorized");
                return;
            }
        }
    }
}
