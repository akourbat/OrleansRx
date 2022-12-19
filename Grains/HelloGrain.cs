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
    private readonly IObservable<Timestamped<long>> _eventSub;
    private readonly CompositeDisposable _ticksSubscription;
    public event EventHandler<IObservable<Timestamped<long>>> TickObservableReceived;

    public HelloGrain(ILogger<HelloGrain> logger)
    {
        _logger = logger;
        
        _eventSub = Observable.FromEventPattern<IObservable<Timestamped<long>>>(
            h => this.TickObservableReceived += h,
            h => this.TickObservableReceived -= h)
            .Synchronize()
            .Select(x => x.EventArgs)
            .Merge();
        
        _ticksSubscription = new CompositeDisposable();
    }
    public override Task OnActivateAsync(CancellationToken token)
    {
        _logger.LogInformation("OnActivateAsync");
        var rxScheduler = new TaskPoolScheduler(new TaskFactory(TaskScheduler.Current));
        var t1 = _eventSub
                 .ObserveOn(rxScheduler)
                 .Subscribe(x => _logger.LogInformation($"Tick received {x}"));
        _ticksSubscription.Add(t1);

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
    
    ValueTask<string> IHello.ApplyDot(int ticks)
    {
        _logger.LogInformation($"ApplyDot message received: number of ticks to process = {ticks}");

        var dot = Observable.Interval(TimeSpan.FromSeconds(1))
           .Take(ticks)
           .SubscribeOn(ThreadPoolScheduler.Instance)
           .Timestamp();

        this.TickObservableReceived?.Invoke(this, dot);        

        return ValueTask.FromResult($"Applying DoT with {ticks} ticks.");
    }

    public void Dispose()
    {
        _ticksSubscription.Dispose();
    }
}