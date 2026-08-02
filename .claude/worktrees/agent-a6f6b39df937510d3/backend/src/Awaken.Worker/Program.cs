using Awaken.Application.Common.Behaviors;
using Awaken.Infrastructure;
using Awaken.Infrastructure.Jobs;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Awaken.Worker"));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfireServer(options =>
{
    options.Queues = HangfireRecurringJobRegistration.ServerQueues;
});

var applicationAssembly = typeof(Awaken.Application.AssemblyReference).Assembly;
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
builder.Services.AddValidatorsFromAssembly(applicationAssembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

var host = builder.Build();

var recurringJobManager = host.Services.GetRequiredService<IRecurringJobManager>();
HangfireRecurringJobRegistration.Register(recurringJobManager);

await host.RunAsync();
