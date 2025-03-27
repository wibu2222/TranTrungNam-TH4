﻿using System;
using System.Linq;
using System.Security.Claims;

namespace ASC.Utilities
{
    public static class ClaimsPrincipalExtensions
    {
        public static CurrentUser? GetCurrentUserDetails(this ClaimsPrincipal principal)
        {
            if (principal == null || !principal.Claims.Any())
                return null;

            return new CurrentUser
            {
                Name = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty,
                Email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty,
                Roles = principal.Claims.Where(c => c.Type == ClaimTypes.Role)
                                        .Select(c => c.Value)
                                        .ToArray(),
                IsActive = bool.TryParse(principal.Claims.FirstOrDefault(c => c.Type == "IsActive")?.Value, out bool isActive) && isActive
            };
        }
    }
}
