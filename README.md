<img src="https://raw.githubusercontent.com/Simution/NServiceBus.Recoverability.RetrySuccessNotification/develop/icons/retrysuccessnotification.png" width="100"/>

# NServiceBus.Recoverability.RetrySuccessNotification

This plugin sends a message to a designated endpoint when a message returned to queue has been successfully processed. It is primarly meant to work with Service Control, which uses the receipt of a retried message via the audit queue to marked them as resolved.

Notifications are only sent after the incoming message is successfully processed and if the incoming message has a pre-configured header that indicates the message is a retry. By default 2 headers, both provided by Service Control, will trigger notifications:

* `ServiceControl.RetryId` - This is the old header Service Control used to identify the failed message.
* `ServiceControl.Retry.UniqueMessageId` - This is the newer header Service Control uses to uniquely identify the failed message.

The notification will include all headers of the incoming message but will not copy the message body.

## Install

NServiceBus.Recoverability.RetrySuccessNotification is installable via [NuGet](https://www.nuget.org/packages/NServiceBus.Recoverability.RetrySuccessNotification/) using the following command:

`Install-Package NServiceBus.Recoverability.RetrySuccessNotification`

## Usage

To enable retry success notifications access the configuration from `EndpointConfiguration` and configure the queue to send notifications to:

```
var notificationConfig = endpointConfiguration.RetrySuccessNotifications();
notificationConfig.SendRetrySuccessNotificationsTo("AddressToSendNotification");
```

If [auditing](https://docs.particular.net/nservicebus/operations/auditing) is turned on, the notification address and the audit address must be different.

To configure additional headers that will trigger success notifications use this configuration option:

```
var notificationConfig = endpointConfiguration.RetrySuccessNotifications();
notificationConfig.AddRetrySuccessNotificationTriggerHeaders("MyCustomHeader");
```

To include the message body of the incoming message in the notification use this configuration option:

```
var notificationConfig = endpointConfiguration.RetrySuccessNotifications();
notificationConfig.CopyMessageBodyInNotification = true;
```

--- 

Icon based on by [Ismael Ruiz](https://thenounproject.com/prisma0081) via The Noun Project under the [Creative Commons license](https://creativecommons.org/licenses/by/3.0/us/).
