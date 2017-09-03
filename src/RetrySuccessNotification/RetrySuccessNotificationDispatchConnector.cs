namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    class RetrySuccessNotificationDispatchConnector : StageConnector<IAuditContext, IRoutingContext>
    {
        public override Task Invoke(IAuditContext context, Func<IRoutingContext, Task> stage)
        {
            var dispatchContext = this.CreateRoutingContext(context.Message, new UnicastRoutingStrategy(context.AuditAddress), context);

            return stage(dispatchContext);
        }
    }
}