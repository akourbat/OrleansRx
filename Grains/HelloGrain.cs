using DynamicData;
using DynamicData.Binding;
using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Grains;
public class HelloGrain : IGrainBase, IHello, IDisposable
{
    private ISourceCache<DoT, int> _cache;
    public ReadOnlyObservableCollection<string> statusEffects;
    private readonly ILogger _logger;
    private readonly IObservable<Timestamped<Tick>> _ticksObservable;
    private readonly CompositeDisposable _ticksSubscription;

    public IGrainContext GrainContext { get; }

    public event EventHandler<IObservable<Timestamped<Tick>>> TickObservableReceived;

    public HelloGrain(ILogger<HelloGrain> logger, IGrainContext context)
    {
        GrainContext= context;
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
    public Task OnActivateAsync(CancellationToken token)
    {
        _logger.LogInformation("OnActivateAsync");
        var rxScheduler = new TaskPoolScheduler(new TaskFactory(TaskScheduler.Current));

        var ticks = _ticksObservable
                 .ObserveOn(rxScheduler)
                 .Publish();

        var t1sub = ticks.Subscribe(x => _logger.LogInformation($"Tick received {x}, Scheduler: {TaskScheduler.Current}"));

        var t2sub = ticks.Scan(new List<string>(), (seed, tick) =>
        {
            if (tick.Value.Status.Contains("START"))
            {
                seed.Add(tick.Value.Type);
                return seed;
            }
            if (tick.Value.Status.Contains("END"))
            {
                seed.Remove(tick.Value.Type);
                return seed;
            }
            return seed;
        }).Subscribe(list => 
        {
            if (list.Any())
            {
                foreach (var item in list)
                {
                    _logger.LogInformation($"Now Effects have {item}");
                }
            }
            else { _logger.LogInformation($"Now Effects have no effects"); }
            
        });

        ticks.Connect();

        var t2 = _cache.Connect().Transform(x => x.Type).Bind(out statusEffects).Subscribe();

        _ticksSubscription.Add(t1sub);
        _ticksSubscription.Add(t2sub);
        _ticksSubscription.Add(t2);

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

        var dotSequence = Observable.Interval(TimeSpan.FromSeconds(2))
           .Select(t => new Tick(1, dot.Type, dot.TickValue))
           .StartWith(new Tick(2, dot.Type, 0, "START----"))
           .Take(dot.NumberOfTicks)
           //.TakeUntil(Observable.Timer(TimeSpan.FromSeconds(7)))
           .SubscribeOn(ThreadPoolScheduler.Instance)
           .Timestamp()
           .Concat(Observable.Return(new Tick(2, dot.Type, 0, "END----")).Timestamp());
         //.Finally(() => this._cache.Remove(dot));

        //this._cache.AddOrUpdate(dot);

        this.TickObservableReceived?.Invoke(this, dotSequence);

        //var debuff = Observable.Never<Tick>()
        //    .StartWith(new Tick(1, dot.Type, -dot.TickValue, "START---- "))
        //    .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(6)).Amb(Observable.Timer(TimeSpan.FromSeconds(4))))
        //    .Concat(Observable.Return(new Tick(1, dot.Type, dot.TickValue, "END---- ")))
        //    .Timestamp();

        //this.TickObservableReceived?.Invoke(this, debuff);

        return ValueTask.FromResult($"Applying DoT with {dot.NumberOfTicks} ticks.");
    }
    
    public void Dispose() => 
        _ticksSubscription.Dispose();
    
}