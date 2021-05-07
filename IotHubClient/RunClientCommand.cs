using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IotHubClient
{
    internal sealed class RunClientCommand : AsyncCommand<RunClientSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, RunClientSettings settings)
        {
            AnsiConsole.Clear();

            InitSettings(settings);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Press [green]SPACE[/] to trigger a message or [orange3]ESC[/] to exit.");
            AnsiConsole.WriteLine();

            await RunCommand(settings);

            Goodbye();

            return 0;
        }

        private static void InitSettings(RunClientSettings settings)
        {
            AnsiConsole.WriteLine();
            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                settings.ConnectionString =
                    AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter [green]IoT device connection string[/]")
                            .PromptStyle("deepskyblue1")
                            .Secret());

            var csBuilder = TryParseConnectionString(settings);

            var table = new Table();

            table.AddColumn("Parameter");
            table.AddColumn(new TableColumn("Value"));

            table.AddRow("Host name", csBuilder.HostName);
            table.AddRow("Device ID", csBuilder.DeviceId);
            table.AddRow("Authentication method", csBuilder.AuthenticationMethod.GetType().Name);
            table.AddRow("Transport type", settings.TransportType.ToString());
            table.AddRow("Inter message delay", settings.InterMessageDelay.ToString());

            table.Border(TableBorder.Rounded);

            AnsiConsole.Render(table);
        }

        private static void Goodbye()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("   " + Emoji.Known.GrowingHeart + "  [orange3]THANK YOU FOR PARTICIPATING IN THIS ENRICHMENT CENTER ACTIVITY[/]  " +
                                   Emoji.Known.BirthdayCake);
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
        }

        private async Task RunCommand(RunClientSettings settings)
        {
            using var client = DeviceClient.CreateFromConnectionString(settings.ConnectionString);
            using var cts = new CancellationTokenSource();
            using var manualTriggerSubject = new Subject<Unit>();

            var sendMessagesTask =
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.BouncingBar)
                    .StartAsync("Running...", _ => SendMessages(client, settings.InterMessageDelay, manualTriggerSubject, cts.Token));

            try
            {
                await MonitorUserInput(cts, manualTriggerSubject);
                await sendMessagesTask;
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }

        private static async Task SendMessages(DeviceClient client, TimeSpan interMessageDelay, IObservable<Unit> manualTrigger, CancellationToken token)
        {
            var i = 0;
            while (!token.IsCancellationRequested)
            {
                var logMessage = $"Sending message [deepskyblue1]{++i,3}[/]";
                AnsiConsole.MarkupLine(logMessage);
                try
                {
                    await DoSendMessage(client, i, token);

                    AnsiConsole.Cursor.Move(CursorDirection.Up, 1);
                    AnsiConsole.MarkupLine(logMessage + " [green]Done[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.Cursor.Move(CursorDirection.Up, 1);
                    AnsiConsole.MarkupLine(logMessage + " [red]Failed[/]");

                    AnsiConsole.WriteException(ex,
                        ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes |
                        ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
                }

                await
                    manualTrigger.Merge(Observable.Timer(interMessageDelay).Select(_ => Unit.Default))
                        .FirstAsync()
                        .ToTask(token);
            }
        }

        private static async Task DoSendMessage(DeviceClient client, int messageNo, CancellationToken token)
        {
            var message = CreateMessage(messageNo);

            await client.SendEventAsync(message, token);
        }

        private static Message? CreateMessage(int messageNo)
        {
            var payload = new
            {
                MessageNo = messageNo,
                TimeStamp = DateTimeOffset.Now,
                Environment.MachineName
            };

            var json = JsonConvert.SerializeObject(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            var message = new Message(bytes);
            return message;
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

        private async Task MonitorUserInput(CancellationTokenSource cts, IObserver<Unit> manualTrigger)
        {
            while (true)
            {
                while (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key is ConsoleKey.Escape)
                    {
                        cts.Cancel();
                        manualTrigger.OnCompleted();
                        return;
                    }

                    if (keyInfo.Key is ConsoleKey.Spacebar)
                        manualTrigger.OnNext(Unit.Default);
                }

                await Task.Delay(200);
            }
        }
    }
}