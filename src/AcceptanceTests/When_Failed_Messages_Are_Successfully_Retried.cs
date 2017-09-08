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
using NServiceBus.Settings;

public class When_Failed_Messages_Are_Successfully_Retried : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_notify_with_sc_uniqueid_header()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FakeServiceControl>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(config =>
                {
                    config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(FakeServiceControl.NotificationsSatellite.NotificationAddress);
                })
            .When(s =>
            {
                var options = new SendOptions();
                options.SetHeader(ServiceControlRetryHeaders.UniqueMessageId, Guid.NewGuid().ToString());
                options.RouteToThisEndpoint();
                return s.Send(new MessageToBeRetried(), options);
            }))
            .Done(c => c.MessageHandlerInvoked && c.NotificationHandlerInvoked)
            .Run();

        Assert.IsFalse(context.HasMessageBody, "Message body is not empty");
        Assert.IsTrue(context.HasProcessingEndpointHeader, "Processing Endpoint header missing");
    }

    [Test]
    public async Task Should_notify_with_sc_retryid_header()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<FakeServiceControl>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(config =>
                {
                    config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(FakeServiceControl.NotificationsSatellite.NotificationAddress);
                })
                .When(s =>
                {
                    var options = new SendOptions();
                    options.SetHeader(ServiceControlRetryHeaders.OldRetryId, Guid.NewGuid().ToString());
                    options.RouteToThisEndpoint();
                    return s.Send(new MessageToBeRetried(), options);
                }))
            .Done(c => c.MessageHandlerInvoked && c.NotificationHandlerInvoked)
            .Run();
    }

    [Test]
    public async Task Should_not_notify_when_trigger_header_is_missing()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FakeServiceControl>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(config =>
                {
                    config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(FakeServiceControl.NotificationsSatellite.NotificationAddress);
                })
                .When(s => s.SendLocal(new MessageToBeRetried())))
            .Done(c =>
            {
                if (!c.MessageHandlerInvoked)
                {
                    return false;
                }

                Thread.Sleep(1000); //Give time for notifications to process

                return true;
            })
            .Run(TimeSpan.FromMinutes(3));

        Assert.IsNotNull(context.FeatureActive, "FeatureActive");
        Assert.IsTrue(context.FeatureActive.Value, "Feature is not active");
        Assert.IsFalse(context.NotificationHandlerInvoked);
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
        public bool? FeatureActive { get; set; }
    }

    class TestEndpoint : EndpointConfigurationBuilder
    {

        public TestEndpoint()
        {
            EndpointSetup<DefaultServer>();
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

        public class ExtractFeature : Feature
        {
            public ExtractFeature()
            {
                EnableByDefault();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(b => new Startuptask(b.Build<Context>(), b.Build<ReadOnlySettings>()));
            }

            public class Startuptask : FeatureStartupTask
            {
                readonly Context context;
                readonly ReadOnlySettings settings;

                public Startuptask(Context context, ReadOnlySettings settings)
                {
                    this.context = context;
                    this.settings = settings;
                }

                protected override Task OnStart(IMessageSession session)
                {
                    context.FeatureActive = settings.IsFeatureActive(typeof(RetrySuccessNotification));
                    return Task.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return Task.CompletedTask;
                }
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
                testContext.HasProcessingEndpointHeader = context.Headers.ContainsKey(Headers.ProcessingEndpoint);
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