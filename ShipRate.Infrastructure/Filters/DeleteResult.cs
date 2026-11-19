using System;
using System.Collections.Generic;
using System.Text;

namespace ShipRate.Infrastructure.Filters
{
    public class DeleteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static DeleteResult Ok() => new() { Success = true };
        public static DeleteResult Fail(string message) => new() { Success = false, Message = message };
    }
}
