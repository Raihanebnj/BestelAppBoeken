using BestelAppBoeken.Receiver;
using BestelAppBoeken.Receiver.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient<SalesforceClient>();
builder.Services.AddSingleton<SalesforceClient>(); 
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<SalesforcePollingService>();

var host = builder.Build();
host.Run();
