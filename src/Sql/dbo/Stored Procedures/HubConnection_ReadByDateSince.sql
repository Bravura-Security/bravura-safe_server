CREATE PROCEDURE [dbo].[HubConnection_ReadByDateSince]
    @NewerThan DATETIME2 (7),
    @ReturnEmptyPayload BIT
AS
BEGIN
    SET NOCOUNT ON
    IF @ReturnEmptyPayload = 1
    BEGIN
        SELECT
            *
        FROM
            [dbo].[HubConnectionView]
        WHERE
            [RevisionDate] > @NewerThan
    END
    ELSE
    BEGIN
        -- Return rows where MessagePayload IS NOT NULL
        SELECT
            *
        FROM
            [dbo].[HubConnectionView]
        WHERE
            [RevisionDate] > @NewerThan AND [MessagePayload] IS NOT NULL;
    END;
END
