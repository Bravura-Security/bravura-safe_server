CREATE PROCEDURE [dbo].[HubConnection_SaveNotificationPayload]
    @Token UNIQUEIDENTIFIER,
    @MessageType NVARCHAR (32),
    @MessagePayload NVARCHAR (2048)
AS
BEGIN
 SET NOCOUNT ON

 DECLARE @Id bigint

    -- Check if a row with the given Token exists
    SELECT @Id = [Id]
    FROM [dbo].[HubConnection]
    WHERE [Token] = @Token;

    IF @Id IS NULL
    BEGIN
        -- If the Token doesn't exist, create a new row
        INSERT INTO [dbo].[HubConnection] ([ConnectionId], [Token], [MessageType], [MessagePayload], [CreationDate], [RevisionDate])
        VALUES ('REPLACEME', @Token, @MessageType, @MessagePayload, GETUTCDATE(), GETUTCDATE());
    END
    ELSE
    BEGIN
         UPDATE [dbo].[HubConnection]

         SET [MessageType] = @MessageType,
             [MessagePayload] = @MessagePayload,
             [RevisionDate] = GETUTCDATE()
         WHERE [Token] = @Token
     END

END
