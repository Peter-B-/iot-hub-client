using System;
using System.Text;
using IotHubClient;
using Spectre.Console.Cli;

Console.OutputEncoding = Encoding.Unicode;
Console.InputEncoding = Encoding.Unicode;
var app = new CommandApp<RunClientCommand>();
await app.RunAsync(args);