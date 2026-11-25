using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relogios_Lamport.Model
{
    public record EventRecord(
    int ProcessId,
    int Timestamp,
    string Type,
    string Description
    );
}
