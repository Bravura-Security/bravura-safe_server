IF OBJECT_ID('dbo.HubConnection', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[HubConnection];
END

CREATE TABLE [dbo].[HubConnection](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [ConnectionId] [nvarchar](2048) NOT NULL,
    [Token] [uniqueidentifier] NOT NULL,
    [MessageType] [nvarchar](32) NULL,
    [MessagePayload] [nvarchar](2048) NULL,
    [CreationDate] [datetime2](7) NOT NULL,
    [RevisionDate] [datetime2](7) NOT NULL
 CONSTRAINT [PK_HubConnection] PRIMARY KEY CLUSTERED ([Id] ASC)
);

CREATE NONCLUSTERED INDEX [IX_HubConnection_Token] ON [dbo].[HubConnection]
(
    Token ASC
);
GO

CREATE OR ALTER VIEW [dbo].[HubConnectionView]
AS
SELECT
    *
FROM
    [dbo].HubConnection
GO


-- Recreate procedure [HubConnection_Create]
IF OBJECT_ID('[dbo].[HubConnection_Create]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[HubConnection_Create]
END
GO

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
GO

-- Recreate procedure [HubConnection_Delete]
IF OBJECT_ID('[dbo].[HubConnection_Delete]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].HubConnection_Delete
END
GO

CREATE PROCEDURE [dbo].HubConnection_Delete
    @ConnectionId NVARCHAR (2048)
AS
BEGIN
    SET NOCOUNT ON
    DELETE
    FROM [dbo].[HubConnection]
    WHERE [ConnectionId] = @ConnectionId
END
GO

-- Recreate procedure [HubConnection_ReadByDateSince]
IF OBJECT_ID('[dbo].[HubConnection_ReadByDateSince]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].HubConnection_ReadByDateSince
END
GO

CREATE PROCEDURE [dbo].HubConnection_ReadByDateSince
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
GO

-- Recreate procedure [HubConnection_ReadByDateSince]
IF OBJECT_ID('[dbo].[HubConnection_ReadByTokenDateSince]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].HubConnection_ReadByTokenDateSince
END
GO

CREATE PROCEDURE [dbo].HubConnection_ReadByTokenDateSince
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
GO



-- Recreate procedure [HubConnection_SaveNotificationPayload]
IF OBJECT_ID('[dbo].[HubConnection_SaveNotificationPayload]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[HubConnection_SaveNotificationPayload]
END
GO

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
GO

-- Recreate procedure [HubConnection_SaveNotificationPayload]
IF OBJECT_ID('[dbo].[HubConnection_GetByToken]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[HubConnection_GetByToken]
END
GO

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


-- Recreate procedure [HubConnection_DeleteByDateSince]
IF OBJECT_ID('[dbo].[HubConnection_DeleteByDateSince]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].HubConnection_DeleteByDateSince
END
GO

CREATE PROCEDURE [dbo].HubConnection_DeleteByDateSince
    @DateOlderThan DATETIME2 (7)
AS
BEGIN
    SET NOCOUNT ON
    DELETE
    FROM [dbo].[HubConnection]
    WHERE [RevisionDate] < @DateOlderThan
END;
