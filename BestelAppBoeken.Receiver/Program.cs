using BestelAppBoeken.Receiver;
using BestelAppBoeken.Receiver.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient<SalesforceClient>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<SalesforceClient>(); // Register as singleton or transient for worker to use

var host = builder.Build();
host.Run();
