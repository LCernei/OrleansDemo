using OrleansDemo.Grains;
using Serilog;
using Serilog.Filters;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Services.AddSerilog(config =>
    config.Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore"))
        .WriteTo.Console());

builder.Host.UseOrleans(static siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("urls");
    siloBuilder.UseDashboard();
    siloBuilder.AddMemoryStreams("STR").AddMemoryGrainStorage("STR");
    siloBuilder.AddStartupTask((sp, ct) =>
    {
        var grains = sp.GetRequiredService<IGrainFactory>();
        var cell = grains.GetGrain<ICellGrain>("mid7");

        return cell.Activate();
    });
});

var app = builder.Build();

app.MapGet("/update",
    static async (IGrainFactory grains, HttpRequest request, string instrument) =>
    {
        var instrumentGrain = grains.GetGrain<IInstrumentGrain>(instrument);

        await instrumentGrain.SetPrice(Guid.NewGuid().GetHashCode().ToString());
        
        return Results.Ok();
    });

app.MapGet("/kill",
    static async (IGrainFactory grains, HttpRequest request, string? cell, string? instrument) =>
    {
        if (cell is not null)
            await grains.GetGrain<ICellGrain>(cell).Deactivate();
        
        if (instrument is not null)
            await grains.GetGrain<IInstrumentGrain>(instrument).Deactivate();
        
        return Results.Ok();
    });
app.Run();

