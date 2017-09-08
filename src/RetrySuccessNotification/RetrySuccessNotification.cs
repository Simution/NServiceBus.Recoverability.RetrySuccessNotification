namespace NServiceBus.Features
{
    using Recoverability;
    using Transport;

    /// <inheritdoc />
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
            Prerequisite(config =>
            {
                if (!config.Settings.IsFeatureActive(typeof(Audit)))
                {
                    return true;
                }
                config.Settings.TryGetAuditQueueAddress(out var auditAddress);
                return auditAddress != config.Settings.GetOrDefault<string>(AddressKey);
            }, "Retry Success Notifications cannot be sent to the same queue as Audits");
            Prerequisite(config => !string.IsNullOrWhiteSpace(config.Settings.GetOrDefault<string>(AddressKey)), "No configured retry success notification address was configured");
        }

        /// <inheritdoc />
        protected override void Setup(FeatureConfigurationContext context)
        {
            var endpointName = context.Settings.EndpointName();
            var notificationAddress = context.Settings.Get<string>(AddressKey);
            var triggerHeaders = context.Settings.Get<string[]>(TriggerHeadersKey);
            var copyBody = context.Settings.Get<bool>(CopyBody);

            context.Settings.Get<QueueBindings>().BindSending(notificationAddress);

            context.Pipeline.Register(new RetrySuccessNotificationBehavior(endpointName, notificationAddress, triggerHeaders, copyBody), "Adds a retry success notification to the pending transport operations.");
        }
    }
}
