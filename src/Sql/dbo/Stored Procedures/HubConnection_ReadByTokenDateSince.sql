CREATE PROCEDURE [dbo].[HubConnection_ReadByTokenDateSince]
    @Token UNIQUEIDENTIFIER,
    @NewerThan DATETIME2 (7),
    @ReturnEmptyPayload BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ReturnEmptyPayload = 1
    BEGIN
        -- Return all rows, regardless of MessagePayload
        SELECT *
        FROM [dbo].[HubConnectionView]
        WHERE [RevisionDate] > @NewerThan
            AND [Token] = @Token;
    END
    ELSE
    BEGIN
        -- Return rows where MessagePayload IS NOT NULL
        SELECT *
        FROM [dbo].[HubConnectionView]
        WHERE [RevisionDate] > @NewerThan
            AND [Token] = @Token
            AND [MessagePayload] IS NOT NULL;
    END;
END
