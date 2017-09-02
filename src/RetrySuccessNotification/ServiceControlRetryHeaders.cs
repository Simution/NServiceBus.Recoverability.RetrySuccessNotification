namespace NServiceBus.Recoverability
{
    /// <summary>
    /// Headers used by Service Control when a message is sent for retry
    /// </summary>
    public class ServiceControlRetryHeaders
    {
        /// <summary>
        /// Header used in older Service Control versions
        /// </summary>
        public const string OldRetryId = "ServiceControl.RetryId";

        /// <summary>
        /// Header used by service control to uniquely identify the message
        /// </summary>
        public const string UniqueMessageId = "ServiceControl.Retry.UniqueMessageId";

        internal ServiceControlRetryHeaders()
        {
        }
    }
}
