IF OBJECT_ID(N'dbo.faqs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.faqs
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        question NVARCHAR(250) NOT NULL,
        answer NVARCHAR(4000) NOT NULL,
        display_order INT NOT NULL CONSTRAINT DF_faqs_display_order DEFAULT (0),
        is_active BIT NOT NULL CONSTRAINT DF_faqs_is_active DEFAULT (1),
        created_at_utc DATETIME2(7) NOT NULL CONSTRAINT DF_faqs_created_at_utc DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF COL_LENGTH(N'dbo.faqs', N'display_order') IS NULL
BEGIN
    ALTER TABLE dbo.faqs ADD display_order INT NULL;
    UPDATE dbo.faqs SET display_order = 0 WHERE display_order IS NULL;
    ALTER TABLE dbo.faqs ALTER COLUMN display_order INT NOT NULL;
END;
GO

IF OBJECT_ID(N'DF_faqs_display_order', N'D') IS NULL
BEGIN
    ALTER TABLE dbo.faqs ADD CONSTRAINT DF_faqs_display_order DEFAULT (0) FOR display_order;
END;
GO

IF COL_LENGTH(N'dbo.faqs', N'is_active') IS NULL
BEGIN
    ALTER TABLE dbo.faqs ADD is_active BIT NULL;
    UPDATE dbo.faqs SET is_active = 1 WHERE is_active IS NULL;
    ALTER TABLE dbo.faqs ALTER COLUMN is_active BIT NOT NULL;
END;
GO

IF OBJECT_ID(N'DF_faqs_is_active', N'D') IS NULL
BEGIN
    ALTER TABLE dbo.faqs ADD CONSTRAINT DF_faqs_is_active DEFAULT (1) FOR is_active;
END;
GO

IF COL_LENGTH(N'dbo.faqs', N'created_at_utc') IS NULL
BEGIN
    ALTER TABLE dbo.faqs ADD created_at_utc DATETIME2(7) NULL;
    UPDATE dbo.faqs SET created_at_utc = SYSUTCDATETIME() WHERE created_at_utc IS NULL;
    ALTER TABLE dbo.faqs ALTER COLUMN created_at_utc DATETIME2(7) NOT NULL;
END;
GO

IF OBJECT_ID(N'DF_faqs_created_at_utc', N'D') IS NULL
BEGIN
    ALTER TABLE dbo.faqs ADD CONSTRAINT DF_faqs_created_at_utc DEFAULT (SYSUTCDATETIME()) FOR created_at_utc;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.faqs')
      AND name = N'IX_faqs_is_active_display_order_created'
)
BEGIN
    CREATE INDEX IX_faqs_is_active_display_order_created
        ON dbo.faqs (is_active, display_order, created_at_utc DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.faqs)
BEGIN
    INSERT INTO dbo.faqs (question, answer, display_order, is_active, created_at_utc)
    VALUES
        (N'What is Kohinoor EA?', N'Kohinoor EA is a premium automated trading system developed for MetaTrader 4. It is designed to analyze market conditions, execute trades based on predefined logic, and manage positions with a disciplined algorithmic approach.', 1, 1, SYSUTCDATETIME()),
        (N'Does it work on MT4?', N'Yes. Kohinoor EA is built specifically for the MetaTrader 4 platform and integrates smoothly with MT4-compatible broker accounts.', 2, 1, SYSUTCDATETIME()),
        (N'Which pair does it support?', N'Kohinoor EA is optimized for XAUUSD (Gold), one of the most actively traded instruments in the market. It is designed to work with the dynamic nature of gold price movements.', 3, 1, SYSUTCDATETIME()),
        (N'Is it fully automated?', N'Yes. Once installed and configured, Kohinoor EA can monitor the market, place trades, and manage open positions automatically according to its programmed strategy rules.', 4, 1, SYSUTCDATETIME()),
        (N'Do I need a VPS?', N'A VPS is not always required, but it is highly recommended for stable 24/5 operation, lower latency, and uninterrupted trading when your personal computer is turned off.', 5, 1, SYSUTCDATETIME()),
        (N'Can I change risk settings?', N'Yes. Kohinoor EA includes adjustable settings that allow users to customize risk levels and trading preferences based on their own goals and account size.', 6, 1, SYSUTCDATETIME()),
        (N'What pricing plans are available?', N'Kohinoor EA is available in flexible plans to suit different needs: Monthly, Annual, and Lifetime - 4 Years.', 7, 1, SYSUTCDATETIME()),
        (N'Do you provide support after purchase?', N'Yes. Support is available after purchase for installation guidance, setup assistance, and general product-related questions to help users get started smoothly.', 8, 1, SYSUTCDATETIME()),
        (N'Does it work on all brokers?', N'Kohinoor EA is designed for MT4-compatible broker accounts. Real-world execution can vary based on broker conditions, spreads, execution quality, and account type, so testing on your preferred broker is recommended.', 9, 1, SYSUTCDATETIME()),
        (N'Minimum deposit?', N'The minimum deposit can depend on the broker, the account type, and the user-selected risk settings. You should define a recommended minimum based on your tested operating range before launch.', 10, 1, SYSUTCDATETIME()),
        (N'Refund policy?', N'We aim to provide a high-quality product and professional support. Due to the digital nature of Kohinoor EA and instant file delivery, refunds are generally not available once the product has been delivered. However, if you experience a verified technical issue that prevents activation or normal use, please contact our support team. If the issue cannot be resolved within a reasonable time, a refund or replacement may be considered at our discretion.', 11, 1, SYSUTCDATETIME()),
        (N'Chargeback requests', N'Please contact support before requesting a chargeback so we can assist you promptly.', 12, 1, SYSUTCDATETIME());
END;
GO
