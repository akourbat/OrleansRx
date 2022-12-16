using GrainInterfaces;
using Microsoft.Extensions.Logging;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Grains;

public class HelloGrain : Grain, IHello
{
    private readonly ILogger _logger;
    private ISubject<long> _ticksSubj;
    public IObservable<long> Ticks => _ticksSubj.AsObservable();

    public HelloGrain(ILogger<HelloGrain> logger)
    {
        _logger = logger;
        _ticksSubj = new Subject<long>();
    }
    public override Task OnActivateAsync(CancellationToken token)
    {
        _logger.LogInformation("OnActivateAsync");
        Ticks.Subscribe(x => _logger.LogInformation($"Tick received {x}"));
        return Task.CompletedTask;
    }

    ValueTask<string> IHello.SayHello(string greeting)
    {
        _logger.LogInformation(
            "SayHello message received: greeting = '{Greeting}'", greeting);

        return ValueTask.FromResult(
            $"""
            Client said: '{greeting}', so HelloGrain says: Hello!
            """);
    }

    Task IHello.DoTick(long tick)
    {
        _ticksSubj.OnNext(tick);
        return Task.CompletedTask;
    }

    ValueTask<string> IHello.ApplyDot(int ticks)
    {
        _logger.LogInformation($"ApplyDot message received: number of ticks to process = {ticks}");

        var rxScheduler = new TaskPoolScheduler(new TaskFactory(TaskScheduler.Current));

        Observable.Interval(TimeSpan.FromSeconds(2))
            .Take(ticks)
            .SubscribeOn(ThreadPoolScheduler.Instance)
            .ObserveOn(rxScheduler)
            .Subscribe(x => _ticksSubj.OnNext(x));
            //.Subscribe(async x => await this.AsReference<IHello>().DoTick(x));

        return ValueTask.FromResult($"Applying DoT with {ticks} ticks.");
    }
}