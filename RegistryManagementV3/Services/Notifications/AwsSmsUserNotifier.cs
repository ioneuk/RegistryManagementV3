using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace RegistryManagementV3.Services.Notifications
{
    public class AwsSmsUserNotifier : ISmsUserNotifier
    {
        private const string AwsNotificationProtocolName = "sms";
        private readonly IAmazonSimpleNotificationService _snsClient;
        
        private readonly IDictionary<NotificationType, string> _topicNames = new Dictionary<NotificationType, string>
        {
            { NotificationType.RmAuthOtp, "rm_auth_otp" },
            { NotificationType.RmPermissionChanged, "rm_registration_approved"},
            { NotificationType.RmRegistrationApproved, "rm_permission_changed"}
        };

        public AwsSmsUserNotifier(IAmazonSimpleNotificationService snsClient)
        {
            _snsClient = snsClient;
        }

        public async Task NotifyAsync(SmsNotificationDto smsNotification)
        {
            await CreateSnsTopic(_topicNames[smsNotification.NotificationType])
                .ContinueWith(topicResponse => PublishNotificationToCreatedTopic(topicResponse.Result, smsNotification));
        }
        
        private Task<CreateTopicResponse> CreateSnsTopic(string topicName)
        {
            var createTopicRequest = new CreateTopicRequest(topicName);
            return _snsClient.CreateTopicAsync(createTopicRequest);
        }

        private Task PublishNotificationToCreatedTopic(CreateTopicResponse topicResponse,
            SmsNotificationDto smsNotification)
        {
            return SubscribeToTopic(topicResponse, AwsNotificationProtocolName,
                    smsNotification.PhoneNumbers)
                .ContinueWith(response => PublishMessageToTopic(smsNotification.Content, topicResponse.TopicArn))
                .ContinueWith(response => DeleteTopic(topicResponse.TopicArn));
        }
        
        private Task<ConfirmSubscriptionResponse[]> SubscribeToTopic(CreateTopicResponse topicResponse, string protocol,
            IEnumerable<string> endpoints)
        {
            var subscriptionConfirmationResponses = endpoints
                .Select(endpoint => new SubscribeRequest(topicResponse.TopicArn, protocol, endpoint))
                .Select(subscribeRequest =>  _snsClient.SubscribeAsync(subscribeRequest)
                    .ContinueWith(subscribeResponse => _snsClient.ConfirmSubscriptionAsync(topicResponse.TopicArn, subscribeResponse.Result.SubscriptionArn)).Result);
            return Task.WhenAll(subscriptionConfirmationResponses.ToArray());
        }

        private Task PublishMessageToTopic(string content, string topicArn)
        {
            var pubRequest = new PublishRequest {Message = content, TopicArn = topicArn};
            return _snsClient.PublishAsync(pubRequest);
        }
        
        private Task DeleteTopic(string topicArn)
        {
            return _snsClient.DeleteTopicAsync(topicArn);
        }
    }
}