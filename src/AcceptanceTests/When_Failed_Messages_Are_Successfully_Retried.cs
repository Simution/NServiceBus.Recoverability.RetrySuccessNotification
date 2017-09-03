using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Features;
using NServiceBus.ObjectBuilder;
using NServiceBus.Recoverability;
using NServiceBus.Transport;
using NUnit.Framework;
using NServiceBus.AcceptanceTesting.Customization;

public class When_Failed_Messages_Are_Successfully_Retried : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_notify_when_successnotifications_are_disabled()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b.When(s => s.SendLocal(new MessageToBeRetried
            {
                Value = Guid.NewGuid()
            })))
            .WithEndpoint<FakeServiceControl>()
            .Done(c =>
            {
                if (!c.MessageHandlerInvoked || !c.AuditHandlerInvoked)
                {
                    return false;
                }

                Thread.Sleep(1000); //Give time for notifications to process

                return true;
            })
            .Run();

        Assert.IsFalse(context.NotificationHandlerInvoked);
        Assert.AreEqual(context.ExpectedValue, context.AuditedValue, "Value mismatch");
        Assert.IsTrue(context.HasProcessingEndpointHeader, "Processing Endpoint header missing");
    }

    [Test]
    public async Task Should_notify_when_successnotifications_are_enabled_and_trigger_header_exists()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(config => config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(FakeServiceControl.NotificationsSatellite.NotificationAddress))
            .When(s =>
            {
                var options = new SendOptions();
                options.SetHeader(ServiceControlRetryHeaders.UniqueMessageId, Guid.NewGuid().ToString());
                options.RouteToThisEndpoint();
                return s.Send(new MessageToBeRetried(), options);
            }))
            .WithEndpoint<FakeServiceControl>()
            .Done(c => c.MessageHandlerInvoked && c.AuditHandlerInvoked && c.NotificationHandlerInvoked)
            .Run();

        Assert.IsFalse(context.HasMessageBody, "Message body is not empty");
        Assert.IsTrue(context.HasProcessingEndpointHeader, "Processing Endpoint header missing");
    }

    [Test]
    public async Task Should_not_notify_when_successnotifications_are_enabled_and_trigger_header_is_missing()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(config => config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(FakeServiceControl.NotificationsSatellite.NotificationAddress))
                .When(s => s.SendLocal(new MessageToBeRetried())))
            .WithEndpoint<FakeServiceControl>()
            .Done(c =>
            {
                if (!c.MessageHandlerInvoked || !c.AuditHandlerInvoked)
                {
                    return false;
                }

                Thread.Sleep(1000); //Give time for notifications to process

                return true;
            })
            .Run(TimeSpan.FromMinutes(3));

        Assert.IsFalse(context.NotificationHandlerInvoked);
        Assert.IsTrue(context.HasProcessingEndpointHeader, "Processing Endpoint header missing");
    }

    class Context : ScenarioContext
    {
        public bool MessageHandlerInvoked { get; set; }
        public bool AuditHandlerInvoked { get; set; }
        public bool NotificationHandlerInvoked { get; set; }
        public Guid ExpectedValue { get; set; }
        public Guid AuditedValue { get; set; }
        public bool HasProcessingEndpointHeader { get; set; }
        public bool HasMessageBody { get; set; }
    }

    class TestEndpoint : EndpointConfigurationBuilder
    {

        public TestEndpoint()
        {
            EndpointSetup<DefaultServer>(c => c
                .AuditProcessedMessagesTo<FakeServiceControl>());
        }

        public class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
        {
            public Context MyContext { get; set; }

            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
            {
                MyContext.MessageHandlerInvoked = true;
                MyContext.ExpectedValue = message.Value;
                return Task.CompletedTask;
            }
        }
    }

    class FakeServiceControl : EndpointConfigurationBuilder
    {
        public FakeServiceControl()
        {
            EndpointSetup<DefaultServer>();
        }

        public class NotificationsSatellite : Feature
        {
            public static string NotificationAddress;

            public NotificationsSatellite()
            {
                EnableByDefault();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                var satelliteLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("Notifications");
                NotificationAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);

                context.AddSatelliteReceiver("NotificationsSatellite", NotificationAddress, PushRuntimeSettings.Default, (config, errorContext) => RecoverabilityAction.MoveToError(config.Failed.ErrorQueue), OnMessage);
            }

            Task OnMessage(IBuilder builder, MessageContext context)
            {
                var testContext = builder.Build<Context>();
                testContext.NotificationHandlerInvoked = true;
                testContext.HasMessageBody = context.Body.Length > 0;
                return Task.CompletedTask;
            }
        }

        public class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
        {
            public Context MyContext { get; set; }

            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
            {
                MyContext.AuditHandlerInvoked = true;
                MyContext.AuditedValue = message.Value;
                MyContext.HasProcessingEndpointHeader = context.MessageHeaders.ContainsKey(Headers.ProcessingEndpoint);
                return Task.CompletedTask;
            }
        }
    }

    [Serializable]
    class MessageToBeRetried : ICommand
    {
        public Guid Value { get; set; }
    }
}