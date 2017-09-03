namespace NServiceBus.Recoverability.RetrySucessNotification.ComponentTests
{
    class CommonTest
    {
        protected static string[] TestHeaderKeys = {
            ServiceControlRetryHeaders.UniqueMessageId
        };

        protected static byte[] FakeMessageBody = { 0x20 };
    }
}
