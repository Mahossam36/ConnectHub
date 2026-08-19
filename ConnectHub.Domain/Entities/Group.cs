using ConnectHub.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.Models.Entities
{
    /// <summary>
    /// Represents a community group within Connect Hub.
    /// </summary>
    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>
        /// Server-side path to the group's image.
        /// </summary>
        public string? ImagePath { get; set; }

        /// <summary>
        /// Identifies the user who created the group.
        /// </summary>
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public User CreatedBy { get; set; } = null!;
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    }
}
