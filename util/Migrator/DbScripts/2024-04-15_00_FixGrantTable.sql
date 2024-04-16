-- Drop the existing primary key constraint
IF EXISTS (
    SELECT *
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_NAME = 'Grant'
    AND CONSTRAINT_NAME = 'PK_Grant'
)
BEGIN
    ALTER TABLE [dbo].[Grant]
    DROP CONSTRAINT [PK_Grant]
END

-- Drop and recreate view later after table is updated
IF EXISTS(SELECT * FROM sys.views WHERE [Name] = 'GrantView')
    BEGIN
        DROP VIEW [dbo].[GrantView]
    END
GO

-- Drop the [Id] column if it exists
IF EXISTS (
    SELECT *
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Grant'
    AND COLUMN_NAME = 'Id'
)
BEGIN
    ALTER TABLE [dbo].[Grant]
    DROP COLUMN [Id]
END

-- Add the new primary key constraint
ALTER TABLE [dbo].[Grant]
ADD CONSTRAINT [PK_Grant] PRIMARY KEY CLUSTERED ([Key])
GO

-- recreate view later after table is updated
CREATE VIEW [dbo].[GrantView]
AS
SELECT
    *
FROM
    [dbo].[Grant]
GO
