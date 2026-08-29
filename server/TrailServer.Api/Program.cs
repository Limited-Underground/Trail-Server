using TrailServer.Api.Configuration;
using TrailServer.Api.Radio;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<TrailServerOptions>()
    .Bind(builder.Configuration.GetSection(TrailServerOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IServerRadioStatus, UnavailableServerRadioStatus>();

var app = builder.Build();

app.MapGet("/api/health", (IServerRadioStatus radio, Microsoft.Extensions.Options.IOptions<TrailServerOptions> options) =>
{
    var radioStatus = radio.GetStatus();

    return Results.Ok(new
    {
        service = "limited-underground-trail-server",
        instance = options.Value.InstanceName,
        status = "host-ready",
        operational = false,
        radio = new
        {
            status = radioStatus.Availability,
            reason = radioStatus.Reason,
        },
    });
});

app.Run();
