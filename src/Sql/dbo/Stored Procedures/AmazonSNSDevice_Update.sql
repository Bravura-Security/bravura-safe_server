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