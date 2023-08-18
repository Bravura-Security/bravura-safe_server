CREATE TABLE [dbo].[AmazonSNSDevice] (
    [Id]                BIGINT           IDENTITY (1, 1) NOT NULL,
    [DeviceId]          UNIQUEIDENTIFIER NOT NULL,
    [EndpointARN]       NVARCHAR (2048) NOT NULL,
    [SubscriptionARN]   NVARCHAR (2048) NOT NULL,
    [CreationDate]      DATETIME2 (7)    NOT NULL,
    CONSTRAINT [PK_AmazonSNSDevice] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AmazonSNSDevice_Device] FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[Device] ([Id]),
);