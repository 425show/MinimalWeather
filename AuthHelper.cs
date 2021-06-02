namespace MinimalWeather
{
    public static class AuthHelper
    {
        public static void UserHasAnyAcceptedScopes(HttpContext context, string[] acceptedScopes)
        {
            if (acceptedScopes == null)
            {
                throw new ArgumentNullException(nameof(acceptedScopes));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            IEnumerable<Claim> userClaims;
            ClaimsPrincipal user;

            // Need to lock due to https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
            lock (context)
            {
                user = context.User;
                userClaims = user.Claims;
            }

            if (user == null || userClaims == null || !userClaims.Any())
            {
                lock (context)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                }

                throw new UnauthorizedAccessException("IDW10204: The user is unauthenticated. The HttpContext does not contain any claims.");
            }
            else
            {
                // Attempt with Scp claim
                Claim? scopeClaim = user.FindFirst(ClaimConstants.Scp);

                // Fallback to Scope claim name
                if (scopeClaim == null)
                {
                    scopeClaim = user.FindFirst(ClaimConstants.Scope);
                }

                if (scopeClaim == null || !scopeClaim.Value.Split(' ').Intersect(acceptedScopes).Any())
                {
                    string message = string.Format(CultureInfo.InvariantCulture, "IDW10203: The 'scope' or 'scp' claim does not contain scopes '{0}' or was not found. ", string.Join(",", acceptedScopes));

                    lock (context)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        context.Response.WriteAsync(message);
                        context.Response.CompleteAsync();
                    }

                    throw new UnauthorizedAccessException(message);
                }
            }
        }
    }
}