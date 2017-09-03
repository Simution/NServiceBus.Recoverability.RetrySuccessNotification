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
                if (!config.Settings.TryGetAuditQueueAddress(out var auditAddress))
                {
                    return true;
                }
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

            if (!context.Settings.IsFeatureActive(typeof(Audit)))
            {
                context.Pipeline.Register(new RetrySuccessNotificationDispatchConnector(endpointName), "Dispatches recovery success notifications to the transport");
                context.Pipeline.Register(new InvokeRetrySuccessNotificationPipelineBehavior(notificationAddress, triggerHeaders, copyBody), "Execute the retry success notification pipeline.");
                return;
            }

            context.Settings.TryGetAuditQueueAddress(out var auditAddress);
            context.Pipeline.Replace("AuditProcessedMessage", new InvokeAuditAndRetrySucessNotificationPipelineBehavior(notificationAddress, triggerHeaders, copyBody, auditAddress), "Execute the audit and retry success notification pipelines.");            
        }
    }
}
