namespace NServiceBus.Recoverability.RetrySucessNotification.ComponentTests
{
    using Extensibility;

    static class ContextBagExtensions
    {
        // ReSharper disable once UnusedVariable
        public static bool ContainsKey<T>(this ContextBag bag, string key) => bag.TryGet(key, out T result);
    }
}
