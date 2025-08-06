IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Permissions] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Resource] nvarchar(100) NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(50) NULL,
    [UpdatedBy] nvarchar(50) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);

CREATE TABLE [Roles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [IsDefault] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(50) NULL,
    [UpdatedBy] nvarchar(50) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [Email] nvarchar(320) NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [IsEmailConfirmed] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [FailedLoginAttempts] int NOT NULL,
    [LockedOutUntil] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(50) NULL,
    [UpdatedBy] nvarchar(50) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [RolePermissions] (
    [Id] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [PermissionId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(50) NULL,
    [UpdatedBy] nvarchar(50) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [Token] nvarchar(500) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [RevokedAt] datetime2 NULL,
    [RevokedByIp] nvarchar(45) NULL,
    [ReplacedByToken] nvarchar(500) NULL,
    [CreatedByIp] nvarchar(45) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(50) NULL,
    [UpdatedBy] nvarchar(50) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserRoles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(50) NULL,
    [UpdatedBy] nvarchar(50) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Action', N'CreatedAt', N'CreatedBy', N'Description', N'IsDeleted', N'Name', N'Resource', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Permissions]'))
    SET IDENTITY_INSERT [Permissions] ON;
INSERT INTO [Permissions] ([Id], [Action], [CreatedAt], [CreatedBy], [Description], [IsDeleted], [Name], [Resource], [UpdatedAt], [UpdatedBy])
VALUES ('33333333-3333-3333-3333-333333333333', N'read', '2025-08-06T06:22:11.6265000Z', NULL, N'Read users', CAST(0 AS bit), N'users.read', N'users', '2025-08-06T06:22:11.6265000Z', NULL),
('44444444-4444-4444-4444-444444444444', N'write', '2025-08-06T06:22:11.6265000Z', NULL, N'Write users', CAST(0 AS bit), N'users.write', N'users', '2025-08-06T06:22:11.6265000Z', NULL),
('55555555-5555-5555-5555-555555555555', N'delete', '2025-08-06T06:22:11.6265000Z', NULL, N'Delete users', CAST(0 AS bit), N'users.delete', N'users', '2025-08-06T06:22:11.6265000Z', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Action', N'CreatedAt', N'CreatedBy', N'Description', N'IsDeleted', N'Name', N'Resource', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Permissions]'))
    SET IDENTITY_INSERT [Permissions] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Description', N'IsDefault', N'IsDeleted', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [CreatedAt], [CreatedBy], [Description], [IsDefault], [IsDeleted], [Name], [UpdatedAt], [UpdatedBy])
VALUES ('11111111-1111-1111-1111-111111111111', '2025-08-06T06:22:11.6261710Z', NULL, N'Full system access', CAST(0 AS bit), CAST(0 AS bit), N'Administrator', '2025-08-06T06:22:11.6261830Z', NULL),
('22222222-2222-2222-2222-222222222222', '2025-08-06T06:22:11.6261950Z', NULL, N'Standard user access', CAST(1 AS bit), CAST(0 AS bit), N'User', '2025-08-06T06:22:11.6261950Z', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Description', N'IsDefault', N'IsDeleted', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'IsDeleted', N'PermissionId', N'RoleId', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[RolePermissions]'))
    SET IDENTITY_INSERT [RolePermissions] ON;
INSERT INTO [RolePermissions] ([Id], [CreatedAt], [CreatedBy], [IsDeleted], [PermissionId], [RoleId], [UpdatedAt], [UpdatedBy])
VALUES ('66666666-6666-6666-6666-666666666666', '2025-08-06T06:22:11.6265610Z', NULL, CAST(0 AS bit), '33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', '2025-08-06T06:22:11.6265610Z', NULL),
('77777777-7777-7777-7777-777777777777', '2025-08-06T06:22:11.6265620Z', NULL, CAST(0 AS bit), '44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', '2025-08-06T06:22:11.6265620Z', NULL),
('88888888-8888-8888-8888-888888888888', '2025-08-06T06:22:11.6265620Z', NULL, CAST(0 AS bit), '55555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111', '2025-08-06T06:22:11.6265620Z', NULL),
('99999999-9999-9999-9999-999999999999', '2025-08-06T06:22:11.6265620Z', NULL, CAST(0 AS bit), '33333333-3333-3333-3333-333333333333', '22222222-2222-2222-2222-222222222222', '2025-08-06T06:22:11.6265620Z', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'IsDeleted', N'PermissionId', N'RoleId', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[RolePermissions]'))
    SET IDENTITY_INSERT [RolePermissions] OFF;

CREATE UNIQUE INDEX [IX_Permissions_Name] ON [Permissions] ([Name]);

CREATE UNIQUE INDEX [IX_Permissions_Resource_Action] ON [Permissions] ([Resource], [Action]);

CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [RefreshTokens] ([ExpiresAt]);

CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);

CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);

CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [RolePermissions] ([RoleId], [PermissionId]);

CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [UserRoles] ([UserId], [RoleId]);

CREATE INDEX [IX_Users_CreatedAt] ON [Users] ([CreatedAt]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250806062211_InitialCreate', N'9.0.8');

COMMIT;
GO

