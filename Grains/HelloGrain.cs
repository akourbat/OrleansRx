using DynamicData;
using DynamicData.Binding;
using GrainInterfaces;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Grains;
public class HelloGrain : Grain, IHello, IDisposable
{
    private ISourceCache<DoT, int> _cache;
    public ReadOnlyObservableCollection<string> statusEffects;
    private readonly ILogger _logger;
    private readonly IObservable<Timestamped<Tick>> _ticksObservable;
    private readonly CompositeDisposable _ticksSubscription;
    public event EventHandler<IObservable<Timestamped<Tick>>> TickObservableReceived;

    public HelloGrain(ILogger<HelloGrain> logger)
    {
        _logger = logger;
        _cache = new SourceCache<DoT, int>(dot => dot.Id);

        _ticksObservable = Observable.FromEventPattern<IObservable<Timestamped<Tick>>>(
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
        var t1 = _ticksObservable
                 .ObserveOn(rxScheduler)
                 .Subscribe(x => _logger.LogInformation($"Tick received {x}, Scheduler: {TaskScheduler.Current}"));


        var t2 = _cache.Connect().Transform(x => x.Type).Bind(out statusEffects)
            .Subscribe();

        var t3 = statusEffects.ObserveCollectionChanges()
            //.Where(e => e.EventArgs.Action == NotifyCollectionChangedAction.Add ||
            //    e.EventArgs.Action == NotifyCollectionChangedAction.Remove)
               .Subscribe(t => _logger.LogInformation($"Something Changed in the effects collection! Scheduler: {TaskScheduler.Current}"));

        _ticksSubscription.Add(t1);
        _ticksSubscription.Add(t2);
        _ticksSubscription.Add(t3);

        return Task.CompletedTask;
    }

    ValueTask<string> IHello.SayHello(string greeting)
    {
        _logger.LogInformation("SayHello message received: greeting = '{Greeting}'", greeting);
    
        return ValueTask.FromResult($"""Client said: '{greeting}', so HelloGrain says: Hello!""");
    }

    ValueTask<string> IHello.ApplyDot(DoT dot)
    {
        _logger.LogInformation($"ApplyDot message received: number of ticks to process = {dot.NumberOfTicks}");

        //var dotSequence = Observable.Interval(TimeSpan.FromSeconds(2))
        //   .Select(t => new Tick { Type = dot.Type, TickValue = dot.TickValue })
        //   .StartWith(new Tick { Type = dot.Type, Status = "START----" })
        //   .Take(dot.NumberOfTicks)
        //   .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(7)))
        //   .SubscribeOn(ThreadPoolScheduler.Instance)
        //   .Timestamp()
        //   .Concat(Observable.Return(new Tick { Type = dot.Type, Status = "END----" }).Timestamp())
        //   .Finally(() => this._cache.Remove(dot));

        //this._cache.AddOrUpdate(dot);
        //this.TickObservableReceived?.Invoke(this, dotSequence);

        var debuff = Observable.Never<Tick>()
            .StartWith(new Tick { Type = dot.Type, TickValue = -dot.TickValue, Status = "START---- "})
            .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(6)).Amb(Observable.Timer(TimeSpan.FromSeconds(4))))
            .Concat(Observable.Return(new Tick { Type = dot.Type, TickValue = dot.TickValue, Status = "END---- " }))
            .Timestamp();

        this.TickObservableReceived?.Invoke(this, debuff);

        return ValueTask.FromResult($"Applying DoT with {dot.NumberOfTicks} ticks.");
    }
    
    public void Dispose() => 
        _ticksSubscription.Dispose();
    
}