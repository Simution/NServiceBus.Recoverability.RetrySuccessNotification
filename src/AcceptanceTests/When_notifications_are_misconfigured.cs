using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Features;
using NServiceBus.Settings;
using NUnit.Framework;

[TestFixture]
class When_notifications_are_misconfigured
{
    [Test]
    public async Task Should_not_activate_when_both_audits_and_notifications_are_sent_to_the_same_address()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(config =>
            {
                config.AuditProcessedMessagesTo("audit");
                config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo("audit");
            }))
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.IsNotNull(context.FeatureActive, "FeatureActive");
        Assert.IsFalse(context.FeatureActive.Value, "Feature is active");
    }

    [Test]
    public async Task Should_not_activate_when_notification_address_is_whitespace()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(config =>
            {
                config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(" ");
            }))
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.IsNotNull(context.FeatureActive, "FeatureActive");
        Assert.IsFalse(context.FeatureActive.Value, "Feature is active");
    }

    [Test]
    public async Task Should_not_activate_when_notification_address_is_null()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(config =>
            {
                config.RetrySuccessNotifications().SendRetrySuccessNotificationsTo(null);
            }))
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.IsNotNull(context.FeatureActive, "FeatureActive");
        Assert.IsFalse(context.FeatureActive.Value, "Feature is active");
    }

    class Context : ScenarioContext
    {
        public bool? FeatureActive { get; set; }
    }

    class TestEndpoint : EndpointConfigurationBuilder
    {

        public TestEndpoint()
        {
            EndpointSetup<DefaultServer>();
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
}