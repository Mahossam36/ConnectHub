using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.DAL.Configurations
{

    public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.ToTable("Attachments");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FilePath).IsRequired().HasMaxLength(500);
            builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(x => x.FileSize).IsRequired();
            builder.Property(x => x.UploadedAt).IsRequired();
            builder.HasOne(a => a.UploadedBy)
                .WithMany()
                .HasForeignKey(a => a.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(a => a.Post)
                .WithMany(p => p.Attachments)
                .HasForeignKey(a => a.PostId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
