namespace NServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus;
    using Pipeline.Contexts;
    using Transports;
    using Unicast;
    using NUnit.Framework;

    [TestFixture]
    class RetrySuccessNotificationBehaviorTests
    {
        const string TestKey = "Test";
        static byte[] FakeMessageBody = { 0x20 };

        static string[] TestHeaderKeys = {
            TestKey
        };

        [Test]
        public void Does_Not_Send_Notification_When_Headers_Are_Absent()
        {
            var messageSender = new FakeMessageSender();
            var behavior = new RetrySuccessNotificationBehavior
            {
                MessageSender = messageSender,
                TriggerHeaders = TestHeaderKeys
            };

            var incomingMessage = new TransportMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>())
            {
                Body = new byte[0],
                Recoverable = true
            };

            var context = new IncomingContext(null, incomingMessage);

            behavior.Invoke(context, () => { });

            Assert.IsFalse(messageSender.WasSent,"Message was sent");
        }

        [Test]
        public void Sends_Notification_When_Valid_Header_Is_Present()
        {
            var messageSender = new FakeMessageSender();
            var behavior = new RetrySuccessNotificationBehavior
            {
                MessageSender = messageSender,            
                TriggerHeaders = TestHeaderKeys,
                ProcessingEndpointName = TestKey
            };

            var incomingMessage = new TransportMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>
            {
                { TestKey, Guid.NewGuid().ToString() }
            })
            {
                Body = FakeMessageBody,
                Recoverable = true
            };

            var context = new IncomingContext(null, incomingMessage);

            behavior.Invoke(context, () => { });

            Assert.IsTrue(messageSender.WasSent, "Message was not sent");
            Assert.IsFalse(messageSender.HasFakeMessageBody, "Message body was copied");
            Assert.IsTrue(messageSender.HasProcessingEndpointHeaderSet, "ProcessingEndpoint header is not set");
        }

        [Test]
        public void Copies_Body_When_Configured()
        {
            var messageSender = new FakeMessageSender();
            var behavior = new RetrySuccessNotificationBehavior
            {
                MessageSender = messageSender,
                TriggerHeaders = TestHeaderKeys,
                CopyBody = true
            };

            var incomingMessage = new TransportMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>
            {
                { TestKey, Guid.NewGuid().ToString() }
            })
            {
                Body = FakeMessageBody,
                Recoverable = true
            };

            var context = new IncomingContext(null, incomingMessage);

            behavior.Invoke(context, () => { });

            Assert.IsTrue(messageSender.HasFakeMessageBody, "Message was not sent");
        }

        class FakeMessageSender : ISendMessages
        {
            public bool WasSent { get; private set; }

            public bool HasFakeMessageBody { get; private set; }

            public bool HasProcessingEndpointHeaderSet { get; private set; }

            public void Send(TransportMessage message, SendOptions sendOptions)
            {
                WasSent = true;
                HasFakeMessageBody = message.Body == FakeMessageBody;
                HasProcessingEndpointHeaderSet = message.Headers.ContainsKey(Headers.ProcessingEndpoint) && message.Headers[Headers.ProcessingEndpoint] == TestKey;
            }
        }
    }
}
