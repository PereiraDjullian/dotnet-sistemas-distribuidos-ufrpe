using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relogios_Lamport.Model
{
    public record LamportMessage(
    string Type,
    int SenderId,
    int Timestamp,
    string Content,
    List<EventRecord>? Events = null
    );
}
