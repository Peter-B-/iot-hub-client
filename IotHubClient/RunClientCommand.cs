using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IotHubClient
{
    internal sealed class RunClientCommand : AsyncCommand<RunClientSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, RunClientSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                settings.ConnectionString =
                    AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter [green]IoT device connection string[/]")
                            .PromptStyle("red")
                            .Secret());

            var csBuilder = TryParseConnectionString(settings);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"Host name:             [blue]{csBuilder.HostName}[/]");
            AnsiConsole.MarkupLine($"Device ID:             [blue]{csBuilder.DeviceId}[/]");
            AnsiConsole.MarkupLine($"Authentication method: [blue]{csBuilder.AuthenticationMethod.GetType().Name}[/]");
            AnsiConsole.MarkupLine($"Transport type:        [blue]{settings.TransportType}[/]");
            AnsiConsole.WriteLine();

            var client = DeviceClient.CreateFromConnectionString(settings.ConnectionString);

            var cts = new CancellationTokenSource();
            
            
            var runTask = 
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.BoxBounce)
                    .StartAsync("Running...", async ctx =>
                    {
                        var i = 0;
                        while(!cts.IsCancellationRequested)
                        {
                            AnsiConsole.MarkupLine($"Run [blue]{++i}[/]");
                            await Task.Delay(200);
                        }
                    });

            await MonitorKeypress(cts);
            await runTask;
            
            return 0;
        }

        private static IotHubConnectionStringBuilder TryParseConnectionString(RunClientSettings settings)
        {
            try
            {
                return IotHubConnectionStringBuilder.Create(settings.ConnectionString);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse provided connection string: " + e.Message, e);
            }
        }

        private async Task MonitorKeypress(CancellationTokenSource cts)
        {
            while (true)
            {
                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key is ConsoleKey.Escape or ConsoleKey.Spacebar)
                    {
                        cts.Cancel();
                        return;
                    }

                }
                await Task.Delay(200);
            }
        }
    }
}