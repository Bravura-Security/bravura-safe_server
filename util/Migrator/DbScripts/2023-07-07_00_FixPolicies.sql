UPDATE [dbo].[Policy]
SET [Data] = (SELECT
    CASE WHEN JSON_VALUE([Data],'$.minComplexity') IS NOT NULL THEN
        CASE WHEN ISNUMERIC(JSON_VALUE([Data],'$.minComplexity')) = 1 THEN JSON_MODIFY([Data],'$.minComplexity',CAST(JSON_VALUE([Data],'$.minComplexity') AS INT))
        ELSE JSON_MODIFY([Data],'$.minComplexity', NULL)
        END
    ELSE [Data]
    END
)
WHERE [Type] = 1 AND ISJSON([Data]) = 1
GO

UPDATE [dbo].[Policy]
SET [Data] = (SELECT
    CASE WHEN JSON_VALUE([Data],'$.minLength') IS NOT NULL THEN
        CASE WHEN ISNUMERIC(JSON_VALUE([Data],'$.minLength')) = 1 THEN JSON_MODIFY([Data],'$.minLength',CAST(JSON_VALUE([Data],'$.minLength') AS INT))
        ELSE JSON_MODIFY([Data],'$.minLength', NULL)
        END
    ELSE [Data]
    END
)
WHERE [Type] = 1 AND ISJSON([Data]) = 1
GO

UPDATE [dbo].[Policy]
SET [Data] = (SELECT
    CASE WHEN JSON_VALUE([Data],'$.requireUpper') IS NOT NULL THEN
        CASE WHEN ISNUMERIC(JSON_VALUE([Data],'$.requireUpper')) = 0 THEN JSON_MODIFY([Data],'$.requireUpper',CAST(JSON_VALUE([Data],'$.requireUpper') AS BIT))
        ELSE JSON_MODIFY([Data],'$.requireUpper', NULL)
        END
    ELSE [Data]
    END
)
WHERE [Type] = 1 AND ISJSON([Data]) = 1
GO

UPDATE [dbo].[Policy]
SET [Data] = (SELECT
    CASE WHEN JSON_VALUE([Data],'$.requireLower') IS NOT NULL THEN
        CASE WHEN ISNUMERIC(JSON_VALUE([Data],'$.requireLower')) = 0 THEN JSON_MODIFY([Data],'$.requireLower',CAST(JSON_VALUE([Data],'$.requireLower') AS BIT))
        ELSE JSON_MODIFY([Data],'$.requireLower', NULL)
        END
    ELSE [Data]
    END
)
WHERE [Type] = 1 AND ISJSON([Data]) = 1
GO

UPDATE [dbo].[Policy]
SET [Data] = (SELECT
    CASE WHEN JSON_VALUE([Data],'$.requireNumbers') IS NOT NULL THEN
        CASE WHEN ISNUMERIC(JSON_VALUE([Data],'$.requireNumbers')) = 0 THEN JSON_MODIFY([Data],'$.requireNumbers',CAST(JSON_VALUE([Data],'$.requireNumbers') AS BIT))
        ELSE JSON_MODIFY([Data],'$.requireNumbers', NULL)
        END
    ELSE [Data]
    END
)
WHERE [Type] = 1 AND ISJSON([Data]) = 1
GO

UPDATE [dbo].[Policy]
SET [Data] = (SELECT
    CASE WHEN JSON_VALUE([Data],'$.requireSpecial') IS NOT NULL THEN
        CASE WHEN ISNUMERIC(JSON_VALUE([Data],'$.requireSpecial')) = 0 THEN JSON_MODIFY([Data],'$.requireSpecial',CAST(JSON_VALUE([Data],'$.requireSpecial') AS BIT))
        ELSE JSON_MODIFY([Data],'$.requireSpecial', NULL)
        END
    ELSE [Data]
    END
)
WHERE [Type] = 1 AND ISJSON([Data]) = 1
GO