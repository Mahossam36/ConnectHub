using ConnectHub.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.BLL.DTOs.Auth
{
    public class ExternalLoginRequest
    {
        public ExternalProvider Provider { get; set; }
        public string ProviderId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
