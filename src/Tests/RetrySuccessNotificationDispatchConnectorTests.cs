namespace NServiceBus.Recoverability.RetrySucessNotification.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using ObjectBuilder;
    using Pipeline;
    using Transport;

    [TestFixture]
    class RetrySuccessNotificationDispatchConnectorTests : CommonTest
    {
        public async Task Should_Call_Stage_With_HeadersAsync()
        {
            const string endpointName = "test";

            var connector = new RetrySuccessNotificationDispatchConnector(endpointName);

            IRoutingContext routingContext = null;

            var outGoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>
            {
                { ServiceControlRetryHeaders.UniqueMessageId, Guid.NewGuid().ToString() }
            }, FakeMessageBody);

            var auditContext = new FakeAuditContext
            {
                Message = outGoingMessage,
                Extensions = new ContextBag()
            };

            auditContext.Extensions.Set("TestKey", "test");

            await connector.Invoke(auditContext, context =>
            {
                routingContext = context;
                return Task.CompletedTask;
            });

            Assert.IsNotNull(routingContext, "Stage was not called");
            Assert.IsNotNull(routingContext.Message, "Message is null");
            Assert.IsNotNull(routingContext.Message.Headers, "Headers are null");
            Assert.IsTrue(routingContext.Message.Headers.ContainsKey(ServiceControlRetryHeaders.UniqueMessageId), "Header was not copied");
            Assert.AreEqual(outGoingMessage.Headers[ServiceControlRetryHeaders.UniqueMessageId], routingContext.Message.Headers[ServiceControlRetryHeaders.UniqueMessageId], "Header value was not copied");
            Assert.IsTrue(routingContext.Message.Headers.ContainsKey(Headers.ProcessingEndpoint), "Header was not added");
            Assert.AreEqual(endpointName, routingContext.Message.Headers[Headers.ProcessingEndpoint], "Header value is incorrect");
            Assert.AreEqual(outGoingMessage.MessageId, routingContext.Message.MessageId, "Message Id are incorrect");
            Assert.IsNotEmpty(routingContext.RoutingStrategies, "No routing strategies found");
            Assert.AreEqual(1, routingContext.RoutingStrategies.Count, "Incorrect number of routing strategies found");
            var routingStrategy = routingContext.RoutingStrategies.First();

            var addressTag = routingStrategy.Apply(new Dictionary<string, string>());

            Assert.AreEqual(auditContext.AuditAddress, addressTag.ToString(), "Address tag is incorrect");

            string extensionsVal;
            Assert.IsTrue(routingContext.Extensions.TryGet("TestKey", out extensionsVal), "RoutingContext was not created with incoming audit context");
        }

        class FakeAuditContext : IAuditContext
        {
            public ContextBag Extensions { get; set; }
            // ReSharper disable once UnassignedGetOnlyAutoProperty
            public IBuilder Builder { get; }
            public void AddAuditData(string key, string value)
            {
            }

            public OutgoingMessage Message { get; set; }
            public string AuditAddress => "notification";
        }
    }
}
