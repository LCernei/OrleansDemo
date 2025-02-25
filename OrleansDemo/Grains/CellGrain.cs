using Orleans;
using Orleans.Streams;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;
using OrleansDemo.Grains;

public interface ICellGrain : IGrainWithStringKey
{
    Task Activate();
    Task Deactivate();
}

public class CellGrain([PersistentState("CellGrain", "urls")] IPersistentState<PriceUpdate> state, ILogger<CellGrain> logger) : Grain, ICellGrain
{
    public Task Activate() => Task.CompletedTask;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var instr = this.GetPrimaryKeyString().Replace("mid", "");
        var streamId = StreamId.Create("STRN", instr);
        
        var streamProvider = this.GetStreamProvider("STR");
        var stream = streamProvider.GetStream<PriceUpdate>(streamId);

        var handles = await stream.GetAllSubscriptionHandles();
        var subscription = handles.FirstOrDefault(x => x.StreamId == streamId);
        if (subscription is null)
        {
            await stream.SubscribeAsync(OnPriceUpdateReceived);
        }
        else
        {
            await subscription.ResumeAsync(OnPriceUpdateReceived);
        }

        logger.LogInformation("Cell {GrainId} activated", this.GetPrimaryKeyString());

        await base.OnActivateAsync(cancellationToken);
    }

    private async Task OnPriceUpdateReceived(PriceUpdate update, StreamSequenceToken? token)
    {
        state.State = update;
        await state.WriteStateAsync();
        logger.LogInformation("Cell {GrainId} set value to {Value}", this.GetPrimaryKeyString(), state.State.Price);
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
}
