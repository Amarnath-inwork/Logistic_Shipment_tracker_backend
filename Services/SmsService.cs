using SendGrid;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Logistic_Shipment_tracker.Services
{
    public class SmsService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IConfiguration config , ILogger<SmsService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendSmsAsync(string toPhoneNumber, string message)
        {
            try
            {
                var accountSid = _config["Twilio:AccountSid"];
                var authToken = _config["Twilio:AuthToken"];
                var fromPhone = _config["Twilio:FromPhone"];

                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhone))
                {
                    _logger.LogWarning("Twilio configuration is missing");
                    return;
                }
                TwilioClient.Init(accountSid, authToken);

                var messageResource = await MessageResource.CreateAsync(
                        to: new PhoneNumber(toPhoneNumber),
                        from: new PhoneNumber(fromPhone),
                        body: message
                    );
                _logger.LogInformation($"SMS sent to {toPhoneNumber} , SID: {messageResource.Sid}");

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Failed to send SMS to {toPhoneNumber}");
                throw;
            }

        }
    }
}
