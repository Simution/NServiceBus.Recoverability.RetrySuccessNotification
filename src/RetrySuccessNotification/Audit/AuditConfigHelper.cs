namespace NServiceBus
{
    using Config;
    using Features;
    using Utils;

    static class AuditConfigHelper
    {
        public static Address GetConfiguredAuditQueue(FeatureConfigurationContext context)
        {
            var auditAddress = GetAuditQueueAddressFromAuditConfig(context);

            if (auditAddress == Address.Undefined)
            {
                // Check to see if the audit queue has been specified either in the registry as a global setting
                auditAddress = ReadAuditQueueNameFromRegistry();
            }
            return auditAddress;

        }

        static Address ReadAuditQueueNameFromRegistry()
        {
            var forwardQueue = RegistryReader.Read("AuditQueue");
            if (string.IsNullOrWhiteSpace(forwardQueue))
            {
                return Address.Undefined;
            }
            return Address.Parse(forwardQueue);
        }

        static Address GetAuditQueueAddressFromAuditConfig(FeatureConfigurationContext context)
        {
            var messageAuditingConfig = context.Settings.GetConfigSection<AuditConfig>();
            if (messageAuditingConfig != null && !string.IsNullOrWhiteSpace(messageAuditingConfig.QueueName))
            {
                return Address.Parse(messageAuditingConfig.QueueName);
            }
            return Address.Undefined;
        }
    }
}
