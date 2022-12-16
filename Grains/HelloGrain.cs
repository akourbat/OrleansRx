using GrainInterfaces;
using Microsoft.Extensions.Logging;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Grains;

public class HelloGrain : Grain, IHello, IDisposable
{
    private readonly ILogger _logger;
    private readonly ISubject<Timestamped<long>> _ticksSubj;
    private readonly IObservable<Timestamped<long>> _ticks;
    private readonly CompositeDisposable _ticksSubscription; 

    public HelloGrain(ILogger<HelloGrain> logger)
    {
        _logger = logger;
        _ticksSubj = new Subject<Timestamped<long>>();
        _ticks = _ticksSubj.AsObservable();
        _ticksSubscription = new CompositeDisposable(_ticks.Subscribe(x => _logger.LogInformation($"Tick received {x}")));
    }
    public override Task OnActivateAsync(CancellationToken token)
    {
        _logger.LogInformation("OnActivateAsync");
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

    Task IHello.DoTick(Timestamped<long> tick)
    {
        _ticksSubj.OnNext(tick);
        return Task.CompletedTask;
    }

    ValueTask<string> IHello.ApplyDot(int ticks)
    {
        _logger.LogInformation($"ApplyDot message received: number of ticks to process = {ticks}");

        var rxScheduler = new TaskPoolScheduler(new TaskFactory(TaskScheduler.Current));

        // NOTE: be sure to dispose any observables before the grain deactivates.
         var dot = Observable.Interval(TimeSpan.FromSeconds(1))
            .Take(ticks)
            .SubscribeOn(ThreadPoolScheduler.Instance)
            .ObserveOn(rxScheduler.DisableOptimizations(new[] { typeof(ISchedulerLongRunning) }))
            .Timestamp()
            .Subscribe(x => _ticksSubj.OnNext(x));

        _ticksSubscription.Add(dot);

        return ValueTask.FromResult($"Applying DoT with {ticks} ticks.");
    }

    public void Dispose()
    {
        _ticksSubscription.Dispose();
    }
}