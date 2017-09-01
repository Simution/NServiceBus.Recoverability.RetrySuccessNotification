namespace NServiceBus.Features
{
    using Recoverability;

    /// <summary>
    /// Provides the retry success notification feature
    /// </summary>
    public class RetrySuccessNotification : Feature
    {
        internal const string TriggerHeadersKey = "RetrySuccessNotifications.Headers";
        internal const string AddressKey = "RetrySuccessNotifications.Address";
        internal const string CopyBody = "RetrySuccessNotification.CopyBody";

        // ReSharper disable once MemberCanBePrivate.Global
        internal static string[] DefaultTriggerHeaders = {
            ServiceControlRetryHeaders.OldRetryId,
            ServiceControlRetryHeaders.UniqueMessageId
        };

        internal RetrySuccessNotification()
        {
            EnableByDefault();
            Defaults(settings =>
            {
                settings.Set(TriggerHeadersKey, DefaultTriggerHeaders);
                settings.Set(CopyBody, false);
            });
            Prerequisite(config => AuditConfigHelper.GetConfiguredAuditQueue(config).ToString() != config.Settings.GetOrDefault<string>(AddressKey), "Retry Success Notifications cannot be sent to the same queue as Audits");
            Prerequisite(config => !string.IsNullOrWhiteSpace(config.Settings.GetOrDefault<string>(AddressKey)), "No configured retry success notification address was configured");
        }

        /// <inheritdoc />
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<RetrySuccessNotificationBehavior.Registration>();

            var endpointName = context.Settings.EndpointName();
            var notificationAddress = Address.Parse(context.Settings.Get<string>(AddressKey));
            var triggerHeaders = context.Settings.Get<string[]>(TriggerHeadersKey);
            var copyBody = context.Settings.Get<bool>(CopyBody);

            context.Container.ConfigureComponent<RetrySuccessNotificationBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(b => b.NotificationAddress, notificationAddress)
                .ConfigureProperty(b => b.ProcessingEndpointName, endpointName)
                .ConfigureProperty(b => b.TriggerHeaders, triggerHeaders)
                .ConfigureProperty(b => b.CopyBody, copyBody);
        }
    }
}
