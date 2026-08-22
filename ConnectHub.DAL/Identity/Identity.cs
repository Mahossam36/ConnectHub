using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.DAL.Identity
{
    /// <summary>
    /// Represents the authentication and security account in ASP.NET Core Identity.
    /// Shares the same Guid Id with the domain User profile.
    /// </summary>
    public class ApplicationUser : IdentityUser<Guid>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
