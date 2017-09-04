<img src="https://raw.githubusercontent.com/Simution/NServiceBus.Recoverability.RetrySuccessNotification/develop/icons/retrysuccessnotification.png" width="100"/>

# NServiceBus.Recoverability.RetrySuccessNotification

This plugin sends a message to a designated endpoint when a message returned to queue has been successfully processed. It is primarly meant to work with ServiceControl, which uses the receipt of a retried message via the audit queue to marked them as resolved. By configuring an endpoint to send notifications to the ServiceControl audit queue, and disabling auditing, it can greatly reduce the load on ServiceControl without giving up the ability for ServiceControl to automatically mark the failed message as resolved. This is especially true if ServiceInsight is not being used to review audit messages.

Notifications are only sent after the incoming message is successfully processed and if the incoming message has a pre-configured header that indicates the message is a retry. By default 2 headers, both provided by ServiceControl, will trigger notifications:

* `ServiceControl.RetryId` - This is the old header ServiceControl used to identify the failed message.
* `ServiceControl.Retry.UniqueMessageId` - This is the newer header ServiceControl uses to uniquely identify the failed message.

The notification will include all headers of the incoming message but will not copy the message body.

## Install

NServiceBus.Recoverability.RetrySuccessNotification is installable via [NuGet](https://www.nuget.org/packages/NServiceBus.Recoverability.RetrySuccessNotification/) using the following command:

`Install-Package NServiceBus.Recoverability.RetrySuccessNotification`

## Usage

To enable retry success notifications access the configuration from `EndpointConfiguration` and configure the queue to send notifications to:

```Csharp
var notificationConfig = endpointConfiguration.RetrySuccessNotifications();
notificationConfig.SendRetrySuccessNotificationsTo("AddressToSendNotification");
```

If [auditing](https://docs.particular.net/nservicebus/operations/auditing) is turned on, the notification address and the audit address must be different.

To configure additional headers that will trigger success notifications use this configuration option:

```Csharp
var notificationConfig = endpointConfiguration.RetrySuccessNotifications();
notificationConfig.AddRetrySuccessNotificationTriggerHeaders("MyCustomHeader");
```

To include the message body of the incoming message in the notification use this configuration option:

```Csharp
var notificationConfig = endpointConfiguration.RetrySuccessNotifications();
notificationConfig.CopyMessageBodyInNotification = true;
```

## Splitting Audit Processing and Error Processing

Normally one instance of ServiceControl is used to handle all audit and error messages. In systems with a high enough message volume a single instance ServiceControl can become overwhelmed. Since audit messages in a relatively healthy system represent the vast majority of the messages processed by ServiceControl it can be advantageous to split audit processing and error processing into seperate ServiceControl instances in this scenario.

NServiceBus.Recoverability.RetrySuccessNotification is designed to work side-by-side with the Auditing feature built into NServiceBus. Configure audit messages to be sent to the auditing instance of ServiceControl, and successful retry notifications to the error processing instance of ServiceControl. 

Install ServicePulse to work with the error processing instance of ServiceControl, since it does not provide functionality that uses audit messages.

By configuring [error message forwarding](https://docs.particular.net/servicecontrol/creating-config-file#transport-servicecontrolforwarderrormessages) in the error processing instance of ServiceControl to forward to the error queue of the auditing instance of ServiceControl, both audit and error messages can be viewed together in ServiceInsight by connecting ServiceInsight to the auditing instance of ServiceControl. This is done through the Tools menu and the `Connect to ServiceControl` menu option.

When using this configuration, it is also recommended to turn the [audit retention period](https://docs.particular.net/servicecontrol/creating-config-file#data-retention-servicecontrolauditretentionperiod) of the error processing instance of ServiceControl to 1 hour.

--- 

Icon based on by [Ismael Ruiz](https://thenounproject.com/prisma0081) via The Noun Project under the [Creative Commons license](https://creativecommons.org/licenses/by/3.0/us/).
