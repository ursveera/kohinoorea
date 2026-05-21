SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.users
	(
		id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
		full_name NVARCHAR(150) NOT NULL,
		email NVARCHAR(320) NOT NULL,
		phone NVARCHAR(50) NULL,
		mt4_broker NVARCHAR(200) NULL,
		password_hash NVARCHAR(500) NOT NULL,
		[role] NVARCHAR(30) NOT NULL CONSTRAINT DF_users_role DEFAULT ('User'),
		is_active BIT NOT NULL CONSTRAINT DF_users_is_active DEFAULT (1),
		created_at_utc DATETIME2(0) NOT NULL CONSTRAINT DF_users_created_at_utc DEFAULT (SYSUTCDATETIME()),
		last_login_at_utc DATETIME2(0) NULL
	);
END
GO

IF COL_LENGTH('dbo.users', 'role') IS NULL
BEGIN
	ALTER TABLE dbo.users
		ADD [role] NVARCHAR(30) NOT NULL CONSTRAINT DF_users_role DEFAULT ('User');
END
GO

IF COL_LENGTH('dbo.users', 'phone') IS NULL
BEGIN
	ALTER TABLE dbo.users
		ADD phone NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.users', 'mt4_broker') IS NULL
BEGIN
	ALTER TABLE dbo.users
		ADD mt4_broker NVARCHAR(200) NULL;
END
GO

IF COL_LENGTH('dbo.users', 'is_active') IS NULL
BEGIN
	ALTER TABLE dbo.users
		ADD is_active BIT NOT NULL CONSTRAINT DF_users_is_active DEFAULT (1);
END
GO

IF COL_LENGTH('dbo.users', 'created_at_utc') IS NULL
BEGIN
	ALTER TABLE dbo.users
		ADD created_at_utc DATETIME2(0) NOT NULL CONSTRAINT DF_users_created_at_utc DEFAULT (SYSUTCDATETIME());
END
GO

IF COL_LENGTH('dbo.users', 'last_login_at_utc') IS NULL
BEGIN
	ALTER TABLE dbo.users
		ADD last_login_at_utc DATETIME2(0) NULL;
END
GO

UPDATE dbo.users
SET [role] = 'User'
WHERE [role] IS NULL OR LTRIM(RTRIM([role])) = '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_users_email' AND object_id = OBJECT_ID('dbo.users'))
BEGIN
	CREATE UNIQUE INDEX UX_users_email ON dbo.users(email);
END
GO

