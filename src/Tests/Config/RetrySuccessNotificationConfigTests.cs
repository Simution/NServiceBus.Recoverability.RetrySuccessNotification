namespace NServiceBus.Tests
{
    using System.Linq;
    using NServiceBus;
    using Configuration.AdvanceExtensibility;
    using Features;
    using NUnit.Framework;

    [TestFixture]
    public class RetrySuccessNotificationConfigTests
    {
        [Test]
        public void Notification_Address_Can_Be_Set()
        {
            var busConfiguration = new BusConfiguration();

            var config = busConfiguration.RetrySuccessNotifications();

            const string testAddress = "Test";

            config.SendRetrySuccessNotificationsTo(testAddress);

            var settings = busConfiguration.GetSettings();

            string notificationAddress;

            var settingRetrieved = settings.TryGet(RetrySuccessNotification.AddressKey, out notificationAddress);

            Assert.IsTrue(settingRetrieved, "Setting was not set");
            Assert.AreEqual(testAddress, notificationAddress, "Incorrect Notification Address value");
        }

        [Test]
        public void Trigger_Headers_Can_Be_Added()
        {
            var busConfiguration = new BusConfiguration();

            var config = busConfiguration.RetrySuccessNotifications();

            const string testHeader = "Test";

            config.AddRetrySuccessNotificationTriggerHeaders(testHeader);

            var settings = busConfiguration.GetSettings();

            string[] triggerHeaders;

            var settingRetrieved = settings.TryGet(RetrySuccessNotification.TriggerHeadersKey, out triggerHeaders);

            var expectedHeaders = RetrySuccessNotification.DefaultTriggerHeaders.Union(new[]
            {
                testHeader
            });

            Assert.IsTrue(settingRetrieved, "Setting was not set");
            Assert.That(triggerHeaders, Is.EquivalentTo(expectedHeaders), "Headers are missing");
        }

        [Test]
        public void Copy_Message_Body_Setting_Can_Be_Set()
        {
            var busConfiguration = new BusConfiguration();

            var config = busConfiguration.RetrySuccessNotifications();

            config.CopyMessageBodyInNotification = true;

            var settings = busConfiguration.GetSettings();

            bool copyBodySetting;

            var settingRetrieved = settings.TryGet(RetrySuccessNotification.CopyBody, out copyBodySetting);

            Assert.IsTrue(settingRetrieved, "Setting was not set");
            Assert.IsTrue(copyBodySetting, "Incorrect Copy Body Setting value");
        }
    }
}
