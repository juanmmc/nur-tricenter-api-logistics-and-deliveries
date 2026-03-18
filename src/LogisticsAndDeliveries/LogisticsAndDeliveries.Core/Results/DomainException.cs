using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LogisticsAndDeliveries.Core.Results
{
    public class DomainException(Error Error) : Exception
    {
        public Error Error { get; } = Error;
    }
}