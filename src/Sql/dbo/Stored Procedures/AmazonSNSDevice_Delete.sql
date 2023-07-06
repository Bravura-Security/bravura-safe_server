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