namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class InvokeRetrySuccessNotificationPipelineBehavior : ForkConnector<IIncomingPhysicalMessageContext, IAuditContext>
    {
        public InvokeRetrySuccessNotificationPipelineBehavior(string notificationAddress, string[] triggerHeaders, bool copyBody)
        {
            this.notificationAddress = notificationAddress;
            this.triggerHeaders = triggerHeaders;
            this.copyBody = copyBody;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next, Func<IAuditContext, Task> fork)
        {
            await next().ConfigureAwait(false);

            await RetrySuccessNotificationInvoke(context, fork);
        }

        protected async Task RetrySuccessNotificationInvoke(IIncomingPhysicalMessageContext context, Func<IAuditContext, Task> fork)
        {
            if (!context.MessageHeaders.Keys.Intersect(triggerHeaders).Any())
            {
                return;
            }

            var processedMessage = new OutgoingMessage(context.MessageId, new Dictionary<string, string>(context.Message.Headers), copyBody ? context.Message.Body : new byte[0]);

            var notificationContext = this.CreateAuditContext(processedMessage, notificationAddress, context);

            await fork(notificationContext).ConfigureAwait(false);
        }

        readonly string notificationAddress;
        readonly string[] triggerHeaders;
        readonly bool copyBody;
    }
}