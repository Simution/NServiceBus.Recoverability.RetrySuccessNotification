namespace NServiceBus.AcceptanceTests.Audit
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ObjectBuilder;
    using Transport;

    public class When_audit_is_overridden_in_code : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_audit_to_target_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<UserEndpoint>(b => b.When(session => session.SendLocal(new MessageToBeAudited())))
                .WithEndpoint<AuditSpy>()
                .Done(c => c.MessageAudited)
                .Run();

            Assert.True(context.MessageAudited);
        }

        public class UserEndpoint : EndpointConfigurationBuilder
        {
            public UserEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(AuditSpy.NullSatellite.Address);
                    c.AuditProcessedMessagesTo("audit_with_code_target");
                });
            }

            class Handler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class AuditSpy : EndpointConfigurationBuilder
        {
            public AuditSpy()
            {
                EndpointSetup<DefaultServer>()
                    .CustomEndpointName("audit_with_code_target");
            }

            class AuditMessageHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context MyContext { get; set; }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    MyContext.MessageAudited = true;
                    return Task.FromResult(0);
                }
            }

            public class NullSatellite : Feature
            {
                public static string Address;

                public NullSatellite()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    var satelliteLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("null");
                    Address = context.Settings.GetTransportAddress(satelliteLogicalAddress);

                    context.AddSatelliteReceiver("NullSatellite", Address, PushRuntimeSettings.Default, (config, errorContext) => RecoverabilityAction.MoveToError(config.Failed.ErrorQueue), OnMessage);
                }

                Task OnMessage(IBuilder builder, MessageContext context)
                {
                    return Task.CompletedTask;
                }
            }
        }

        class Context : ScenarioContext
        {
            public bool MessageAudited { get; set; }
        }


        public class MessageToBeAudited : IMessage
        {
        }
    }
}