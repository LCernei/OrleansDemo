using Orleans.BroadcastChannel;
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
    siloBuilder.AddBroadcastChannel("BR");
    siloBuilder.UseDashboard();
    siloBuilder.Services.AddSingleton<IBroadcastChannelProvider>(sp =>
    {
        var clusterClient = sp.GetRequiredService<IClusterClient>();
        return clusterClient.GetBroadcastChannelProvider("BR");
    });
    siloBuilder.Services.AddKeyedSingleton<IChannelIdMapper, OtherCellMapper>(OtherCellMapper.Name);
    siloBuilder.Services.AddKeyedSingleton<IChannelIdMapper, CellMapper>(CellMapper.Name);
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

