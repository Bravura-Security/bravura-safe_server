CREATE PROCEDURE [dbo].[HubConnection_Create]
    @Id BIGINT OUTPUT,
    @ConnectionId NVARCHAR (2048),
    @Token UNIQUEIDENTIFIER,
    @MessageType NVARCHAR (32),
    @MessagePayload NVARCHAR (2048),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @TmpId bigint

    -- Check if @RevisionDate is NULL and set it to current UTC time if NULL
    IF @RevisionDate IS NULL
    BEGIN
        SET @RevisionDate = GETUTCDATE();
    END

    -- Check if a row with the given Token exists
    SELECT @TmpId = [Id]
    FROM [dbo].[HubConnection]
    WHERE [Token] = @Token;

    IF @TmpId IS NULL
    BEGIN
      -- If the Token doesn't exist, create a new row
        INSERT INTO [dbo].[HubConnection]
        (
            [ConnectionId],
            [Token],
            [MessageType],
            [MessagePayload],
            [CreationDate],
            [RevisionDate]
        )
        VALUES
        (
            @ConnectionId,
            @Token,
            @MessageType,
            @MessagePayload,
            @CreationDate,
            @RevisionDate
        )

        SET @Id = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
         UPDATE [dbo].[HubConnection]
         SET [ConnectionId] = @ConnectionId,
             [MessageType] = @MessageType,
             [MessagePayload] = @MessagePayload,
             [RevisionDate] = @RevisionDate
         WHERE [Token] = @Token
    END
END
