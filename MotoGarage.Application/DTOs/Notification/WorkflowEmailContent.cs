namespace MotoGarage.Application.DTOs.Notification;

public record WorkflowEmailContent(Guid DeliveryId, string Subject, string TextBody, string HtmlBody);
