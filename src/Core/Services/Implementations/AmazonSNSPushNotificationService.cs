using System.Text.Json;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Models;
using Bit.Core.Models.Data;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Bit.Core.Vault.Entities;
using Bit.Core.Tools.Entities;
using Bit.Core.Auth.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections;
using IdentityServer4.Extensions;

namespace Bit.Core.Services;

public class AmazonSNSPushNotificationService : IPushNotificationService
{

    private readonly IInstallationDeviceRepository _installationDeviceRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly GlobalSettings _globalSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private AmazonSimpleNotificationServiceClient _client = null;
    private ILogger _logger;

    public AmazonSNSPushNotificationService(
        IInstallationDeviceRepository installationDeviceRepository,
        IDeviceRepository deviceRepository,
        GlobalSettings globalSettings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<NotificationsApiPushNotificationService> logger)
    {

        _installationDeviceRepository = installationDeviceRepository;
        _deviceRepository = deviceRepository;
        _globalSettings = globalSettings;
        _httpContextAccessor = httpContextAccessor;
        _client = new AmazonSimpleNotificationServiceClient(
                _globalSettings.Amazon.AccessKeyId,
                _globalSettings.Amazon.AccessKeySecret,
                RegionEndpoint.GetBySystemName(_globalSettings.Amazon.Region)
            );
        _logger = logger;
    }

    public async Task PushSyncCipherCreateAsync(Cipher cipher, IEnumerable<Guid> collectionIds)
    {
        await PushCipherAsync(cipher, PushType.SyncCipherCreate, collectionIds);
    }

    public async Task PushSyncCipherUpdateAsync(Cipher cipher, IEnumerable<Guid> collectionIds)
    {
        await PushCipherAsync(cipher, PushType.SyncCipherUpdate, collectionIds);
    }

    public async Task PushSyncCipherDeleteAsync(Cipher cipher)
    {
        await PushCipherAsync(cipher, PushType.SyncLoginDelete, null);
    }

    private async Task PushCipherAsync(Cipher cipher, PushType type, IEnumerable<Guid> collectionIds)
    {
        if (cipher.OrganizationId.HasValue)
        {
            // We cannot send org pushes since access logic is much more complicated than just the fact that they belong
            // to the organization. Potentially we could blindly send to just users that have the access all permission
            // device registration needs to be more granular to handle that appropriately. A more brute force approach could
            // me to send "full sync" push to all org users, but that has the potential to DDOS the API in bursts.

            // await SendPayloadToOrganizationAsync(cipher.OrganizationId.Value, type, message, true);
        }
        else if (cipher.UserId.HasValue)
        {
            var message = new SyncCipherPushNotification
            {
                Id = cipher.Id,
                UserId = cipher.UserId,
                OrganizationId = cipher.OrganizationId,
                RevisionDate = cipher.RevisionDate,
                CollectionIds = collectionIds,
            };

            await SendPayloadToUserAsync(cipher.UserId.Value, type, message, true);
        }
    }

    public async Task PushSyncFolderCreateAsync(Folder folder)
    {
        await PushFolderAsync(folder, PushType.SyncFolderCreate);
    }

    public async Task PushSyncFolderUpdateAsync(Folder folder)
    {
        await PushFolderAsync(folder, PushType.SyncFolderUpdate);
    }

    public async Task PushSyncFolderDeleteAsync(Folder folder)
    {
        await PushFolderAsync(folder, PushType.SyncFolderDelete);
    }

    private async Task PushFolderAsync(Folder folder, PushType type)
    {
        var message = new SyncFolderPushNotification
        {
            Id = folder.Id,
            UserId = folder.UserId,
            RevisionDate = folder.RevisionDate
        };

        await SendPayloadToUserAsync(folder.UserId, type, message, true);
    }

    public async Task PushSyncCiphersAsync(Guid userId)
    {
        await PushUserAsync(userId, PushType.SyncCiphers);
    }

    public async Task PushSyncVaultAsync(Guid userId)
    {
        await PushUserAsync(userId, PushType.SyncVault);
    }

    public async Task PushSyncOrgKeysAsync(Guid userId)
    {
        await PushUserAsync(userId, PushType.SyncOrgKeys);
    }

    public async Task PushSyncSettingsAsync(Guid userId)
    {
        await PushUserAsync(userId, PushType.SyncSettings);
    }

    public async Task PushLogOutAsync(Guid userId, bool excludeCurrentContext = false)
    {
        await PushUserAsync(userId, PushType.LogOut, excludeCurrentContext);
    }

