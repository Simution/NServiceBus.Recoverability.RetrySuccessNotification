using System;
using System.Threading;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Recoverability;
using NServiceBus.Satellites;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class When_Failed_Messages_Are_Successfully_Retried : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_not_notify_when_successnotifications_are_disabled()
    {
        var context = Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
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
    }

    [Test]
    public void Should_notify_when_successnotifications_are_enabled_and_trigger_header_exists()
    {
        Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b =>  b.CustomConfig(config => config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(FakeServiceControl.NotificationsSatellite.NotificationAddress))
            .Given(bus =>
            {
                bus.OutgoingHeaders.Add(ServiceControlRetryHeaders.UniqueMessageId, Guid.NewGuid().ToString());
                bus.SendLocal(new MessageToBeRetried());
            }))
            .WithEndpoint<FakeServiceControl>()
            .Done(c => c.MessageHandlerInvoked && c.AuditHandlerInvoked && c.NotificationHandlerInvoked)
            .Run(TimeSpan.FromMinutes(3));
    }

    [Test]
    public void Should_not_notify_when_successnotifications_are_enabled_and_trigger_header_is_missing()
    {
        var context = Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(config => config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(FakeServiceControl.NotificationsSatellite.NotificationAddress))
                .Given(bus =>
                {
                    bus.SendLocal(new MessageToBeRetried());
                }))
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
    }

    public class Context : ScenarioContext
    {
        public bool MessageHandlerInvoked { get; set; }
        public bool AuditHandlerInvoked { get; set; }
        public bool NotificationHandlerInvoked { get; set; }
        public Guid ExpectedValue { get; set; }
        public Guid AuditedValue { get; set; }
        public bool HasMessageBody { get; set; }
    }

    class TestEndpoint : EndpointConfigurationBuilder
    {

        public TestEndpoint()
        {
            EndpointSetup<DefaultServer>()
                .AuditTo<FakeServiceControl>();
        }

        public class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
        {
            public Context MyContext { get; set; }

            public void Handle(MessageToBeRetried message)
            {
                MyContext.MessageHandlerInvoked = true;
                MyContext.ExpectedValue = message.Value;
            }
        }
    }

    class FakeServiceControl : EndpointConfigurationBuilder
    {

        public FakeServiceControl()
        {
            EndpointSetup<DefaultServer>();
        }

        public class NotificationsSatellite : ISatellite
        {
            public static string NotificationAddress = Conventions.EndpointNamingConvention(typeof(FakeServiceControl)) + ".Notifications";

            public Context MyContext { get; set; }

            public bool Handle(TransportMessage message)
            {
                MyContext.NotificationHandlerInvoked = true;
                MyContext.HasMessageBody = message.Body.Length > 0;
                return true;
            }

            public void Start()
            {                
            }

            public void Stop()
            {
            }                

            public Address InputAddress => Address.Parse(NotificationAddress);
            public bool Disabled => false;
        }

        public class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
        {
            public Context MyContext { get; set; }

            public void Handle(MessageToBeRetried message)
            {
                MyContext.AuditHandlerInvoked = true;
                MyContext.AuditedValue = message.Value;
            }
        }
    }

    [Serializable]
    class MessageToBeRetried : ICommand
    {
        Guid value = Guid.NewGuid();

        public Guid Value => value;
    }
}