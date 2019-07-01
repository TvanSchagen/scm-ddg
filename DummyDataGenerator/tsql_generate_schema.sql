IF NOT EXISTS 
   (
     SELECT name FROM master.dbo.sysdatabases 
     WHERE name = N'scm_b2_d2'
    )
CREATE DATABASE [scm_b2_d2]
GO

USE [scm_b2_d2]
/****** Object:  Table [dbo].[activity]    Script Date: 27-6-2019 13:58:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[activity](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[GUID] [uniqueidentifier] NULL,
	[name] [varchar](256) NULL,
	[description] [varchar](max) NULL,
	[created] [datetime] NULL,
	[last_updated] [datetime] NULL,
 CONSTRAINT [PK_activity] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[consists_of]    Script Date: 27-6-2019 13:58:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[consists_of](
	[id] [int] NOT NULL,
	[parent_product_id] [int] NULL,
	[child_product_id] [int] NULL,
 CONSTRAINT [PK_consists_of] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[location]    Script Date: 27-6-2019 13:58:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[location](
	[id] [int] NOT NULL,
	[GUID] [uniqueidentifier] NULL,
	[longtitude] [float] NULL,
	[latitude] [float] NULL,
	[country] [varchar](64) NULL,
	[postal_code] [varchar](64) NULL,
	[province] [varchar](64) NULL,
	[city] [varchar](64) NULL,
	[street] [varchar](128) NULL,
	[created] [datetime] NULL,
	[last_updated] [datetime] NULL,
 CONSTRAINT [PK_location] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[organization]    Script Date: 27-6-2019 13:58:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[organization](
	[id] [int] NOT NULL,
	[GUID] [uniqueidentifier] NULL,
	[name] [varchar](256) NULL,
	[description] [varchar](max) NULL,
	[ein] [char](16) NULL,
	[number_of_employees] [int] NULL,
	[email_address] [varchar](256) NULL,
	[website] [varchar](256) NULL,
	[created] [datetime] NULL,
	[last_updated] [datetime] NULL,
 CONSTRAINT [PK_organization] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[product]    Script Date: 27-6-2019 13:58:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[product](
	[id] [int] NOT NULL,
	[GUID] [uniqueidentifier] NULL,
	[name] [varchar](256) NULL,
	[description] [varchar](max) NULL,
	[ean] [char](13) NULL,
	[category] [varchar](128) NULL,
	[sub_category] [varchar](128) NULL,
	[created] [datetime] NULL,
	[last_updated] [datetime] NULL,
 CONSTRAINT [PK_product] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[supplies]    Script Date: 27-6-2019 13:58:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[supplies](
	[id] [int] NOT NULL,
	[organization_id] [int] NULL,
	[activity_id] [int] NULL,
	[product_id] [int] NULL,
	[location_id] [int] NULL,
 CONSTRAINT [PK_supplies] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[consists_of]  WITH CHECK ADD  CONSTRAINT [FK_consists_of_child_product_id] FOREIGN KEY([child_product_id])
REFERENCES [dbo].[product] ([id])
GO
ALTER TABLE [dbo].[consists_of] CHECK CONSTRAINT [FK_consists_of_child_product_id]
GO
ALTER TABLE [dbo].[consists_of]  WITH CHECK ADD  CONSTRAINT [FK_consists_of_parent_product_id] FOREIGN KEY([parent_product_id])
REFERENCES [dbo].[product] ([id])
GO
ALTER TABLE [dbo].[consists_of] CHECK CONSTRAINT [FK_consists_of_parent_product_id]
GO
ALTER TABLE [dbo].[supplies]  WITH CHECK ADD  CONSTRAINT [FK_location_organization_id] FOREIGN KEY([location_id])
REFERENCES [dbo].[location] ([id])
GO
ALTER TABLE [dbo].[supplies] CHECK CONSTRAINT [FK_location_organization_id]
GO
ALTER TABLE [dbo].[supplies]  WITH CHECK ADD  CONSTRAINT [FK_product_organization_id] FOREIGN KEY([product_id])
REFERENCES [dbo].[product] ([id])
GO
ALTER TABLE [dbo].[supplies] CHECK CONSTRAINT [FK_product_organization_id]
GO
ALTER TABLE [dbo].[supplies]  WITH CHECK ADD  CONSTRAINT [FK_supplies_activity_id] FOREIGN KEY([activity_id])
REFERENCES [dbo].[activity] ([id])
GO
ALTER TABLE [dbo].[supplies] CHECK CONSTRAINT [FK_supplies_activity_id]
GO
ALTER TABLE [dbo].[supplies]  WITH CHECK ADD  CONSTRAINT [FK_supplies_organization_id] FOREIGN KEY([organization_id])
REFERENCES [dbo].[organization] ([id])
GO
ALTER TABLE [dbo].[supplies] CHECK CONSTRAINT [FK_supplies_organization_id]
GO
