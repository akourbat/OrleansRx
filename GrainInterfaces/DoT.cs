using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces
{
    [GenerateSerializer, Immutable]
    public record Tick (int Id, string Type, int TickValue, string Status = "Running----")
    {
        public override string ToString() => $"Tick of type {Type} dealing {TickValue} damage, Status: {Status}";
    }

    [GenerateSerializer, Immutable]
    public class DoT
    {
        [Id(0)]
        public int Id { get; set; }
        [Id(1)]
        public string Type { get; set; }
        [Id(2)]
        public int NumberOfTicks { get; set; }
        [Id(3)]
        public int TickValue { get; set; }

        public override string ToString()
        {
            return $"Tick of type {Type} was added";
        }
    }
    //[GenerateSerializer, Immutable]
    //public class Tick
    //{
    //    public int Id { get; set; }
    //    public string Type { get; set; }
    //    public int TickValue { get; set; }
    //    public string Status { get; set; } = "Running----";
    //    public override string ToString() => $"Tick of type {Type} dealing {TickValue} damage, Status: {Status}";
    //}
}
