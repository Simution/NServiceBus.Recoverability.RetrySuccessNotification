namespace NServiceBus.Recoverability.RetrySucessNotification.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Pipeline;
    using Transport;

    [TestFixture]
    class InvokeAuditAndRetrySuccessNotificationPipelineBehaviorTests : CommonTest
    {
        const string testHeader = "TestKey";

        [Test]
        public async Task Audits_Message_Without_Notification_When_TriggerHeaders_Are_Absent()
        {
            const string auditAddress = "audit";

            var behavior = new InvokeAuditAndRetrySucessNotificationPipelineBehavior(null, TestHeaderKeys, false, auditAddress);

            var nextWasCalled = false;

            var auditContexts = new List<IAuditContext>();

            var messageId = Guid.NewGuid().ToString();

            var header = new KeyValuePair<string, string>(testHeader, "test");

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
                auditContexts.Add(context);
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            Assert.IsTrue(nextWasCalled, "next was not called");
            Assert.AreEqual(1, auditContexts.Count, "Wrong number of audit contexts");

            var auditContext = auditContexts.First();

            Assert.AreEqual(auditAddress, auditContext.AuditAddress, "Audit address does not match");
            Assert.IsNotNull(auditContext.Message, "AuditContext message is null");
            Assert.AreEqual(messageId, auditContext.Message.MessageId, "Message Ids do not match");
            Assert.IsTrue(auditContext.Message.Headers.ContainsKey(header.Key), "Header not found");
            Assert.AreEqual(header.Value, auditContext.Message.Headers[header.Key], "Header value does not match");
            Assert.AreEqual(FakeMessageBody, auditContext.Message.Body, "Body was not copied when it should have been.");
            string extensionsVal;
            Assert.IsTrue(auditContext.Extensions.TryGet(FakeIncomingPhysicalMessageContext.TestKey, out extensionsVal), "AuditContext was not created with incoming physical context");
        }

        [Test]
        public async Task Audits_Message_With_Notification_When_TriggerHeaders_Are_Present()
        {
            const string notificationAddress = "notification";
            const string auditAddress = "audit";
            const string testHeaderVal = "test";

            var behavior = new InvokeAuditAndRetrySucessNotificationPipelineBehavior(notificationAddress, TestHeaderKeys, false, auditAddress);

            var nextWasCalled = false;

            var auditContexts = new List<IAuditContext>();

            var messageId = Guid.NewGuid().ToString();

            var incomingContext = new FakeIncomingPhysicalMessageContext(new IncomingMessage(messageId, new Dictionary<string, string>
            {
                {ServiceControlRetryHeaders.UniqueMessageId, testHeaderVal}
            }, FakeMessageBody));

            await behavior.Invoke(incomingContext, () =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            }, context =>
            {
                auditContexts.Add(context);
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            Assert.IsTrue(nextWasCalled, "next was not called");
            Assert.AreEqual(2, auditContexts.Count, "Wrong number of audit contexts");

            var auditContext = auditContexts.First();

            Assert.AreEqual(auditAddress, auditContext.AuditAddress, "Audit address does not match");
            Assert.IsNotNull(auditContext.Message, "Audit AuditContext message is null");
            Assert.AreEqual(messageId, auditContext.Message.MessageId, "Audit message Ids do not match");
            Assert.IsTrue(auditContext.Message.Headers.ContainsKey(ServiceControlRetryHeaders.UniqueMessageId), "Audit header not found");
            Assert.AreEqual(testHeaderVal, auditContext.Message.Headers[ServiceControlRetryHeaders.UniqueMessageId], "Audit header value does not match");
            Assert.AreEqual(FakeMessageBody, auditContext.Message.Body, "Audit body was not copied when it should have been.");
            string extensionsVal;
            Assert.IsTrue(auditContext.Extensions.TryGet(FakeIncomingPhysicalMessageContext.TestKey, out extensionsVal), "Audit AuditContext was not created with incoming physical context");

            auditContext = auditContexts.Last();

            Assert.AreEqual(notificationAddress, auditContext.AuditAddress, "Notificaiton address does not match");
            Assert.IsNotNull(auditContext.Message, "Notification AuditContext message is null");
            Assert.AreEqual(messageId, auditContext.Message.MessageId, "Notification message Ids do not match");
            Assert.IsTrue(auditContext.Message.Headers.ContainsKey(ServiceControlRetryHeaders.UniqueMessageId), "Notification header not found");
            Assert.AreEqual(testHeaderVal, auditContext.Message.Headers[ServiceControlRetryHeaders.UniqueMessageId], "Noticication header value does not match");
            Assert.AreNotEqual(FakeMessageBody, auditContext.Message.Body, "Notification body was copied when it should not have been.");

            Assert.IsTrue(auditContext.Extensions.TryGet(FakeIncomingPhysicalMessageContext.TestKey, out extensionsVal), "AuditContext was not created with incoming physical context");
        }
    }
}