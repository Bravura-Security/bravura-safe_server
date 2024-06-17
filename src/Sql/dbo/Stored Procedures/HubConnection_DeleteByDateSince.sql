CREATE PROCEDURE [dbo].[HubConnection_DeleteByDateSince]
    @DateOlderThan DATETIME2 (7)
AS
BEGIN
    SET NOCOUNT ON
    DELETE
    FROM [dbo].[HubConnection]
    WHERE [RevisionDate] < @DateOlderThan
END;
