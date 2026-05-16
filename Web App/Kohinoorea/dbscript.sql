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
