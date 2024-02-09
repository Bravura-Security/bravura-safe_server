  WITH DuplicateCTE AS (
    SELECT
        [Id],
        [DeviceId],
        [EndpointARN],
        [SubscriptionARN],
        [CreationDate],
        ROW_NUMBER() OVER (PARTITION BY [DeviceId], [EndpointARN], [SubscriptionARN], [CreationDate] ORDER BY [Id]) AS RowNum
    FROM
        [dbo].[AmazonSNSDevice]
)

DELETE FROM DuplicateCTE
WHERE RowNum > 1;

GO
