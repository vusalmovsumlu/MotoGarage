using System.Security.Claims;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Infrastructure.Authentication;

public static class BusinessApprovalAuthorization
{
    // Apply only after the signed role has been checked against the account's current role.
    public static void Apply(ClaimsPrincipal principal, User user)
    {
        if (user.Role is not (UserRole.Mechanic or UserRole.Store) ||
            user.BusinessApprovalStatus == BusinessApprovalStatus.Approved) return;
        foreach (var identity in principal.Identities)
            foreach (var claim in identity.FindAll(identity.RoleClaimType).ToList())
                identity.RemoveClaim(claim);
        // The applicant can still read their application, notifications and account.
        if (principal.Identity is ClaimsIdentity current)
            current.AddClaim(new Claim(current.RoleClaimType, UserRole.User.ToString()));
    }
}
