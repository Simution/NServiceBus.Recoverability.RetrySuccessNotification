namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class InvokeAuditAndRetrySucessNotificationPipelineBehavior : InvokeRetrySuccessNotificationPipelineBehavior
    {
        public InvokeAuditAndRetrySucessNotificationPipelineBehavior(string notificationAddress, string[] triggerHeaders, bool copyBody, string auditAddress) : base(notificationAddress, triggerHeaders, copyBody)
        {
            this.auditAddress = auditAddress;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next, Func<IAuditContext, Task> fork)
        {
            await next().ConfigureAwait(false);

            context.Message.RevertToOriginalBodyIfNeededUsingReflection();

            var processedMessage = new OutgoingMessage(context.Message.MessageId, new Dictionary<string, string>(context.Message.Headers), context.Message.Body);

            var auditContext = this.CreateAuditContext(processedMessage, auditAddress, context);

            await fork(auditContext).ConfigureAwait(false);

            await RetrySuccessNotificationInvoke(context, fork);
        }

        string auditAddress;    
    }
}