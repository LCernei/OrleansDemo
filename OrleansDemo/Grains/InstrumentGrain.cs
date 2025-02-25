using Orleans.BroadcastChannel;

namespace OrleansDemo.Grains;

[GenerateSerializer]
public class PriceUpdate
{
    [Id(0)]
    public string InstrumentId { get; set; }
    [Id(1)]
    public string Price { get; set; }
    [Id(2)]
    public DateTime Timestamp { get; set; }
};

public interface IInstrumentGrain : IGrainWithStringKey
{
    Task SetPrice(string price);
    Task Deactivate();
}

public sealed class InstrumentGrain(
    [PersistentState(stateName: "InstrumentGrain", storageName: "urls")]
    IPersistentState<PriceUpdate> state,
    IBroadcastChannelProvider provider,
    ILogger<InstrumentGrain> logger)
    : Grain, IInstrumentGrain
{
    private IBroadcastChannelWriter<PriceUpdate> channelWriter;

    public async Task SetPrice(string price)
    {
        state.State = new PriceUpdate
        {
            InstrumentId = this.GetPrimaryKeyString(),
            Price = price,
            Timestamp = DateTime.UtcNow,
        };
        await state.WriteStateAsync();
        
        logger.LogInformation("Instrument {GrainId} set price to {Price}", this.GetPrimaryKeyString(), price);
        
        await SendUpdateMessage();
    }
    
    public Task SendUpdateMessage()
    {
        return channelWriter.Publish(state.State);
    }
    
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var channelId = ChannelId.Create("BR", this.GetPrimaryKeyString());
        channelWriter = provider.GetChannelWriter<PriceUpdate>(channelId);
        
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