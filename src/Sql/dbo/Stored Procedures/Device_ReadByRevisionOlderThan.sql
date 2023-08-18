CREATE PROCEDURE [dbo].[Device_ReadByRevisionOlderThan]
    @OlderThan DATETIME2 (7)
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[DeviceView]
    WHERE
        [RevisionDate] < @OlderThan
END