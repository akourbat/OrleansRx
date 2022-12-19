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
    //private readonly ISubject<IObservable<Timestamped<long>>> _ticksSubj;
    private ISubject<IObservable<Timestamped<long>>> _ticksSubjSync;
   // private readonly IObservable<IObservable<Timestamped<long>>> _ticks;
    private CompositeDisposable _ticksSubscription;

    public HelloGrain(ILogger<HelloGrain> logger)
    {
        _logger = logger;
       // _ticksSubj = new Subject<IObservable<Timestamped<long>>>();
       // _ticks = _ticksSubj.AsObservable();
       
    }
    public override Task OnActivateAsync(CancellationToken token)
    {
        _logger.LogInformation("OnActivateAsync");
        var rxScheduler = new TaskPoolScheduler(new TaskFactory(TaskScheduler.Current));
        _ticksSubjSync = Subject.Synchronize(new Subject<IObservable<Timestamped<long>>>(), rxScheduler);
        _ticksSubscription = new CompositeDisposable(_ticksSubjSync.Merge().Subscribe(x => _logger.LogInformation($"Tick received {x}")));//Now thread-safe, serial messages downstream on RXScheduler
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
        //_ticksSubj.OnNext(tick);
        return Task.CompletedTask;
    }

    ValueTask<string> IHello.ApplyDot(int ticks)
    {
        _logger.LogInformation($"ApplyDot message received: number of ticks to process = {ticks}");

        var dot = Observable.Interval(TimeSpan.FromSeconds(1))
           .Take(ticks)
           .SubscribeOn(ThreadPoolScheduler.Instance)
           .Timestamp();
        _ticksSubjSync.OnNext(dot);

        return ValueTask.FromResult($"Applying DoT with {ticks} ticks.");
    }

    public void Dispose()
    {
        _ticksSubscription.Dispose();
    }
}