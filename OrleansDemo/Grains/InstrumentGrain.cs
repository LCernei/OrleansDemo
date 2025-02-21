using Orleans.Utilities;

namespace OrleansDemo.Grains;

public interface IInstrumentGrain : IGrainWithStringKey
{
    Task SetPrice(string price);
    Task Subscribe(IChat observer);
    Task UnSubscribe(IChat observer);
    Task Deactivate();
}

public sealed class InstrumentGrain(
    [PersistentState(stateName: "InstrumentGrain", storageName: "urls")]
    IPersistentState<UrlDetails> state,
    ILogger<InstrumentGrain> logger)
    : Grain, IInstrumentGrain
{
    private readonly ObserverManager<IChat> subsManager = new(TimeSpan.FromSeconds(30), logger);

    public async Task SetPrice(string price)
    {
        state.State.Price = price;
        await state.WriteStateAsync();
        
        logger.LogInformation("Instrument {GrainId} set price to {Price}", this.GetPrimaryKeyString(), price);
        
        await SendUpdateMessage(price);
    }
    
    // Clients call this to subscribe.
    public Task Subscribe(IChat observer)
    {
        subsManager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    //Clients use this to unsubscribe and no longer receive messages.
    public Task UnSubscribe(IChat observer)
    {
        subsManager.Unsubscribe(observer);

        return Task.CompletedTask;
    }
    
    public Task SendUpdateMessage(string message)
    {
        return subsManager.Notify(s => s.ReceiveMessage(message));
    }
    
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Instrument {GrainId} activated", this.GetPrimaryKeyString());
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        logger.LogInformation("Instrument {GrainId} deactivated", this.GetPrimaryKeyString());
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    public Task Deactivate()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }
}

[GenerateSerializer, Alias(nameof(UrlDetails))]
public sealed record class UrlDetails
{
    [Id(0)]
    public string Price { get; set; } = "";
}