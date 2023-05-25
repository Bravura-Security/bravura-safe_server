
-- Update existing Enterprise Customers to allow 10 GB of storage per user.
UPDATE  [dbo].[User]
SET     [MaxStorageGb] = 100;
GO
