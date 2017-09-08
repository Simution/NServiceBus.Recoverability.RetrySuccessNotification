using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NUnit.Framework;
using NServiceBus.Recoverability;

[TestFixture]
class RetrySuccssNotificationBehaviorTests
{
    static string[] TestHeaderKeys = {
        ServiceControlRetryHeaders.UniqueMessageId
    };

    static byte[] FakeMessageBody = { 0x20 };

    const string endpointName = "testEndpoint";
    const string testAddress = "testAddress";

    [Test]
    public async Task Does_Not_Send_When_Headers_Are_Absent()
    {
        var behavior = new RetrySuccessNotificationBehavior(null, null, TestHeaderKeys, false);

        var nextWasCalled = false;

        var incomingContext = new FakeIncomingPhysicalMessageContext(new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]));

        var pendingOperations = incomingContext.Extensions.Get<PendingTransportOperations>();

        await behavior.Invoke(incomingContext, () =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        Assert.IsTrue(nextWasCalled, "next was not called");
        Assert.IsEmpty(pendingOperations.Operations,"Operations are present");
    }

    [Test]
    public async Task Sends_When_Headers_Are_Present()
    {
        var behavior = new RetrySuccessNotificationBehavior(endpointName, testAddress, TestHeaderKeys, false);

        var nextWasCalled = false;

        var messageId = Guid.NewGuid().ToString();

        var header = new KeyValuePair<string, string>(ServiceControlRetryHeaders.UniqueMessageId, "test");

        var incomingContext = new FakeIncomingPhysicalMessageContext(new IncomingMessage(messageId, new Dictionary<string, string>
        {
            {header.Key, header.Value}
        }, FakeMessageBody));

        var pendingOperations = incomingContext.Extensions.Get<PendingTransportOperations>();

        await behavior.Invoke(incomingContext, () =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        Assert.IsTrue(nextWasCalled, "next was not called");
        Assert.IsNotEmpty(pendingOperations.Operations, "Operations were not added");
        Assert.AreEqual(1, pendingOperations.Operations.Length, "More than one operation was added");

        var operation = pendingOperations.Operations.Single();

        Assert.IsAssignableFrom<UnicastAddressTag>(operation.AddressTag, "Addresstag is not the correct type");

        var addressTag = operation.AddressTag as UnicastAddressTag;

        Assert.AreEqual(testAddress, addressTag.Destination, "Notification address does not match");
        Assert.AreEqual(messageId, operation.Message.MessageId, "Message Ids do not match");
        Assert.IsTrue(operation.Message.Headers.ContainsKey(header.Key), "Header not found");
        Assert.AreEqual(header.Value, operation.Message.Headers[header.Key], "Header value does not match");
        Assert.AreNotEqual(FakeMessageBody, operation.Message.Body, "Body was copied when it should not have been");
        Assert.IsTrue(operation.Message.Headers.ContainsKey(Headers.ProcessingEndpoint), "Processing Endpoint header not found");
        Assert.AreEqual(endpointName, operation.Message.Headers[Headers.ProcessingEndpoint], "Processing Endpoint header value does not match");
    }

    [Test]
    public async Task Copies_Body_When_Configured()
    {
        var behavior = new RetrySuccessNotificationBehavior(endpointName, testAddress, TestHeaderKeys, true);

        var nextWasCalled = false;

        var messageId = Guid.NewGuid().ToString();

        var header = new KeyValuePair<string, string>(ServiceControlRetryHeaders.UniqueMessageId, "test");

        var incomingContext = new FakeIncomingPhysicalMessageContext(new IncomingMessage(messageId, new Dictionary<string, string>
        {
            {header.Key, header.Value}
        }, FakeMessageBody));

        var pendingOperations = incomingContext.Extensions.Get<PendingTransportOperations>();

        await behavior.Invoke(incomingContext, () =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        Assert.IsTrue(nextWasCalled, "next was not called");
        Assert.IsNotEmpty(pendingOperations.Operations, "Operations were not added");
        Assert.AreEqual(1, pendingOperations.Operations.Length, "More than one operation was added");

        var operation = pendingOperations.Operations.Single();

        Assert.AreEqual(FakeMessageBody, operation.Message.Body, "Body was not copied when it should have been");
    }
}