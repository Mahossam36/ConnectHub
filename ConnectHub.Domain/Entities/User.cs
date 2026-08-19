namespace ConnectHub.Models.Entities
{

    /// <summary>
    /// Represents a user's profile and account status within Connect Hub.
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Bio { get; set; }

        /// <summary>
        /// Server-side path to the user's profile image.
        /// The image itself is stored on the server.
        /// </summary>
        public string? ProfileImagePath { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Group> CreatedGroups { get; set; } = new List<Group>();
        public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    }
}