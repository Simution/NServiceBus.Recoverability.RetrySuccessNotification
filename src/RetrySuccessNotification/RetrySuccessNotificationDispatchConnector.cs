namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    class RetrySuccessNotificationDispatchConnector : StageConnector<IAuditContext, IRoutingContext>
    {
        public RetrySuccessNotificationDispatchConnector(string endpointName)
        {
            this.endpointName = endpointName;
        }

        public override Task Invoke(IAuditContext context, Func<IRoutingContext, Task> stage)
        {
            context.Message.Headers.Add(Headers.ProcessingEndpoint, endpointName);

            var dispatchContext = this.CreateRoutingContext(context.Message, new UnicastRoutingStrategy(context.AuditAddress), context);

            return stage(dispatchContext);
        }

        string endpointName;
    }
}