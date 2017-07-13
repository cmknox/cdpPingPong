using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System.RemoteSystems;

namespace CDPPingPong.Core
{
    public sealed class PingPongMessage
    {
        private string Type { get; set; }
        public string TargetId { get; private set; }
        public TimeSpan RoundTripTime => (DateTime.Now - this.CreationDate);
        public DateTime CreationDate { get; private set; }
        public ValueSet ToValueSet()
        {
            var valueSet = new ValueSet
            {
                ["Type"] = this.Type,
                ["CreationDate"] = this.CreationDate.ToString(CultureInfo.InvariantCulture),
                ["TargetId"] = this.TargetId
            };

            return valueSet;
        }

        public static PingPongMessage CreatePingCommand(RemoteSystem remoteSystem)
        {
            var command = new PingPongMessage
            {
                Type = "ping",
                CreationDate = DateTime.Now,
                TargetId = remoteSystem.Id
            };

            return command;
        }

        public static PingPongMessage CreatePingCommand(AppServiceConnection appService)
        {
            var command = new PingPongMessage
            {
                Type = "ping",
                CreationDate = DateTime.Now,
                TargetId = appService.AppServiceName
            };

            return command;
        }

        public static PingPongMessage CreatePongCommand(PingPongMessage command)
        {
            command.Type = "pong";

            return command;
        }

        private PingPongMessage() {}

        public static PingPongMessage FromValueSet(ValueSet message)
        {
            var command = new PingPongMessage()
            {
                Type = message["Type"] as string,
                CreationDate = DateTime.Parse(message["CreationDate"] as string),
                TargetId = message["TargetId"] as string
            };

            return command;
        }

    }
}
