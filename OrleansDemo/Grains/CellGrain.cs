using Orleans.BroadcastChannel;
using Orleans.Metadata;
using OrleansDemo.Grains;

public interface ICellGrain : IGrainWithStringKey
{
    Task Activate();
    Task Deactivate();
}

public class CellMapper : IChannelIdMapper
{
    public const string Name = "CellMapper";

    public IdSpan GetGrainKeyId(GrainBindings grainBindings, ChannelId streamId)
    {
        return IdSpan.Create("mid" + streamId.GetKeyAsString());
    }
}

[ImplicitChannelSubscription("BR", CellMapper.Name)]
public class CellGrain([PersistentState("CellGrain", "urls")] IPersistentState<PriceUpdate> state, ILogger<CellGrain> logger) : Grain, ICellGrain, IOnBroadcastChannelSubscribed
{
    public Task Activate() => Task.CompletedTask;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Cell {GrainId} activated", this.GetPrimaryKeyString());

        await base.OnActivateAsync(cancellationToken);
    }
    
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        logger.LogInformation("Cell {GrainId} deactivated", this.GetPrimaryKeyString());
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
        logger.LogInformation("Cell {GrainId} set value to {Value}", this.GetPrimaryKeyString(), state.State.Price);
    }

    private Task OnError(Exception ex)
    {
        logger.LogError(ex, "OnError");

        return Task.CompletedTask;
    }
}