    private async Task PushUserAsync(Guid userId, PushType type, bool excludeCurrentContext = false)
    {
        var message = new UserPushNotification
        {
            UserId = userId,
            Date = DateTime.UtcNow
        };

        await SendPayloadToUserAsync(userId, type, message, excludeCurrentContext);
    }

    public async Task PushSyncSendCreateAsync(Send send)
    {
        await PushSendAsync(send, PushType.SyncSendCreate);
    }

    public async Task PushSyncSendUpdateAsync(Send send)
    {
        await PushSendAsync(send, PushType.SyncSendUpdate);
    }

    public async Task PushSyncSendDeleteAsync(Send send)
    {
        await PushSendAsync(send, PushType.SyncSendDelete);
    }

    private async Task PushSendAsync(Send send, PushType type)
    {
        if (send.UserId.HasValue)
        {
            var message = new SyncSendPushNotification
            {
                Id = send.Id,
                UserId = send.UserId.Value,
                RevisionDate = send.RevisionDate
            };

            await SendPayloadToUserAsync(message.UserId, type, message, true);
        }
    }

    public async Task PushAuthRequestAsync(AuthRequest authRequest)
    {
        await PushAuthRequestAsync(authRequest, PushType.AuthRequest);
    }

    public async Task PushAuthRequestResponseAsync(AuthRequest authRequest)
    {
        await PushAuthRequestAsync(authRequest, PushType.AuthRequestResponse);
    }

    private async Task PushAuthRequestAsync(AuthRequest authRequest, PushType type)
    {
        var message = new AuthRequestPushNotification
        {
            Id = authRequest.Id,
            UserId = authRequest.UserId
        };

        await SendPayloadToUserAsync(authRequest.UserId, type, message, true);
    }

    private async Task SendPayloadToUserAsync(Guid userId, PushType type, object payload, bool excludeCurrentContext)
    {
        await SendPayloadToUserAsync(userId.ToString(), type, payload, GetContextIdentifier(excludeCurrentContext));
    }

    private string GetContextIdentifier(bool excludeCurrentContext)
    {
        if (!excludeCurrentContext)
        {
            return null;
        }

        var currentContext = _httpContextAccessor?.HttpContext?.
            RequestServices.GetService(typeof(ICurrentContext)) as ICurrentContext;
        return currentContext?.DeviceIdentifier;
    }
    public async Task SendPayloadToOrganizationAsync(string orgId, PushType type, object payload, string identifier, string deviceId = null) => await SendPayloadAsync(orgId, type, payload, identifier, deviceId);
    public async Task SendPayloadToUserAsync(string userId, PushType type, object payload, string identifier, string deviceId = null) => await SendPayloadAsync(userId, type, payload, identifier, deviceId);

    private async Task SendPayloadAsync(string Id, PushType type, object payload, string identifier, string deviceId = null)
    {
        var messageData = new Dictionary<string, IDictionary>
            {
                {"data", new Dictionary<string, string>
                    {
                        { "type",  ((byte)type).ToString() },
                        { "payload", JsonSerializer.Serialize(payload) }
                    }
                }
            };
        var messageStr = JsonSerializer.Serialize(messageData);
        //Google Android devices need an extra nested data key:
        var messageAndroid = new Dictionary<string, IDictionary>
            {
                {"data", messageData }
            };
        var messageStrAndroid = JsonSerializer.Serialize(messageAndroid);
        //IOS devices need the aps key:
        var messageIOS = new Dictionary<string, IDictionary>(messageData)
        {
            { "aps", new Dictionary<string, int> { { "content-available", 1 } } }
        };
        var messageStrIOS = JsonSerializer.Serialize(messageIOS);
        var message = new Dictionary<string, string>
        {
            {"GCM", messageStrAndroid },
            {"APNS", messageStrIOS },
            {"ADM", messageStr },
            {"default", messageStr }
        };

        var messageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "recipientId", new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = Id
                        }
                    }
                };
        if(!identifier.IsNullOrEmpty())
        {
            messageAttributes.Add("deviceIdentifier", new MessageAttributeValue
            {
                DataType = "String",
                StringValue = identifier
            }
            );
        };
        await _client.PublishAsync(new PublishRequest
        {
            TopicArn = _globalSettings.Amazon.SNSTopicARN,
            MessageStructure = "json",
            Message = JsonSerializer.Serialize(message),
            MessageAttributes = messageAttributes
        }
        );

        if (InstallationDeviceEntity.IsInstallationDeviceId(deviceId))
        {
            await _installationDeviceRepository.UpsertAsync(new InstallationDeviceEntity(deviceId));
        }
    }
}
