using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algoritmo_Berkeley.Models
{
    public record BerkeleyMessage
    {

        public string Type { get; init; } = string.Empty;
        public long TimestampTicks { get; init; }    
        public double AdjustmentMs { get; init; }
    }
}
