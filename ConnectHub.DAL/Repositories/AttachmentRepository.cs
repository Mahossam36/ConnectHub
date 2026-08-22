using ConnectHub.DAL.Context;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.DAL.Repositories
{
    public class AttachmentRepository : GenericRepository<Attachment>, IAttachmentRepository
    {
        public AttachmentRepository(AppDbContext context) : base(context)
        {
        }
    }
}
