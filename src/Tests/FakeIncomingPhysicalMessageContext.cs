using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.ObjectBuilder;
using NServiceBus.Pipeline;
using NServiceBus.Transport;

[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
class FakeIncomingPhysicalMessageContext : IIncomingPhysicalMessageContext
{
    public const string TestKey = "Key";
    const string TestValue = "Value";

    public ContextBag Extensions { get; }
    public IBuilder Builder { get; }

    public FakeIncomingPhysicalMessageContext(IncomingMessage message)
    {
        Message = message;
        Extensions = new ContextBag();
        Extensions.Set(TestKey, TestValue);
        Extensions.Set(new PendingTransportOperations());
    }

    public Task Send(object message, SendOptions options)
    {
        return Task.CompletedTask;
    }

    public Task Send<T>(Action<T> messageConstructor, SendOptions options)
    {
        return Task.CompletedTask;
    }

    public Task Publish(object message, PublishOptions options)
    {
        return Task.CompletedTask;
    }

    public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
    {
        return Task.CompletedTask;
    }

    public Task Reply(object message, ReplyOptions options)
    {
        return Task.CompletedTask;
    }

    public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
    {
        return Task.CompletedTask;
    }

    public Task ForwardCurrentMessageTo(string destination)
    {
        return Task.CompletedTask;
    }

    public string MessageId => Message.MessageId;
    public string ReplyToAddress { get; }
    public IReadOnlyDictionary<string, string> MessageHeaders => Message.Headers;

    public void UpdateMessage(byte[] body)
    {
    }

    public IncomingMessage Message { get; }
}