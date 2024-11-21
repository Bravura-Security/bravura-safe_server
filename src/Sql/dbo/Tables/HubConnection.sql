CREATE TABLE [dbo].[HubConnection](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [ConnectionId] [NVARCHAR](2048) NOT NULL,
    [Token] [UNIQUEIDENTIFIER] NOT NULL,
    [MessageType] [NVARCHAR](32) NULL,
    [MessagePayload] [NVARCHAR](2048) NULL,
    [CreationDate] [DATETIME2](7) NOT NULL,
    [RevisionDate] [DATETIME2](7) NOT NULL
    CONSTRAINT [PK_HubConnection] PRIMARY KEY CLUSTERED ([Id] ASC)
);

GO

CREATE NONCLUSTERED INDEX [IX_HubConnection_Token] ON [dbo].[HubConnection]
(
    [Token] ASC
);
GO
