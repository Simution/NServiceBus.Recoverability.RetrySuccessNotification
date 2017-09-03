namespace NServiceBus.Recoverability.RetrySucessNotification.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Pipeline;
    using Transport;

    [TestFixture]
    class InvokeRetrySuccessNotificationPipelineBehaviorTests : CommonTest
    {
        [Test]
        public async Task Does_Not_Fork_When_Headers_Are_Absent()
        {
            var behavior = new InvokeRetrySuccessNotificationPipelineBehavior(null, TestHeaderKeys, false);

            var nextWasCalled = false;
            var forkWasCalled = false;

            await behavior.Invoke(new FakeIncomingPhysicalMessageContext(new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0])), () =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            }, context =>
            {
                forkWasCalled = true;
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            Assert.IsTrue(nextWasCalled, "next was not called");
            Assert.IsFalse(forkWasCalled, "fork was called");
        }

        [Test]
        public async Task Forks_When_Headers_Are_Present()
        {
            const string testAddress = "testAddress";

            var behavior = new InvokeRetrySuccessNotificationPipelineBehavior(testAddress, TestHeaderKeys, false);

            var nextWasCalled = false;
            var forkWasCalled = false;
            IAuditContext auditContext = null;

            var messageId = Guid.NewGuid().ToString();

            var header = new KeyValuePair<string, string>(ServiceControlRetryHeaders.UniqueMessageId, "test");

            var incomingContext = new FakeIncomingPhysicalMessageContext(new IncomingMessage(messageId, new Dictionary<string, string>()
            {
                {header.Key, header.Value}
            }, FakeMessageBody));

            await behavior.Invoke(incomingContext, () =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            }, context =>
            {
                forkWasCalled = true;
                auditContext = context;
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            Assert.IsTrue(nextWasCalled, "next was not called");
            Assert.IsTrue(forkWasCalled, "fork was not called");
            Assert.IsNotNull(auditContext, "Audit Context is null");
            Assert.AreEqual(testAddress, auditContext.AuditAddress, "Audit address does not match");
            Assert.IsNotNull(auditContext.Message, "AuditContext message is null");
            Assert.AreEqual(messageId, auditContext.Message.MessageId, "Message Ids do not match");
            Assert.IsTrue(auditContext.Message.Headers.ContainsKey(header.Key), "Header not found");
            Assert.AreEqual(header.Value, auditContext.Message.Headers[header.Key], "Header value does not match");
            Assert.AreNotEqual(FakeMessageBody, auditContext.Message.Body, "Body was copied when it should not have been");
            Assert.IsTrue(auditContext.Extensions.ContainsKey<string>(FakeIncomingPhysicalMessageContext.TestKey), "AuditContext was not created with incoming physical context");
        }

        [Test]
        public async Task Copies_Body_When_Configured()
        {
            const string testAddress = "testAddress";

            var behavior = new InvokeRetrySuccessNotificationPipelineBehavior(testAddress, TestHeaderKeys, true);

            var nextWasCalled = false;
            var forkWasCalled = false;
            IAuditContext auditContext = null;

            var messageId = Guid.NewGuid().ToString();

            var header = new KeyValuePair<string, string>(ServiceControlRetryHeaders.UniqueMessageId, "test");

            await behavior.Invoke(new FakeIncomingPhysicalMessageContext(new IncomingMessage(messageId, new Dictionary<string, string>
            {
                {header.Key, header.Value}
            }, FakeMessageBody)),() =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            }, context =>
            {
                forkWasCalled = true;
                auditContext = context;
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            Assert.IsTrue(nextWasCalled, "next was not called");
            Assert.IsTrue(forkWasCalled, "fork was not called");
            Assert.IsNotNull(auditContext, "Audit Context is null");
            Assert.IsNotNull(auditContext.Message, "AuditContext message is null");
            Assert.AreEqual(FakeMessageBody, auditContext.Message.Body, "Body was not copied when it should have been");
        }
    }
}
