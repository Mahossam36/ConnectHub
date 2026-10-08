using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectHub.DAL.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}
