using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.DAL.Configurations
{
    public sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
    {
        public void Configure(EntityTypeBuilder<GroupMember> builder)
        {
            builder.ToTable("GroupMembers");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Role).IsRequired().HasConversion<int>();
            builder.Property(x => x.JoinedAt).IsRequired();
            builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

            // Unique constraint: A user can only have one membership record per group
            builder.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
            builder.HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(gm => gm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
