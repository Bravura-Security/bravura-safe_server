CREATE PROCEDURE [dbo].[HubConnection_GetByToken]
    @MessageType nvarchar(32),
    @Token uniqueidentifier,
    @Timestamp datetime2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [dbo].[HubConnection]
    WHERE [MessageType] = @MessageType
        AND [Token] = @Token
        AND [RevisionDate] > @Timestamp
    ORDER BY [RevisionDate] ASC

END;