IF OBJECT_ID(N'dbo.signup_submissions', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.signup_submissions
	(
		id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
		full_name NVARCHAR(150) NOT NULL,
		email NVARCHAR(320) NOT NULL,
		phone NVARCHAR(50) NULL,
		mt4_broker NVARCHAR(200) NULL,
		access_plan NVARCHAR(50) NOT NULL,
		notes NVARCHAR(MAX) NULL,
		created_at_utc DATETIME2(0) NOT NULL CONSTRAINT DF_signup_submissions_created_at_utc DEFAULT (SYSUTCDATETIME())
	);
END
GO

IF OBJECT_ID(N'dbo.products', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.products
	(
		id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
		[name] NVARCHAR(150) NOT NULL,
		[description] NVARCHAR(MAX) NULL,
		image_link NVARCHAR(2000) NULL,
		price DECIMAL(18,2) NOT NULL,
		is_active BIT NOT NULL CONSTRAINT DF_products_is_active DEFAULT (1),
		created_at_utc DATETIME2(0) NOT NULL CONSTRAINT DF_products_created_at_utc DEFAULT (SYSUTCDATETIME())
	);
END
GO

IF COL_LENGTH('dbo.products', 'image_link') IS NULL
BEGIN
	ALTER TABLE dbo.products
		ADD image_link NVARCHAR(2000) NULL;
END
GO

IF COL_LENGTH('dbo.products', 'is_master') IS NULL
BEGIN
	ALTER TABLE dbo.products
		ADD is_master BIT NOT NULL CONSTRAINT DF_products_is_master DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.products', 'country_code') IS NULL
BEGIN
	ALTER TABLE dbo.products
		ADD country_code NVARCHAR(5) NULL;
END
GO

IF OBJECT_ID(N'dbo.orders', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.orders
	(
		id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
		product_id BIGINT NOT NULL,
		user_id BIGINT NOT NULL,
		quantity INT NOT NULL,
		unit_price DECIMAL(18,2) NOT NULL,
		total_amount DECIMAL(18,2) NOT NULL,
		payment_method NVARCHAR(50) NOT NULL CONSTRAINT DF_orders_payment_method DEFAULT ('Card'),
		status NVARCHAR(30) NOT NULL CONSTRAINT DF_orders_status DEFAULT ('Pending'),
		ordered_at_utc DATETIME2(0) NOT NULL CONSTRAINT DF_orders_ordered_at_utc DEFAULT (SYSUTCDATETIME()),
		CONSTRAINT FK_orders_products FOREIGN KEY (product_id) REFERENCES dbo.products(id),
		CONSTRAINT FK_orders_users FOREIGN KEY (user_id) REFERENCES dbo.users(id)
	);
END
GO

IF COL_LENGTH('dbo.orders', 'payment_method') IS NULL
BEGIN
	ALTER TABLE dbo.orders
		ADD payment_method NVARCHAR(50) NOT NULL CONSTRAINT DF_orders_payment_method DEFAULT ('Card');
END
GO

IF COL_LENGTH('dbo.orders', 'status') IS NULL
BEGIN
	ALTER TABLE dbo.orders
		ADD status NVARCHAR(30) NOT NULL CONSTRAINT DF_orders_status DEFAULT ('Pending');
END
GO

IF OBJECT_ID(N'dbo.cart_items', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.cart_items
	(
		id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
		user_id BIGINT NOT NULL,
		product_id BIGINT NOT NULL,
		quantity INT NOT NULL,
		unit_price DECIMAL(18,2) NOT NULL,
		total_amount DECIMAL(18,2) NOT NULL,
		added_at_utc DATETIME2(0) NOT NULL CONSTRAINT DF_cart_items_added_at_utc DEFAULT (SYSUTCDATETIME()),
		CONSTRAINT FK_cart_items_users FOREIGN KEY (user_id) REFERENCES dbo.users(id),
		CONSTRAINT FK_cart_items_products FOREIGN KEY (product_id) REFERENCES dbo.products(id)
	);
END
GO

IF OBJECT_ID(N'dbo.support_queries', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.support_queries
	(
		id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
		user_id BIGINT NOT NULL,
		subject NVARCHAR(150) NOT NULL,
		category NVARCHAR(50) NOT NULL,
		message NVARCHAR(MAX) NOT NULL,
		status NVARCHAR(30) NOT NULL CONSTRAINT DF_support_queries_status DEFAULT ('Open'),
		created_at_utc DATETIME2(0) NOT NULL CONSTRAINT DF_support_queries_created_at_utc DEFAULT (SYSUTCDATETIME()),
		CONSTRAINT FK_support_queries_users FOREIGN KEY (user_id) REFERENCES dbo.users(id)
	);
END
GO

IF OBJECT_ID(N'dbo.support_query_messages', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.support_query_messages
	(
		id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
		query_id BIGINT NOT NULL,
		sender_role NVARCHAR(30) NOT NULL,
		sender_user_id BIGINT NULL,
		message NVARCHAR(MAX) NOT NULL,
		created_at_utc DATETIME2(0) NOT NULL CONSTRAINT DF_support_query_messages_created_at_utc DEFAULT (SYSUTCDATETIME()),
		CONSTRAINT FK_support_query_messages_queries FOREIGN KEY (query_id) REFERENCES dbo.support_queries(id),
		CONSTRAINT FK_support_query_messages_users FOREIGN KEY (sender_user_id) REFERENCES dbo.users(id)
	);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_orders_user_id' AND object_id = OBJECT_ID('dbo.orders'))
BEGIN
	CREATE INDEX IX_orders_user_id ON dbo.orders(user_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_orders_product_id' AND object_id = OBJECT_ID('dbo.orders'))
BEGIN
	CREATE INDEX IX_orders_product_id ON dbo.orders(product_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_cart_items_user_id' AND object_id = OBJECT_ID('dbo.cart_items'))
BEGIN
	CREATE INDEX IX_cart_items_user_id ON dbo.cart_items(user_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_cart_items_product_id' AND object_id = OBJECT_ID('dbo.cart_items'))
BEGIN
	CREATE INDEX IX_cart_items_product_id ON dbo.cart_items(product_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_support_queries_user_id' AND object_id = OBJECT_ID('dbo.support_queries'))
BEGIN
	CREATE INDEX IX_support_queries_user_id ON dbo.support_queries(user_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_support_query_messages_query_id' AND object_id = OBJECT_ID('dbo.support_query_messages'))
BEGIN
	CREATE INDEX IX_support_query_messages_query_id ON dbo.support_query_messages(query_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_support_query_messages_sender_user_id' AND object_id = OBJECT_ID('dbo.support_query_messages'))
BEGIN
	CREATE INDEX IX_support_query_messages_sender_user_id ON dbo.support_query_messages(sender_user_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.products)
BEGIN
	INSERT INTO dbo.products([name], [description], image_link, price, is_active, created_at_utc)
	VALUES
	(N'Monthly Plan', N'Full Kohinoor EA access with standard onboarding support.', N'https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=800&q=80', 99.00, 1, SYSUTCDATETIME()),
	(N'Annual Plan', N'Best-value access with priority onboarding and launch promo eligibility.', N'https://images.unsplash.com/photo-1551281044-8b66f0f6c1b0?auto=format&fit=crop&w=800&q=80', 299.00, 1, SYSUTCDATETIME()),
	(N'Lifetime Plan (4 Years)', N'Long-term continuity plan with priority support.', N'https://images.unsplash.com/photo-1551281044-8b66f0f6c1b0?auto=format&fit=crop&w=800&q=80', 799.00, 1, SYSUTCDATETIME());
END
GO

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

IF OBJECT_ID(N'dbo.contact_messages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.contact_messages
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        name NVARCHAR(120) NOT NULL,
        email NVARCHAR(255) NOT NULL,
        subject NVARCHAR(200) NOT NULL,
        message NVARCHAR(4000) NOT NULL,
        is_replied BIT NOT NULL CONSTRAINT DF_contact_messages_is_replied DEFAULT (0),
        created_at_utc DATETIME2(7) NOT NULL CONSTRAINT DF_contact_messages_created_at_utc DEFAULT (SYSUTCDATETIME()),
        last_replied_at_utc DATETIME2(7) NULL
    );
END;
GO

IF COL_LENGTH(N'dbo.contact_messages', N'name') IS NULL
BEGIN
    ALTER TABLE dbo.contact_messages ADD name NVARCHAR(120) NULL;
    UPDATE dbo.contact_messages SET name = N'Unknown Contact' WHERE name IS NULL;
    ALTER TABLE dbo.contact_messages ALTER COLUMN name NVARCHAR(120) NOT NULL;
END;
GO

IF COL_LENGTH(N'dbo.contact_messages', N'email') IS NULL
BEGIN
    ALTER TABLE dbo.contact_messages ADD email NVARCHAR(255) NULL;
    UPDATE dbo.contact_messages SET email = N'' WHERE email IS NULL;
    ALTER TABLE dbo.contact_messages ALTER COLUMN email NVARCHAR(255) NOT NULL;
END;
GO

IF COL_LENGTH(N'dbo.contact_messages', N'subject') IS NULL
BEGIN
    ALTER TABLE dbo.contact_messages ADD subject NVARCHAR(200) NULL;
    UPDATE dbo.contact_messages SET subject = N'Contact Request' WHERE subject IS NULL;
    ALTER TABLE dbo.contact_messages ALTER COLUMN subject NVARCHAR(200) NOT NULL;
END;
GO

IF COL_LENGTH(N'dbo.contact_messages', N'message') IS NULL
BEGIN
    ALTER TABLE dbo.contact_messages ADD message NVARCHAR(4000) NULL;
    UPDATE dbo.contact_messages SET message = N'' WHERE message IS NULL;
    ALTER TABLE dbo.contact_messages ALTER COLUMN message NVARCHAR(4000) NOT NULL;
END;
GO

IF COL_LENGTH(N'dbo.contact_messages', N'is_replied') IS NULL
BEGIN
    ALTER TABLE dbo.contact_messages ADD is_replied BIT NULL;
    UPDATE dbo.contact_messages SET is_replied = 0 WHERE is_replied IS NULL;
    ALTER TABLE dbo.contact_messages ALTER COLUMN is_replied BIT NOT NULL;
END;
GO

IF OBJECT_ID(N'DF_contact_messages_is_replied', N'D') IS NULL
BEGIN
    ALTER TABLE dbo.contact_messages ADD CONSTRAINT DF_contact_messages_is_replied DEFAULT (0) FOR is_replied;
END;
GO

IF COL_LENGTH(N'dbo.contact_messages', N'created_at_utc') IS NULL
BEGIN
    ALTER TABLE dbo.contact_messages ADD created_at_utc DATETIME2(7) NULL;
    UPDATE dbo.contact_messages SET created_at_utc = SYSUTCDATETIME() WHERE created_at_utc IS NULL;
    ALTER TABLE dbo.contact_messages ALTER COLUMN created_at_utc DATETIME2(7) NOT NULL;
END;
GO

IF OBJECT_ID(N'DF_contact_messages_created_at_utc', N'D') IS NULL
BEGIN
    ALTER TABLE dbo.contact_messages ADD CONSTRAINT DF_contact_messages_created_at_utc DEFAULT (SYSUTCDATETIME()) FOR created_at_utc;
END;
GO

IF COL_LENGTH(N'dbo.contact_messages', N'last_replied_at_utc') IS NULL
BEGIN
    ALTER TABLE dbo.contact_messages ADD last_replied_at_utc DATETIME2(7) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.contact_messages')
      AND name = N'IX_contact_messages_reply_created'
)
BEGIN
    CREATE INDEX IX_contact_messages_reply_created
        ON dbo.contact_messages (is_replied, created_at_utc DESC);
END;
GO
