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
