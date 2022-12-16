namespace GrainInterfaces;

public interface IHello : IGrainWithStringKey
{
    ValueTask<string> SayHello(string greeting);
    ValueTask<string> ApplyDot(int ticks);
    Task DoTick(long tick);
}