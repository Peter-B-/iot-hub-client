using System.ComponentModel;
using Microsoft.Azure.Devices.Client;
using Spectre.Console.Cli;

namespace IotHubClient
{
    public class RunClientSettings : CommandSettings
    {
        [Description("Azure IoT device connection string as presented in the Azure portal")]
        [CommandArgument(0, "[connectionString]")]
        public string? ConnectionString { get; set; }

        [Description(
            "TransportType used by the library.\r\nSupported: Amqp, Http1,Amqp_WebSocket_Only, Amqp_Tcp_Only, Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only")]
        [CommandOption("-t|--transportType")]
        [DefaultValue(typeof(TransportType), "Amqp")]
        public TransportType TransportType { get; set; }
    }
}