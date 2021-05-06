using IotHubClient;
using Spectre.Console.Cli;

var app = new CommandApp<RunClientCommand>();
await app.RunAsync(args);