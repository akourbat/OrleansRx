using Microsoft.Extensions.DependencyModel.Resolution;
using System.Reactive;

namespace GrainInterfaces;

public interface IHello : IGrainWithStringKey
{
    ValueTask<string> SayHello(string greeting);
    ValueTask<string> ApplyDot(DoT dot);
}