because we have problem currently to connect to ProgressPlayDB, lets change the connection to ProgressPlayDBTest for now. I added these 2 tables to ProgressPlayDBTest, so you can use it. 
USE [ProgressPlayDBTest]
GO

/****** Object:  Table [stats].[tbl_Monitor_Statistics]    Script Date: 6/12/2025 11:16:06 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [stats].[tbl_Monitor_Statistics](
	[Day] [date] NOT NULL,
	[Hour] [tinyint] NOT NULL,
	[CollectorID] [bigint] NOT NULL,
	[ItemName] [varchar](50) NULL,
	[Total] [decimal](18, 2) NULL,
	[Marked] [decimal](18, 2) NULL,
	[MarkedPercent] [decimal](18, 2) NULL,
	[UpdatedDate] [datetime] NULL
) ON [PRIMARY]
GO


USE [ProgressPlayDBTest]
GO

/****** Object:  Table [stats].[tbl_Monitor_StatisticsCollectors]    Script Date: 6/12/2025 11:16:16 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [stats].[tbl_Monitor_StatisticsCollectors](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[CollectorID] [bigint] NOT NULL,
	[CollectorCode] [varchar](500) NULL,
	[CollectorDesc] [nvarchar](500) NULL,
	[FrequencyMinutes] [int] NOT NULL,
	[LastMinutes] [int] NULL,
	[StoreProcedure] [varchar](50) NULL,
	[IsActive] [bit] NULL,
	[UpdatedDate] [datetime] NULL,
	[LastRun] [datetime] NULL,
	[LastRunResult] [nvarchar](500) NULL
) ON [PRIMARY]
GO


create models and all and lets integrate it, first in creating indicators
