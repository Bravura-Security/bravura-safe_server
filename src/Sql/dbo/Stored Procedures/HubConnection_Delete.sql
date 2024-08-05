CREATE PROCEDURE [dbo].[HubConnection_Delete]
    @ConnectionId NVARCHAR (2048)
AS
BEGIN
    SET NOCOUNT ON
    DELETE
    FROM [dbo].[HubConnection]
    WHERE [ConnectionId] = @ConnectionId
END
