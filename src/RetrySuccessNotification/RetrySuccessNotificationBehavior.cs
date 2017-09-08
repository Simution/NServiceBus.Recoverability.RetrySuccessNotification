namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transport;

    class RetrySuccessNotificationBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public RetrySuccessNotificationBehavior(string endpointName, string notificationAddress, string[] triggerHeaders, bool copyBody)
        {
            this.endpointName = endpointName;
            this.notificationAddress = notificationAddress;
            this.triggerHeaders = triggerHeaders;
            this.copyBody = copyBody;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            if (!context.MessageHeaders.Keys.Intersect(triggerHeaders).Any())
            {
                return;
            }

            var processedMessage = new OutgoingMessage(context.MessageId, new Dictionary<string, string>(context.Message.Headers), copyBody ? context.Message.Body : new byte[0]);

            processedMessage.Headers.Add(Headers.ProcessingEndpoint, endpointName);

            var operations = context.Extensions.Get<PendingTransportOperations>();

            operations.Add(new TransportOperation(processedMessage, new UnicastAddressTag(notificationAddress)));
        }

        readonly string endpointName;
        readonly string notificationAddress;
        readonly string[] triggerHeaders;
        readonly bool copyBody;
    }
}