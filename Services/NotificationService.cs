using Logistic_Shipment_tracker.Data;
using Logistic_Shipment_tracker.Models;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Logistic_Shipment_tracker.Services
{
    public interface INotificationService
    {
        Task SendEmailNotificationAsync(string email, string subject, string message);
        Task SendSMSNotificationAsync(string phoneNumber, string message);
        Task NotifyShipmentStatusChangeAsync(Shipment shipment, string status);
        Task NotifyDriverAssignmentAsync(Guid shipmentId, Guid driverId);
    }

    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDBContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly SmsService _smsService;

        public NotificationService(
            IConfiguration configuration,
            ApplicationDBContext context,
            ILogger<NotificationService> logger,
            SmsService smsService
            )
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _smsService = smsService;
        }

        public async Task SendEmailNotificationAsync(string email , string subject , string message)
        {
            try
            {
                var apiKey = _configuration["SendGrid:ApiKey"];
                if(string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("SendGrid API key not configured");
                    return;
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(
                    _configuration["SendGrid:FromEmail"],
                    "Logistic Tracker"
                    );
                var to = new EmailAddress(email);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);

                var response = await client.SendEmailAsync(msg);
                _logger.LogInformation($"Email sent to {email}");
            }
            catch( Exception ex )
            {
                _logger.LogError(ex, $"failed to send email to {email}");
            }
        }

        public async Task SendSMSNotificationAsync(string phoneNumber , string message)
        {
            try
            {
                await _smsService.SendSmsAsync(phoneNumber, message);
                _logger.LogInformation($"SMS sent tp {phoneNumber}");
            }
            catch( Exception ex )
            {
                _logger.LogError(ex, $"Failed to send SMS to {phoneNumber}");
            }
        }

        public async Task NotifyShipmentStatusChangeAsync(Shipment shipment, string status)
        {
            try
            {
                // Load related entities if not already loaded
                if (shipment.Sender == null)
                {
                    await _context.Entry(shipment).Reference(s => s.Sender).LoadAsync();
                }
                if (shipment.AssignedDriver == null && shipment.AssignedDriverId.HasValue)
                {
                    await _context.Entry(shipment).Reference(s => s.AssignedDriver).LoadAsync();
                }

                var notifications = new List<Notification>();

                // Notify Receiver
                var receiverSubject = $"Shipment Update - {shipment.TrackingNumber}";
                var receiverMessage =
                    $"Your shipment {shipment.TrackingNumber} status has been updated to: {status}";

                await SendEmailNotificationAsync(shipment.ReceiverEmail, receiverSubject, receiverMessage);

                notifications.Add(new Notification
                {
                    ShipmentId = shipment.Id,
                    Type = NotificationType.Email,
                    Recipient = shipment.ReceiverEmail,
                    Message = receiverMessage,
                    Status = NotificationStatus.Sent,
                    SentAt = DateTime.UtcNow,
                });

                if (!string.IsNullOrEmpty(shipment.ReceiverPhone))
                {
                    await SendSMSNotificationAsync(shipment.ReceiverPhone, receiverMessage);
                    notifications.Add(new Notification
                    {
                        ShipmentId = shipment.Id,
                        Type = NotificationType.SMS,
                        Recipient = shipment.ReceiverPhone,
                        Message = receiverMessage,
                        Status = NotificationStatus.Sent,
                        SentAt = DateTime.UtcNow,
                    });
                }

                // Notify Sender
                if (shipment.Sender != null)
                {
                    var senderSubject = $"Your Shipment Update - {shipment.TrackingNumber}";
                    var senderMessage =
                        $"Your shipment {shipment.TrackingNumber} to {shipment.ReceiverName} has been updated to: {status}";

                    await SendEmailNotificationAsync(shipment.Sender.Email, senderSubject, senderMessage);

                    notifications.Add(new Notification
                    {
                        ShipmentId = shipment.Id,
                        Type = NotificationType.Email,
                        Recipient = shipment.Sender.Email,
                        Message = senderMessage,
                        Status = NotificationStatus.Sent,
                        SentAt = DateTime.UtcNow,
                    });

                    if (!string.IsNullOrEmpty(shipment.Sender.Phone))
                    {
                        await SendSMSNotificationAsync(shipment.Sender.Phone, senderMessage);
                        notifications.Add(new Notification
                        {
                            ShipmentId = shipment.Id,
                            Type = NotificationType.SMS,
                            Recipient = shipment.Sender.Phone,
                            Message = senderMessage,
                            Status = NotificationStatus.Sent,
                            SentAt = DateTime.UtcNow,
                        });
                    }
                }

                // Notify Driver (if assigned)
                if (shipment.AssignedDriver != null)
                {
                    var driverSubject = $"Shipment Assignment - {shipment.TrackingNumber}";
                    var driverMessage =
                        $"Shipment {shipment.TrackingNumber} status updated to: {status}. Origin: {shipment.OriginAddress}, Destination: {shipment.DestinationAddress}";

                    await SendEmailNotificationAsync(shipment.AssignedDriver.Email, driverSubject, driverMessage);

                    notifications.Add(new Notification
                    {
                        ShipmentId = shipment.Id,
                        Type = NotificationType.Email,
                        Recipient = shipment.AssignedDriver.Email,
                        Message = driverMessage,
                        Status = NotificationStatus.Sent,
                        SentAt = DateTime.UtcNow,
                    });

                    if (!string.IsNullOrEmpty(shipment.AssignedDriver.Phone))
                    {
                        await SendSMSNotificationAsync(shipment.AssignedDriver.Phone, driverMessage);
                        notifications.Add(new Notification
                        {
                            ShipmentId = shipment.Id,
                            Type = NotificationType.SMS,
                            Recipient = shipment.AssignedDriver.Phone,
                            Message = driverMessage,
                            Status = NotificationStatus.Sent,
                            SentAt = DateTime.UtcNow,
                        });
                    }
                }

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notifications for shipment {shipment.Id}");
            }
        }

        public async Task NotifyDriverAssignmentAsync(Guid shipmentId, Guid driverId)
        {
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.Sender)
                    .Include(s => s.AssignedDriver)
                    .FirstOrDefaultAsync(s => s.Id == shipmentId);

                if (shipment == null || shipment.AssignedDriver == null)
                {
                    _logger.LogWarning($"Shipment {shipmentId} or driver not found for notification");
                    return;
                }

                var notifications = new List<Notification>();

                // Notify Driver about new assignment
                var driverSubject = $"New Shipment Assignment - {shipment.TrackingNumber}";
                var driverMessage =
                    $"You have been assigned a new shipment {shipment.TrackingNumber}. " +
                    $"Origin: {shipment.OriginAddress}, Destination: {shipment.DestinationAddress}. " +
                    $"Receiver: {shipment.ReceiverName}, Phone: {shipment.ReceiverPhone}";

                await SendEmailNotificationAsync(shipment.AssignedDriver.Email, driverSubject, driverMessage);

                notifications.Add(new Notification
                {
                    ShipmentId = shipment.Id,
                    Type = NotificationType.Email,
                    Recipient = shipment.AssignedDriver.Email,
                    Message = driverMessage,
                    Status = NotificationStatus.Sent,
                    SentAt = DateTime.UtcNow,
                });

                if (!string.IsNullOrEmpty(shipment.AssignedDriver.Phone))
                {
                    var shortDriverMessage = $"New shipment {shipment.TrackingNumber} assigned. Check your email for details.";
                    await SendSMSNotificationAsync(shipment.AssignedDriver.Phone, shortDriverMessage);
                    notifications.Add(new Notification
                    {
                        ShipmentId = shipment.Id,
                        Type = NotificationType.SMS,
                        Recipient = shipment.AssignedDriver.Phone,
                        Message = shortDriverMessage,
                        Status = NotificationStatus.Sent,
                        SentAt = DateTime.UtcNow,
                    });
                }

                // Notify Sender about driver assignment
                if (shipment.Sender != null)
                {
                    var senderSubject = $"Driver Assigned - {shipment.TrackingNumber}";
                    var senderMessage =
                        $"A driver has been assigned to your shipment {shipment.TrackingNumber}. " +
                        $"Driver: {shipment.AssignedDriver.FullName}, Phone: {shipment.AssignedDriver.Phone}";

                    await SendEmailNotificationAsync(shipment.Sender.Email, senderSubject, senderMessage);

                    notifications.Add(new Notification
                    {
                        ShipmentId = shipment.Id,
                        Type = NotificationType.Email,
                        Recipient = shipment.Sender.Email,
                        Message = senderMessage,
                        Status = NotificationStatus.Sent,
                        SentAt = DateTime.UtcNow,
                    });

                    if (!string.IsNullOrEmpty(shipment.Sender.Phone))
                    {
                        var shortSenderMessage = $"Driver {shipment.AssignedDriver.FullName} assigned to shipment {shipment.TrackingNumber}.";
                        await SendSMSNotificationAsync(shipment.Sender.Phone, shortSenderMessage);
                        notifications.Add(new Notification
                        {
                            ShipmentId = shipment.Id,
                            Type = NotificationType.SMS,
                            Recipient = shipment.Sender.Phone,
                            Message = shortSenderMessage,
                            Status = NotificationStatus.Sent,
                            SentAt = DateTime.UtcNow,
                        });
                    }
                }

                // Notify Receiver about driver assignment
                var receiverSubject = $"Driver Assigned to Your Delivery - {shipment.TrackingNumber}";
                var receiverMessage =
                    $"A driver has been assigned to deliver your package {shipment.TrackingNumber}. " +
                    $"Driver: {shipment.AssignedDriver.FullName}, Phone: {shipment.AssignedDriver.Phone}";

                await SendEmailNotificationAsync(shipment.ReceiverEmail, receiverSubject, receiverMessage);

                notifications.Add(new Notification
                {
                    ShipmentId = shipment.Id,
                    Type = NotificationType.Email,
                    Recipient = shipment.ReceiverEmail,
                    Message = receiverMessage,
                    Status = NotificationStatus.Sent,
                    SentAt = DateTime.UtcNow,
                });

                if (!string.IsNullOrEmpty(shipment.ReceiverPhone))
                {
                    var shortReceiverMessage = $"Driver assigned to your delivery {shipment.TrackingNumber}. Track your shipment.";
                    await SendSMSNotificationAsync(shipment.ReceiverPhone, shortReceiverMessage);
                    notifications.Add(new Notification
                    {
                        ShipmentId = shipment.Id,
                        Type = NotificationType.SMS,
                        Recipient = shipment.ReceiverPhone,
                        Message = shortReceiverMessage,
                        Status = NotificationStatus.Sent,
                        SentAt = DateTime.UtcNow,
                    });
                }

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Driver assignment notifications sent for shipment {shipmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send driver assignment notifications for shipment {shipmentId}");
            }
        }
    }
}
