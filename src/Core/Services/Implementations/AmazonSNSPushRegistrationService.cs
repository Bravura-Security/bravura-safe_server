using Bit.Core.Enums;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Text.Json;
using Bit.Core.Models.Data;

namespace Bit.Core.Services;

internal class SafeFilterPolicy
{
    public List<string> recipientId { get; set; }
    public List<Dictionary<string, string>> deviceIdentifier { get; set; }
}

public class AmazonSNSPushRegistrationService : IPushRegistrationService
{
    private readonly GlobalSettings _globalSettings;
    private readonly IInstallationDeviceRepository _installationDeviceRepository;
    private readonly IAmazonSNSDeviceRepository _amazonSNSDeviceRepository;
    private AmazonSimpleNotificationServiceClient _client = null;

    public AmazonSNSPushRegistrationService(
    IInstallationDeviceRepository installationDeviceRepository,
    IAmazonSNSDeviceRepository amazonSNSDeviceRepository,
    GlobalSettings globalSettings)
    {
        _installationDeviceRepository = installationDeviceRepository;
        _amazonSNSDeviceRepository = amazonSNSDeviceRepository;
        _globalSettings = globalSettings;
        _client = new AmazonSimpleNotificationServiceClient(
                _globalSettings.Amazon.AccessKeyId,
                _globalSettings.Amazon.AccessKeySecret, 
                RegionEndpoint.GetBySystemName(_globalSettings.Amazon.Region)
            );

        try
        {
            // List topics
            var listTopicsRequest = new ListTopicsRequest();
            var listTopicsResponse = _client.ListTopicsAsync(listTopicsRequest).Result;

            bool topicFound = false;
            // Check if listing topics was successful
            if (listTopicsResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                // Iterate over the topics and print their ARNs
                foreach (var topic in listTopicsResponse.Topics)
                {
                    Console.WriteLine("AWS SNS: Topic ARN: " + topic.TopicArn);
                    if (topic.TopicArn.Equals(_globalSettings.Amazon.SNSTopicARN))
                    {
                        Console.WriteLine("AWS SNS: Found topic as specified in settings: " + topic.TopicArn);
                        topicFound = true;
                        break;
                    }
                }


                if (!topicFound)
                {
                    Console.WriteLine("AWS SNS: Unable to find topic as configured: " + _globalSettings.Amazon.SNSTopicARN);
                    /* **
                    try
                    {
                        // this is POC code that proves we can create a topic, but we should not
                        // be autocreating imo.
                        CreateSNSTopic();
                    }
                    catch (System.Exception)
                    {

                    }
                    *** */
                }
                
            }
            else
            {
                Console.WriteLine("AWS SNS: Failed to list topics.");
            }
        }
        catch (System.Exception)
        {
            Console.WriteLine("AWS SNS: Failed to list topics in AWS SNS.");
        }
	}

    private bool CreateSNSTopic()
    {
        // Create a new topic
        // topic creation can fail if insufficient permission within AWS console
        var createTopicRequest = new CreateTopicRequest
        {
            Name = "_SNS_BSAFE_AUTOCREATED_"
        };

        var createTopicResponse = _client.CreateTopicAsync(createTopicRequest).Result;

        // Check if creating the new topic was successful
        if (createTopicResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine("AWS SNS: New topic created: " + createTopicResponse.TopicArn);
            return true;
        }

        
        Console.WriteLine("AWS SNS: Failed to create the new topic.");
        return false;
    }

    internal static string StripPrefix(string prefixedValue)
    {
        if (prefixedValue != null && prefixedValue.Length > 36 && prefixedValue[36] == '_')
        {
            return prefixedValue[37..];
        }
        return prefixedValue;
    }

    public async Task CreateOrUpdateRegistrationAsync(string pushToken, string deviceId, string userId, string identifier, DeviceType type)
    {
        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return;
        }

        string platformARN = null;
        string topicARN = _globalSettings.Amazon.SNSTopicARN;
        switch (type)
        {
            case DeviceType.Android:
                platformARN = _globalSettings.Amazon.SNSPlatformARNAndroid;
                break;
            case DeviceType.iOS:
                platformARN = _globalSettings.Amazon.SNSPlatformARNIOS;
                break;
            case DeviceType.AndroidAmazon:
                platformARN = _globalSettings.Amazon.SNSPlatformARNAndroid;
                break;
            default:
                break;
        }
        //this will fail when pushToken is null
        var response = await _client.CreatePlatformEndpointAsync(
            new CreatePlatformEndpointRequest
            {
                PlatformApplicationArn = platformARN,
                Token = pushToken,
            }
        );
        var endpointARN = response.EndpointArn;
        var subscribeResponse = await _client.SubscribeAsync(
            new SubscribeRequest
            {
                Protocol = "application",
                Endpoint = endpointARN,
                TopicArn = topicARN,
                Attributes = new Dictionary<string, string>
                {
                    {"FilterPolicyScope", "MessageAttributes"},
                    {"FilterPolicy", JsonSerializer.Serialize<SafeFilterPolicy>(
                        new SafeFilterPolicy
                        {
                            deviceIdentifier = new List<Dictionary<string, string>> 
                                {
                                    new Dictionary<string, string> 
                                    {
                                        {"anything-but", StripPrefix(identifier) }
                                    }
                                },
                            recipientId = new List<string> {StripPrefix(userId)}
                        }
                    )}
                }
            }
        );

