# NServiceBus.Recoverability.RetrySuccessNotification

Sends a message to a designated endpoint when a message returned to queue has been successfully processed. It is primarly meant to work with ServiceControl, which uses the receipt of a retried message via the audit queue to marked them as resolved.
