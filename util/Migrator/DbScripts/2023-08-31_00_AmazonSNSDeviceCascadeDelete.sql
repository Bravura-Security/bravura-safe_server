ALTER TABLE [dbo].[AmazonSNSDevice] DROP CONSTRAINT [FK_AmazonSNSDevice_Device]
GO

ALTER TABLE [dbo].[AmazonSNSDevice]  WITH CHECK ADD  CONSTRAINT [FK_AmazonSNSDevice_Device] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Device] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[AmazonSNSDevice] CHECK CONSTRAINT [FK_AmazonSNSDevice_Device]
GO

DELETE FROM [dbo].[Device]
GO