namespace NServiceBus
{
    using System.Linq;
    using Features;
    using Settings;

    /// <summary>
    /// Retry Success Notification configuration options
    /// </summary>
    public class RetrySuccessNotificationConfig
    {
        readonly SettingsHolder settings;

        internal RetrySuccessNotificationConfig(SettingsHolder settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Set the address to send retry success notifications to.
        /// </summary>
        public void SendRetrySuccessNotificationsTo(string address)
        {
            settings.Set(RetrySuccessNotification.AddressKey, address);
        }

        /// <summary>
        /// Add new headers that when included in the message will trigger sending a success notification
        /// </summary>
        public void AddRetrySuccessNotificationTriggerHeaders(params string[] additionalTriggerHeaders)
        {
            settings.Set(RetrySuccessNotification.TriggerHeadersKey, RetrySuccessNotification.DefaultTriggerHeaders.Union(additionalTriggerHeaders).ToArray());
        }

        /// <summary>
        /// Sets whether to copy the message body from the incoming message to the notification
        /// </summary>
        public bool CopyMessageBodyInNotification
        {
            set => settings.Set(RetrySuccessNotification.CopyBody, value);
        }
    }
}
