using System.Linq;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NUnit.Framework;

[TestFixture]
public class RetrySuccessNotificationConfigTests
{
    [Test]
    public void Notification_Address_Can_Be_Set()
    {
        var endpointConfiguration = new EndpointConfiguration("test");

        var config = endpointConfiguration.RetrySuccessNotifications();

        const string testAddress = "Test";

        config.SendRetrySuccessNotificationsTo(testAddress);

        var settings = endpointConfiguration.GetSettings();

        var settingRetrieved = settings.TryGet(RetrySuccessNotification.AddressKey, out string notificationAddress);

        Assert.IsTrue(settingRetrieved, "Setting was not set");
        Assert.AreEqual(testAddress, notificationAddress, "Incorrect Notification Address value");
    }

    [Test]
    public void Trigger_Headers_Can_Be_Added()
    {
        var endpointConfiguration = new EndpointConfiguration("test");

        var config = endpointConfiguration.RetrySuccessNotifications();

        const string testHeader = "Test";

        config.AddRetrySuccessNotificationTriggerHeaders(testHeader);

        var settings = endpointConfiguration.GetSettings();

        var settingRetrieved = settings.TryGet(RetrySuccessNotification.TriggerHeadersKey, out string[] triggerHeaders);

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
        var endpointConfiguration = new EndpointConfiguration("test");

        var config = endpointConfiguration.RetrySuccessNotifications();

        config.CopyMessageBodyInNotification = true;

        var settings = endpointConfiguration.GetSettings();

        var settingRetrieved = settings.TryGet(RetrySuccessNotification.CopyBody, out bool copyBodySetting);

        Assert.IsTrue(settingRetrieved, "Setting was not set");
        Assert.IsTrue(copyBodySetting, "Incorrect Copy Body Setting value");
    }
}