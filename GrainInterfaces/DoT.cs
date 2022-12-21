using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces
{
    [GenerateSerializer, Immutable]
    public class DoT
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int NumberOfTicks { get; set; }
        public int TickValue { get; set; }
    }
    [GenerateSerializer, Immutable]
    public class Tick
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int TickValue { get; set; }
        public string Status { get; set; } = "Running----";
        public override string ToString() => $"Tick of type {Type} dealing {TickValue} damage, Status: {Status}";
    }
}
