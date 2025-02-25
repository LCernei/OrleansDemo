using Orleans.BroadcastChannel;
using Orleans.Metadata;
using OrleansDemo.Grains;

public interface IOtherCellGrain : IGrainWithStringKey
{
    Task Activate();
    Task Deactivate();
}

public class OtherCellMapper : IChannelIdMapper
{
    public const string Name = "OtherCellMapper";

    public IdSpan GetGrainKeyId(GrainBindings grainBindings, ChannelId streamId)
    {
        return IdSpan.Create("othermid" + streamId.GetKeyAsString());
    }
}

[ImplicitChannelSubscription("BR", OtherCellMapper.Name)]
public class OtherCellGrain([PersistentState("OtherCell", "urls")] IPersistentState<PriceUpdate> state, ILogger<CellGrain> logger) : Grain, IOtherCellGrain, IOnBroadcastChannelSubscribed
{
    public Task Activate() => Task.CompletedTask;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("OtherCell {GrainId} activated", this.GetPrimaryKeyString());

        await base.OnActivateAsync(cancellationToken);
    }
    
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        logger.LogInformation("OtherCell {GrainId} deactivated", this.GetPrimaryKeyString());
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    public Task Deactivate()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }

    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        return streamSubscription.Attach<PriceUpdate>(OnPriceUpdated, OnError);
    }
    
    private async Task OnPriceUpdated(PriceUpdate update)
    {
        state.State = update;
        await state.WriteStateAsync();
        logger.LogInformation("OtherCell {GrainId} set value to {Value}", this.GetPrimaryKeyString(), state.State.Price);
    }

    private Task OnError(Exception ex)
    {
        logger.LogError(ex, "OnError");

        return Task.CompletedTask;
    }
}
