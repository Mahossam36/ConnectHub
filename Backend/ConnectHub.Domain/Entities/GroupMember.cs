using ConnectHub.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.Models.Entities
{

    /// <summary>
    /// Represents a user's membership and role within a specific group.
    /// </summary>
    public class GroupMember
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public GroupRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        public Group Group { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
