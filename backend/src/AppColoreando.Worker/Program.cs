using AppColoreando.Worker;
using AppColoreando.Application;
using AppColoreando.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<ScheduledPublicationWorker>();

var host = builder.Build();
host.Run();
