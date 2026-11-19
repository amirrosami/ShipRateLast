using System;
using System.Collections.Generic;
using System.Text;

namespace ShipRate.Domain.Interfaces
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
    }

}
