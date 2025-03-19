-- Organization_DemoteById, Organization_PromoteById
IF OBJECT_ID('[dbo].[Organization_DemoteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_DemoteById]
END
GO
CREATE PROCEDURE [dbo].[Organization_DemoteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN

    SET NOCOUNT ON

    UPDATE
        [dbo].[Organization]
    SET
        [Plan] = 'Bravura Teams',
        [PlanType] = 98,
        [Seats] = 32767,
        [Use2fa] = 0,
        [UseSso] = 0,
        [UseResetPassword] = 0,
        [Skip2faForSso] = 0,
        [RevisionDate] = GETUTCDATE()
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[Organization_PromoteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_PromoteById]
END
GO
CREATE PROCEDURE [dbo].[Organization_PromoteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN

    SET NOCOUNT ON

    UPDATE
        [dbo].[Organization]
    SET
        [Plan] = 'Bravura Enterprise',
        [PlanType] = 99,
        [Seats] = 1000000,
        [Use2fa] = 1,
        [UseSso] = 1,
        [UseResetPassword] = 1,
        [Skip2faForSso] = 1,
        [RevisionDate] = GETUTCDATE()
    WHERE
        [Id] = @Id
END
GO

UPDATE [dbo].[Organization] SET [PlanType] = 98, [Plan] = 'Bravura Teams' WHERE [PlanType] = 12;
UPDATE [dbo].[Organization] SET [PlanType] = 99, [Plan] = 'Bravura Enterprise' WHERE [PlanType] = 13;
GO

-- OrganizationUserOrganizationDetailsView
CREATE OR ALTER VIEW [dbo].[OrganizationUserOrganizationDetailsView]
AS
SELECT
    OU.[UserId],
    OU.[OrganizationId],
    OU.[Id] OrganizationUserId,
    O.[Name],
    O.[Enabled],
    O.[PlanType],
    O.[UsePolicies],
    O.[UseSso],
    O.[UseKeyConnector],
    O.[UseScim],
    O.[UseGroups],
    O.[UseDirectory],
    O.[UseEvents],
    O.[UseTotp],
    O.[Use2fa],
    O.[UseApi],
    O.[UseResetPassword],
    O.[SelfHost],
    O.[UsersGetPremium],
    O.[UseCustomPermissions],
    O.[UseSecretsManager],
    O.[Seats],
    O.[MaxCollections],
    O.[MaxStorageGb],
    O.[Identifier],
    OU.[Key],
    OU.[ResetPasswordKey],
    O.[PublicKey],
    O.[PrivateKey],
    OU.[Status],
    OU.[Type],
    SU.[ExternalId] SsoExternalId,
    OU.[Permissions],
    PO.[ProviderId],
    P.[Name] ProviderName,
    P.[Type] ProviderType,
    SS.[Data] SsoConfig,
    OS.[FriendlyName] FamilySponsorshipFriendlyName,
    OS.[LastSyncDate] FamilySponsorshipLastSyncDate,
    OS.[ToDelete] FamilySponsorshipToDelete,
    OS.[ValidUntil] FamilySponsorshipValidUntil,
    OU.[AccessSecretsManager],
    O.[UsePasswordManager],
    O.[SmSeats],
    O.[SmServiceAccounts],
    O.[LimitCollectionCreationDeletion],
    O.[AllowAdminAccessToAllCollectionItems],
    O.[FlexibleCollections],
    O.[Skip2faForSso],
    OU.[ForcePasswordReset]
FROM
    [dbo].[OrganizationUser] OU
LEFT JOIN
    [dbo].[Organization] O ON O.[Id] = OU.[OrganizationId]
LEFT JOIN
    [dbo].[SsoUser] SU ON SU.[UserId] = OU.[UserId] AND SU.[OrganizationId] = OU.[OrganizationId]
LEFT JOIN
    [dbo].[ProviderOrganization] PO ON PO.[OrganizationId] = O.[Id]
LEFT JOIN
    [dbo].[Provider] P ON P.[Id] = PO.[ProviderId]
LEFT JOIN
    [dbo].[SsoConfig] SS ON SS.[OrganizationId] = OU.[OrganizationId]
LEFT JOIN
    [dbo].[OrganizationSponsorship] OS ON OS.[SponsoringOrganizationUserID] = OU.[Id]
GO

-- Refresh modules for SPROCs reliant on 'OrganizationUserOrganizationDetailsView'.
IF OBJECT_ID('[dbo].[OrganizationUserOrganizationDetails_ReadByUserIdStatus]') IS NOT NULL
BEGIN
    EXECUTE sp_refreshsqlmodule N'[dbo].[OrganizationUserOrganizationDetails_ReadByUserIdStatus]';
END
GO

IF OBJECT_ID('[dbo].[OrganizationUserOrganizationDetails_ReadByUserIdStatusOrganizationId]') IS NOT NULL
BEGIN
    EXECUTE sp_refreshsqlmodule N'[dbo].[OrganizationUserOrganizationDetails_ReadByUserIdStatusOrganizationId]';
END
GO
