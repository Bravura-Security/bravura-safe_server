IF OBJECT_ID('[dbo].[AmazonSNSDevice]') IS NULL
BEGIN
CREATE TABLE [dbo].[AmazonSNSDevice] (
    [Id]                BIGINT           IDENTITY (1, 1) NOT NULL,
    [DeviceId]          UNIQUEIDENTIFIER NOT NULL,
    [EndpointARN]       NVARCHAR (2048) NOT NULL,
    [SubscriptionARN]   NVARCHAR (2048) NOT NULL,
    [CreationDate]      DATETIME2 (7)    NOT NULL,
    CONSTRAINT [PK_AmazonSNSDevice] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AmazonSNSDevice_Device] FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[Device] ([Id]),
);
END
GO

IF EXISTS(SELECT * FROM sys.views WHERE [Name] = 'AmazonSNSDeviceView')
BEGIN
    DROP VIEW [dbo].[AmazonSNSDeviceView]
END
GO

CREATE VIEW [dbo].[AmazonSNSDeviceView]
AS
SELECT
    *
FROM
    [dbo].[AmazonSNSDevice]
GO

IF OBJECT_ID('[dbo].[AmazonSNSDevice_Create]') IS NULL
BEGIN
    DROP PROCEDURE [dbo].[AmazonSNSDevice_Create]
END
GO

CREATE PROCEDURE [dbo].[AmazonSNSDevice_Create]
    @Id BIGINT OUTPUT,
    @DeviceId UNIQUEIDENTIFIER,
    @EndpointARN NVARCHAR (2048),
    @SubscriptionARN NVARCHAR (2048),
    @CreationDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO [dbo].[AmazonSNSDevice]
    (
        [DeviceId],
        [EndpointARN],
        [SubscriptionARN],
        [CreationDate]
    )
    VALUES
    (
        @DeviceId,
        @EndpointARN,
        @SubscriptionARN,
        @CreationDate
    )

    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('[dbo].[AmazonSNSDevice_Update]') IS NULL
BEGIN
    DROP PROCEDURE [dbo].[AmazonSNSDevice_Update]
END
GO

CREATE PROCEDURE [dbo].[AmazonSNSDevice_Update]
    @Id BIGINT OUTPUT,
    @DeviceId UNIQUEIDENTIFIER,
    @EndpointARN NVARCHAR (2048),
    @SubscriptionARN NVARCHAR (2048),
    @CreationDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        [dbo].[AmazonSNSDevice]
    SET
        [DeviceId] = @DeviceId,
        [EndpointARN] = @EndpointARN,
        [SubscriptionARN] = @SubscriptionARN,
        [CreationDate] = @CreationDate
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[AmazonSNSDevice_Delete]') IS NULL
BEGIN
    DROP PROCEDURE [dbo].[AmazonSNSDevice_Delete]
END
GO

CREATE PROCEDURE [dbo].[AmazonSNSDevice_Delete]
    @DeviceId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    DELETE
    FROM
        [dbo].[AmazonSNSDevice]
    WHERE
        [DeviceId] = @DeviceId
END
GO

IF OBJECT_ID('[dbo].[AmazonSNSDevice_ReadByDeviceId]') IS NULL
BEGIN
    DROP PROCEDURE [dbo].[AmazonSNSDevice_ReadByDeviceId]
END
GO

CREATE PROCEDURE [dbo].[AmazonSNSDevice_ReadByDeviceId]
    @DeviceId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[AmazonSNSDeviceView]
    WHERE
        [DeviceId] = @DeviceId
END
GO