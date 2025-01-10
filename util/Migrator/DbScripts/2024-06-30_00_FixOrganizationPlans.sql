IF OBJECT_ID('[dbo].[Organization_DemoteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_DemoteById]
END
GO
CREATE PROCEDURE [dbo].[Organization_DemoteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN

    SET NOCOUNT ON

    UPDATE
        [dbo].[Organization]
    SET
        [Plan] = 'Bravura Teams',
        [PlanType] = 98,
        [Seats] = 32767,
        [Use2fa] = 0,
        [UseSso] = 0,
        [UseResetPassword] = 0,
        [Skip2faForSso] = 0,
        [RevisionDate] = GETUTCDATE()
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[Organization_PromoteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_PromoteById]
END
GO
CREATE PROCEDURE [dbo].[Organization_PromoteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN

    SET NOCOUNT ON

    UPDATE
        [dbo].[Organization]
    SET
        [Plan] = 'Bravura Enterprise',
        [PlanType] = 99,
        [Seats] = 1000000,
        [Use2fa] = 1,
        [UseSso] = 1,
        [UseResetPassword] = 1,
        [Skip2faForSso] = 1,
        [RevisionDate] = GETUTCDATE()
    WHERE
        [Id] = @Id
END
GO

UPDATE [dbo].[Organization] SET [PlanType] = 98, [Plan] = 'Bravura Teams' WHERE [PlanType] = 12;
UPDATE [dbo].[Organization] SET [PlanType] = 99, [Plan] = 'Bravura Enterprise' WHERE [PlanType] = 13;
