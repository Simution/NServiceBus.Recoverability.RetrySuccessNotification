namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;

    /// <summary>
    /// Configuration options for Retry Success Notifications
    /// </summary>
    public static class ConfigureRetrySuccessNotification
    {
        /// <summary>
        /// Access settings to configure Retry Success Notifications
        /// </summary>
        public static RetrySuccessNotificationConfig RetrySuccessNotifications(this EndpointConfiguration config)
        {
            return new RetrySuccessNotificationConfig(config.GetSettings());
        }
    }
}