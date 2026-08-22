using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.DAL.Configurations
{
    public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.ToTable("Reports");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TargetType).IsRequired().HasConversion<int>();
            builder.Property(x => x.TargetId).IsRequired();
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Status).IsRequired().HasConversion<int>();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasOne(r => r.ReportedBy)
                .WithMany()
                .HasForeignKey(r => r.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TargetType, x.TargetId });
        }
    }
}
