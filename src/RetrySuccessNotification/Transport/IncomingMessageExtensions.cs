namespace NServiceBus.Transport
{
    using System.Reflection;

    static class IncomingMessageExtensions
    {
        public static void RevertToOriginalBodyIfNeededUsingReflection(this IncomingMessage message)
        {
            var incomingMessageType = typeof(IncomingMessage);
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            var methodInfo = incomingMessageType.GetMethod("RevertToOriginalBodyIfNeeded", bindingFlags);
            methodInfo.Invoke(message, null);
        }
    }
}