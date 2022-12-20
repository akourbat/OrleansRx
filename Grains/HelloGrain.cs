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
    private readonly ISubject<IObservable<Timestamped<long>>> _ticksSubjSync; //Sync Subject in case of non-grain usage
    private readonly ISubject<IObservable<Timestamped<long>>> _ticksSubj;
    private CompositeDisposable _ticksSubscription;

    public HelloGrain(ILogger<HelloGrain> logger)
    {
        _logger = logger;
        _ticksSubj = new Subject<IObservable<Timestamped<long>>>();
        _ticksSubjSync = Subject.Synchronize(_ticksSubj);
    }
    public override Task OnActivateAsync(CancellationToken token)
    {
        _logger.LogInformation("OnActivateAsync");
        var rxScheduler = new TaskPoolScheduler(new TaskFactory(TaskScheduler.Current));
        
        _ticksSubscription = new CompositeDisposable(_ticksSubjSync.AsObservable()
            .Synchronize()
            .Merge()
            .Do(e => _logger.LogInformation($"Before ObserveOn() Scheduler: {TaskScheduler.Current}"))
            .ObserveOn(rxScheduler)//.DisableOptimizations(new[] { typeof(ISchedulerLongRunning) }))
            .Subscribe(x => _logger.LogInformation($"Tick received {x}, Scheduler: {TaskScheduler.Current}")));
        
        return Task.CompletedTask;
    }

    ValueTask<string> IHello.SayHello(string greeting)
    {
        _logger.LogInformation("SayHello message received: greeting = '{Greeting}'", greeting);
        return ValueTask.FromResult($"""Client said: '{greeting}', so HelloGrain says: Hello!""");
    }

    ValueTask<string> IHello.ApplyDot(int ticks)
    {
        _logger.LogInformation($"ApplyDot message received: number of ticks to process = {ticks}");

        var dot = Observable.Interval(TimeSpan.FromSeconds(1))
           .Take(ticks)
           .SubscribeOn(ThreadPoolScheduler.Instance) //not really nessesary, as RX will do it anyways for time-based operators like Interval
           .Timestamp();
        _ticksSubjSync.OnNext(dot);

        return ValueTask.FromResult($"Applying DoT with {ticks} ticks.");
    }

    public void Dispose()
    {
        _ticksSubscription.Dispose();
    }
}