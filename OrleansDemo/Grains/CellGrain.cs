namespace OrleansDemo.Grains;

public interface IChat : IGrainObserver
{
    Task ReceiveMessage(string message);
}

public interface ICellGrain: IGrainWithStringKey, IChat
{
    Task SetValue(string value);
    Task Deactivate();
}

public sealed class CellGrain(
    [PersistentState(stateName: "CellGrain", storageName: "urls")]
    IPersistentState<CellState> state,
    ILogger<CellGrain> logger)
    : Grain, ICellGrain
{
    public async Task SetValue(string value)
    {
        state.State.Value= value;
        await state.WriteStateAsync();
        logger.LogInformation("Cell {GrainId} set value to {Value}", this.GetPrimaryKeyString(), value);
    }

    public Task ReceiveMessage(string message)
    {
        return SetValue(message);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Cell {GrainId} activated", this.GetPrimaryKeyString());
        return base.OnActivateAsync(cancellationToken);
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

[GenerateSerializer, Alias(nameof(CellState))]
public sealed record class CellState
{
    [Id(0)]
    public string Value { get; set; } = "";
}