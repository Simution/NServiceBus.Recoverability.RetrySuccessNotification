namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public interface ITestSuiteConstraints
    {
        bool SupportsDtc { get; }

        bool SupportsCrossQueueTransactions { get; }

        bool SupportsNativePubSub { get; }

        bool SupportsNativeDeferral { get; }

        bool SupportsOutbox { get; }

        // ReSharper disable UnusedMemberInSuper.Global
        IConfigureEndpointTestExecution CreateTransportConfiguration();        

        IConfigureEndpointTestExecution CreatePersistenceConfiguration();
        // ReSharper restore UnusedMemberInSuper.Global
    }

    // ReSharper disable once PartialTypeWithSinglePart
    public partial class TestSuiteConstraints : ITestSuiteConstraints
    {
        public static TestSuiteConstraints Current = new TestSuiteConstraints();
    }
}