        // Check if the subscription was successful
        if (subscribeResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine("AWS SNS: Subscription successful. Endpoint subscribed to the topic.");
        }
        else
        {
            Console.WriteLine("AWS SNS: Failed to subscribe the endpoint to the topic.");
        }

        var subscriptionARN = subscribeResponse.SubscriptionArn;
        if (InstallationDeviceEntity.IsInstallationDeviceId(deviceId))
        {
            await _installationDeviceRepository.UpsertAsync(new InstallationDeviceEntity(deviceId));
        }

        var strippedDeviceId = new Guid(StripPrefix(deviceId));
        var deviceRegistration = await _amazonSNSDeviceRepository.GetByDeviceIDAsync(strippedDeviceId);
        if (deviceRegistration == null)
        {
            deviceRegistration = new Entities.AmazonSNSDevice
            {
                DeviceID = strippedDeviceId,
                EndpointARN = endpointARN,
                SubscriptionARN = subscriptionARN
            };
        }
        else
        {
            deviceRegistration.EndpointARN = endpointARN;
            deviceRegistration.SubscriptionARN = subscriptionARN;
        }
        await _amazonSNSDeviceRepository.UpsertAsync(deviceRegistration);
    }

    public async Task DeleteRegistrationAsync(string deviceId)
    {
        var deviceGuid = new Guid(StripPrefix(deviceId));
        var snsDevice = await _amazonSNSDeviceRepository.GetByDeviceIDAsync(deviceGuid);
        await _client.UnsubscribeAsync(
            new UnsubscribeRequest
            {
                SubscriptionArn = snsDevice.SubscriptionARN,
            }
        );
        await _client.DeleteEndpointAsync(
            new DeleteEndpointRequest
            {
                EndpointArn = snsDevice.EndpointARN,
            }
        );
        if (InstallationDeviceEntity.IsInstallationDeviceId(deviceId))
        {
            await _installationDeviceRepository.DeleteAsync(new InstallationDeviceEntity(deviceId));
        }
        await _amazonSNSDeviceRepository.DeleteAsync(deviceGuid);
    }

    public async Task AddUserRegistrationOrganizationAsync(IEnumerable<string> deviceIds, string organizationId)
    {
        organizationId = StripPrefix(organizationId);
        foreach (var devicId in deviceIds)
        {
            var snsDevice = await _amazonSNSDeviceRepository.GetByDeviceIDAsync(new Guid(StripPrefix(devicId)));
            GetSubscriptionAttributesResponse subscriptionAttributes = await _client.GetSubscriptionAttributesAsync(new GetSubscriptionAttributesRequest
            {
                SubscriptionArn = snsDevice.SubscriptionARN
            });
            var filterPolicy = JsonSerializer.Deserialize<SafeFilterPolicy>(subscriptionAttributes.Attributes["FilterPolicy"]);
            if (!filterPolicy.recipientId.Contains(organizationId))
            {
                filterPolicy.recipientId.Add(organizationId);
                var setSubscriptionAttributesResponse = await _client.SetSubscriptionAttributesAsync(new SetSubscriptionAttributesRequest
                {
                    SubscriptionArn = snsDevice.SubscriptionARN,
                    AttributeName = "FilterPolicy",
                    AttributeValue = JsonSerializer.Serialize<SafeFilterPolicy>(filterPolicy)
                });

                // Check if setting the filter policy was successful
                if (setSubscriptionAttributesResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("AWS SNS: Filter policy set successfully for the subscription.");
                }
                else
                {
                    Console.WriteLine("AWS SNS: Failed to set the filter policy for the subscription.");
                }
            }
        }
        if (deviceIds.Any() && InstallationDeviceEntity.IsInstallationDeviceId(deviceIds.First()))
        {
            var entities = deviceIds.Select(e => new InstallationDeviceEntity(e));
            await _installationDeviceRepository.UpsertManyAsync(entities.ToList());
        }
    }
    public async Task DeleteUserRegistrationOrganizationAsync(IEnumerable<string> deviceIds, string organizationId)
    {
        organizationId = StripPrefix(organizationId);
        foreach (var devicId in deviceIds)
        {
            var snsDevice = await _amazonSNSDeviceRepository.GetByDeviceIDAsync(new Guid(StripPrefix(devicId)));
            GetSubscriptionAttributesResponse subscriptionAttributes = await _client.GetSubscriptionAttributesAsync(new GetSubscriptionAttributesRequest
            {
                SubscriptionArn = snsDevice.SubscriptionARN,
            });
            var filterPolicy = JsonSerializer.Deserialize<SafeFilterPolicy>(subscriptionAttributes.Attributes["FilterPolicy"]);
            if (filterPolicy.recipientId.Contains(organizationId))
            {
                filterPolicy.recipientId.Remove(organizationId);
                await _client.SetSubscriptionAttributesAsync(new SetSubscriptionAttributesRequest
                {
                    SubscriptionArn = snsDevice.SubscriptionARN,
                    AttributeName = "FilterPolicy",
                    AttributeValue = JsonSerializer.Serialize<SafeFilterPolicy>(filterPolicy)
                });
            }
        }
        if (deviceIds.Any() && InstallationDeviceEntity.IsInstallationDeviceId(deviceIds.First()))
        {
            var entities = deviceIds.Select(e => new InstallationDeviceEntity(e));
            await _installationDeviceRepository.UpsertManyAsync(entities.ToList());
        }
    }
}
