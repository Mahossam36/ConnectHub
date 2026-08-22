using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectHub.DAL.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        builder.Property(a => a.Metadata)
            .HasColumnType("nvarchar(max)");

        // AuditLog → User (many-to-one, nullable — system actions have no user)
        builder.HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
