namespace NServiceBus
{
    using System;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Unicast;

    class RetrySuccessNotificationBehavior : IBehavior<IncomingContext>
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public Address NotificationAddress { get; set; }
        public string ProcessingEndpointName { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global        
        public ISendMessages MessageSender { get; set; }        
        public string[] TriggerHeaders { get; set; }
        public bool CopyBody { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global

        public void Invoke(IncomingContext context, Action next)
        {
            next();

            if (!context.PhysicalMessage.Headers.Keys.Intersect(TriggerHeaders).Any())
            {
                return;
            }

            var messageToForward = new TransportMessage(context.PhysicalMessage.Id, context.PhysicalMessage.Headers)
            {
                Body = CopyBody ? context.PhysicalMessage.Body : new byte[0],
                Recoverable = context.PhysicalMessage.Recoverable
            };

            messageToForward.Headers[Headers.ProcessingEndpoint] = ProcessingEndpointName;

            MessageSender.Send(messageToForward, new SendOptions(NotificationAddress));
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("RetrySuccessNotification", typeof(RetrySuccessNotificationBehavior), "Dispatches retry success notifications to the transport")
            {
                InsertBefore("ProcessingStatistics");
            }
        }
    }
}
