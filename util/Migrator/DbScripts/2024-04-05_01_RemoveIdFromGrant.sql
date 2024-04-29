IF NOT EXISTS (
    SELECT *  FROM sys.indexes  WHERE [Name] = 'IX_Grant_SubjectId_ClientId_Type'
    AND object_id = OBJECT_ID('[dbo].[Grant]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Grant_SubjectId_ClientId_Type]
        ON [dbo].[Grant]([SubjectId] ASC, [ClientId] ASC, [Type] ASC)
END
GO

IF NOT EXISTS (
    SELECT *  FROM sys.indexes  WHERE [Name] = 'IX_Grant_SubjectId_SessionId_Type'
    AND object_id = OBJECT_ID('[dbo].[Grant]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Grant_SubjectId_SessionId_Type]
        ON [dbo].[Grant]([SubjectId] ASC, [SessionId] ASC, [Type] ASC)
END
GO

IF EXISTS(SELECT name FROM sys.indexes WHERE name = 'IX_Grant_Key')
BEGIN
    DROP INDEX [IX_Grant_Key] ON [dbo].[Grant]
END
GO

IF COL_LENGTH('[dbo].[Grant]', 'Id') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Grant]
        DROP CONSTRAINT [PK_Grant];

    ALTER TABLE [dbo].[Grant]
        DROP COLUMN [Id]

    ALTER TABLE [dbo].[Grant]
        ADD CONSTRAINT [PK_Grant] PRIMARY KEY CLUSTERED ([Key] ASC);
END
GO

IF EXISTS(SELECT *
FROM sys.views
WHERE [Name] = 'GrantView')
    BEGIN
    DROP VIEW [dbo].[GrantView];
    END
GO

CREATE VIEW [dbo].[GrantView]
AS
    SELECT
        *
    FROM
        [dbo].[Grant]
GO
