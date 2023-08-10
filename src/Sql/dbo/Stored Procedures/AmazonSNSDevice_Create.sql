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