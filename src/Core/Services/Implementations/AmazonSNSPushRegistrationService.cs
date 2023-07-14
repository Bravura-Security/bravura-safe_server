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
        var subscriptionARN = subscribeResponse.SubscriptionArn;
        if (InstallationDeviceEntity.IsInstallationDeviceId(deviceId))
        {
            await _installationDeviceRepository.UpsertAsync(new InstallationDeviceEntity(deviceId));
        }
        await _amazonSNSDeviceRepository.UpsertAsync(
            new Entities.AmazonSNSDevice { DeviceID = new Guid(StripPrefix(deviceId)), EndpointARN = endpointARN, SubscriptionARN = subscriptionARN }
            );
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
