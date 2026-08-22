using ConnectHub.DAL.Identity;
using ConnectHub.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ConnectHub.DAL.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> DomainUsers => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<PostLike> PostLikes => Set<PostLike>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
        public DbSet<Attachment> Attachments => Set<Attachment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Apply all IEntityTypeConfiguration classes from this assembly
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}