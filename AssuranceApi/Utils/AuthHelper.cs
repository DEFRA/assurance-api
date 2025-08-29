using System.Security.Claims;

namespace AssuranceApi.Utils
{
    /// <summary>
    /// Provides helper methods for working with authentication and authorization.
    /// </summary>
    public static class AuthHelper
    {
        /// <summary>
        /// Retrieves the email address from the specified <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the authenticated user.</param>
        /// <returns>The email address if found; otherwise, <c>null</c>.</returns>
        public static string? GetEmail(this ClaimsPrincipal user)
        {
            return user?.FindFirst(ClaimTypes.Email)?.Value
                ?? user?.FindFirst("email")?.Value
                ?? user?.FindFirst("preferred_username").Value;
        }
    }
}
