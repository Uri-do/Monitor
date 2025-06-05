Request Title: Monitoring Grid

Background / Context: The goal of this project is to create a monitoring grid based on our KPI’s, and relying on our Realtime DB. Querying the DB for the KPI’s checking count and volume of defined KPI’s in order to know if there is an issue, and sending alerts (email/sms) to tech and the relevant stakeholders.

Take the requirements below as guidelines, but make the solution more comprehensive, robust and full of toggeble features. Use C\# .net 8\. This is the mssql DB connection string: 

data source=192.168.166.11,1433;initial catalog=ProgressPlayDBTest;user id=saturn;password=XXXXXXXX;asynchronous processing=true;

Detailed Requirements:

1\. Infrastructure:

Create tables in the DB that will contain all Grid components, each with its frequency and Deviation.

Create a Service that will constantly go over the table and run the relevant queries on DB2 (to avoid load on DB1).

Create a designated SP for each row that will check if to trigger alerts.

SMS will be sent via Email to SMS services.

2\. KPI’s Table:

KPI table will include the following fields:

Indicator The name of the indicator

Owner Who is in charge of this KPI

Priority 1 – SMS, 2 \- Email

Frequency KPI frequency, how much time for result

Deviation How much do we accept deviation from the 4 weeks comparison

SP name Stored Procedure to run

These are few store procedures all will be with the same structure: 

USE \[ProgressPlayDB\]

GO

/\*\*\*\*\*\* Object:  StoredProcedure \[stats\].\[stp\_MonitorTransactions\]    Script Date: 6/5/2025 12:55:22 PM \*\*\*\*\*\*/

SET ANSI\_NULLS ON

GO

SET QUOTED\_IDENTIFIER ON

GO

ALTER procedure \[stats\].\[stp\_MonitorTransactions\]

(

	@ForLastMinutes int

)

AS

BEGIN

	SELECT	ltt.lut\_name AS ItemName,

			SUM(1) AS Total,

			SUM(CASE WHEN at.is\_done \= 1 THEN 1 ELSE 0 END) AS Successful

	FROM accounts.tbl\_Account\_transactions at (NOLOCK) 

	INNER JOIN common.tbl\_Luts ltt (NOLOCK) ON ltt.lut\_id \= at.transaction\_type\_id

	LEFT JOIN accounts.tbl\_Account\_payment\_methods apm (NOLOCK) ON apm.account\_payment\_method\_id \= at.account\_payment\_method\_id

	LEFT JOIN accounts.tbl\_Settlement\_companies sc (NOLOCK) ON sc.settlement\_company\_id \= apm.settlement\_company\_id

	WHERE at.updated\_dt \>= DATEADD(MINUTE, \-@ForLastMinutes, GETUTCDATE())

	GROUP BY ltt.lut\_name

	ORDER BY SUM(1) DESC	

END

…	SELECT	sc.name AS ItemName,

			SUM(1) AS Total,

			SUM(CASE WHEN at.is\_done \= 1 THEN 1 ELSE 0 END) AS Successful

	FROM accounts.tbl\_Account\_transactions at (NOLOCK) 

	INNER JOIN accounts.tbl\_Account\_payment\_methods apm (NOLOCK) ON apm.account\_payment\_method\_id \= at.account\_payment\_method\_id

	INNER JOIN accounts.tbl\_Settlement\_companies sc (NOLOCK) ON sc.settlement\_company\_id \= apm.settlement\_company\_id

	WHERE at.updated\_dt \>= DATEADD(MINUTE, \-@ForLastMinutes, GETUTCDATE())

	AND at.transaction\_type\_id \= 263

	GROUP BY sc.name

	ORDER BY SUM(1) DESC	

	SELECT	c.country\_name AS Country,

			SUM(CASE WHEN 1 \= 1 THEN 1 ELSE 0 END) AS AllDeposits,

			SUM(CASE WHEN at.is\_done \= 1 THEN 1 ELSE 0 END) AS Deposits

	FROM accounts.tbl\_Account\_transactions at (NOLOCK) 

	INNER JOIN accounts.tbl\_Account\_payment\_methods apm (NOLOCK) ON apm.account\_payment\_method\_id \= at.account\_payment\_method\_id

	INNER JOIN accounts.tbl\_Settlement\_companies sc (NOLOCK) ON sc.settlement\_company\_id \= apm.settlement\_company\_id

	INNER JOIN common.tbl\_Players p (NOLOCK) ON p.player\_id \= at.player\_id

	INNER JOIN common.tbl\_Countries c (NOLOCK) ON c.country\_id \= p.country\_id

	INNER JOIN common.tbl\_White\_labels wl (NOLOCK) ON wl.label\_id \= p.white\_label\_id

	WHERE at.updated\_dt \>= DATEADD(MINUTE, \-@ForLastMinutes, GETUTCDATE())

	AND at.transaction\_type\_id \= 263

	GROUP BY c.country\_name	

	SELECT	wl.label\_name AS Label,

			SUM(1) AS AllDeposits,

			SUM(CASE WHEN at.is\_done \= 1 THEN 1 ELSE 0 END) AS Deposits

	FROM accounts.tbl\_Account\_transactions at (NOLOCK) 

	INNER JOIN accounts.tbl\_Account\_payment\_methods apm (NOLOCK) ON apm.account\_payment\_method\_id \= at.account\_payment\_method\_id

	INNER JOIN accounts.tbl\_Settlement\_companies sc (NOLOCK) ON sc.settlement\_company\_id \= apm.settlement\_company\_id

	INNER JOIN common.tbl\_Players p (NOLOCK) ON p.player\_id \= at.player\_id

	INNER JOIN common.tbl\_Countries c (NOLOCK) ON c.country\_id \= p.country\_id

	INNER JOIN common.tbl\_White\_labels wl (NOLOCK) ON wl.label\_id \= p.white\_label\_id

	WHERE at.updated\_dt \>= DATEADD(MINUTE, \-@ForLastMinutes, GETUTCDATE())

	AND at.transaction\_type\_id \= 263

	GROUP BY wl.label\_name

	ORDER BY SUM(1) DESC

Subject The subject that will be sent in SMS/Email

Description The description that will be sent in email

Who to inform Who to alert when triggered (a separate table will contain contact emails and phones)

Last run Last updated run of the SP

An example of a record for reference:

Indicator Deposits

Owner Amnon

Priority SMS

Frequency 5

Deviation 10

SP name Sp\_monitorGrid\_deposits\_general

Subject no deposits in last \[frequency\]

Description check if there are \[threshold\] deposits in the last \[frequency\]

Who to inform Itai, Mike, Gavriel

Last Run Updated date of sp last run

The full table can be found here: Monitoring Requitements

3\. Service:

A service that will run constantly, and every minute will go over the KPI’s table and will run the SP’s based on the fields \[frequency\] and \[last run\].

There will be an option to compare results for average (in this day of week, in this timeframe etc) and alert If the result is different in more than \[deviation\]% ot more in absolute terms etc then an alert is triggered.

Once triggered, based on priority an alert will be sent:

· SMS – an email will be sent to an email to sms gateway to the \[who to inform\] contacts.

A regular email will be also sent.

· Email – an email with the subject and description will be sent to the \[who to inform\] contacts.

Alerts Log:

Every time an alert is triggered, it will be added to a log table, with all the information that we have on that alert.

Here's a comprehensive, robust solution for the Monitoring Grid system with toggleable features, designed for .NET 8 and SQL Server:

\#\#\# 1\. Enhanced Database Schema  
\`\`\`sql  
CREATE SCHEMA monitoring;  
GO

\-- KPI Configuration Table  
CREATE TABLE monitoring.KPIs (  
    KpiId INT IDENTITY PRIMARY KEY,  
    Indicator NVARCHAR(255) NOT NULL,  
    Owner NVARCHAR(100) NOT NULL,  
    Priority TINYINT NOT NULL CHECK (Priority IN (1, 2)), \-- 1=SMS, 2=Email  
    Frequency INT NOT NULL, \-- Minutes  
    Deviation DECIMAL(5,2) NOT NULL, \-- Percentage  
    SpName NVARCHAR(255) NOT NULL,  
    SubjectTemplate NVARCHAR(500) NOT NULL,  
    DescriptionTemplate NVARCHAR(MAX) NOT NULL,  
    IsActive BIT NOT NULL DEFAULT 1,  
    LastRun DATETIME2 NULL,  
    CooldownMinutes INT NOT NULL DEFAULT 30, \-- Prevent alert flooding  
    MinimumThreshold DECIMAL(18,2) NULL \-- Absolute value threshold  
);

\-- Contact Management  
CREATE TABLE monitoring.Contacts (  
    ContactId INT IDENTITY PRIMARY KEY,  
    Name NVARCHAR(255) NOT NULL,  
    Email NVARCHAR(255) NULL,  
    Phone NVARCHAR(50) NULL,  
    IsActive BIT NOT NULL DEFAULT 1  
);

\-- KPI-Contact Mapping  
CREATE TABLE monitoring.KpiContacts (  
    KpiId INT NOT NULL,  
    ContactId INT NOT NULL,  
    PRIMARY KEY (KpiId, ContactId),  
    FOREIGN KEY (KpiId) REFERENCES monitoring.KPIs(KpiId),  
    FOREIGN KEY (ContactId) REFERENCES monitoring.Contacts(ContactId)  
);

\-- Alert History  
CREATE TABLE monitoring.AlertLogs (  
    AlertId BIGINT IDENTITY PRIMARY KEY,  
    KpiId INT NOT NULL,  
    TriggerTime DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),  
    Message NVARCHAR(500) NOT NULL,  
    Details NVARCHAR(MAX) NULL,  
    SentVia TINYINT NOT NULL, \-- 1=SMS, 2=Email  
    SentTo NVARCHAR(MAX) NOT NULL,  
    IsResolved BIT NOT NULL DEFAULT 0,  
    ResolvedTime DATETIME2 NULL,  
    FOREIGN KEY (KpiId) REFERENCES monitoring.KPIs(KpiId)  
);

\-- Configuration Table  
CREATE TABLE monitoring.Config (  
    ConfigKey NVARCHAR(50) PRIMARY KEY,  
    ConfigValue NVARCHAR(255) NOT NULL  
);

INSERT INTO monitoring.Config VALUES  
('SmsGateway', 'gateway@example.com'),  
('AdminEmail', 'admin@example.com'),  
('MaxParallelExecutions', '5'),  
('AlertRetryCount', '3');  
\`\`\`

\#\#\# 2\. Stored Procedure Template  
\`\`\`sql  
CREATE PROCEDURE monitoring.usp\_CalculateKpi  
    @ForLastMinutes INT,  
    @Key NVARCHAR(255) OUTPUT,  
    @CurrentValue DECIMAL(18,2) OUTPUT,  
    @HistoricalValue DECIMAL(18,2) OUTPUT  
AS  
BEGIN  
    /\*   
    Template for KPI procedures:  
    1\. Calculate current value for @ForLastMinutes  
    2\. Calculate historical value (same period 4 weeks back)  
    3\. Return single metric set  
    \*/  
      
    \-- Example implementation:  
    SELECT TOP 1   
        @Key \= 'DefaultMetric',  
        @CurrentValue \= COUNT(\*)  
    FROM accounts.tbl\_Account\_transactions  
    WHERE updated\_dt \>= DATEADD(MINUTE, \-@ForLastMinutes, GETUTCDATE());  
      
    SELECT @HistoricalValue \= AVG(Value)  
    FROM monitoring.HistoricalData  
    WHERE Metric \= 'DefaultMetric'  
        AND Period \= @ForLastMinutes  
        AND Timestamp BETWEEN DATEADD(WEEK, \-4, GETUTCDATE())   
            AND DATEADD(WEEK, \-4, DATEADD(MINUTE, @ForLastMinutes, GETUTCDATE()));  
END  
\`\`\`

\#\#\# 3\. .NET 8 Worker Service Implementation

\*\*Program.cs:\*\*  
\`\`\`csharp  
using Microsoft.EntityFrameworkCore;  
using Microsoft.Data.SqlClient;  
using System.Net.Mail;

var builder \= Host.CreateApplicationBuilder(args);  
builder.Services.AddHostedService\<MonitoringWorker\>();  
builder.Services.AddDbContext\<MonitoringContext\>(options \=\>   
    options.UseSqlServer(builder.Configuration.GetConnectionString("MonitoringGrid")));  
builder.Services.AddSingleton\<EmailService\>();  
builder.Services.AddSingleton\<SmsService\>();  
builder.Services.Configure\<MonitoringConfig\>(builder.Configuration.GetSection("Monitoring"));

var host \= builder.Build();  
host.Run();  
\`\`\`

\*\*appsettings.json:\*\*  
\`\`\`json  
{  
  "ConnectionStrings": {  
    "MonitoringGrid": "data source=192.168.166.11,1433;initial catalog=ProgressPlayDBTest;user id=saturn;password=XXXXXXXX;asynchronous processing=true;"  
  },  
  "Monitoring": {  
    "SmsGateway": "sms@gateway.com",  
    "AdminEmail": "tech-alerts@example.com",  
    "MaxParallelExecutions": 5,  
    "AlertRetryCount": 3,  
    "EnableSms": true,  
    "EnableEmail": true  
  }  
}  
\`\`\`

\*\*Worker Service:\*\*  
\`\`\`csharp  
public class MonitoringWorker : BackgroundService  
{  
    private readonly ILogger\<MonitoringWorker\> \_logger;  
    private readonly IServiceProvider \_provider;  
    private readonly MonitoringConfig \_config;

    public MonitoringWorker(ILogger\<MonitoringWorker\> logger,   
        IServiceProvider provider,  
        IOptions\<MonitoringConfig\> config)  
    {  
        \_logger \= logger;  
        \_provider \= provider;  
        \_config \= config.Value;  
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)  
    {  
        while (\!stoppingToken.IsCancellationRequested)  
        {  
            using var scope \= \_provider.CreateScope();  
            var context \= scope.ServiceProvider.GetRequiredService\<MonitoringContext\>();  
              
            var dueKpis \= await context.KPIs  
                .Where(k \=\> k.IsActive &&   
                    (k.LastRun \== null ||   
                     k.LastRun \< DateTime.UtcNow.AddMinutes(-k.Frequency)))  
                .Take(\_config.MaxParallelExecutions)  
                .ToListAsync(stoppingToken);

            var tasks \= dueKpis.Select(kpi \=\>   
                ProcessKpiAsync(kpi, scope.ServiceProvider, stoppingToken));  
              
            await Task.WhenAll(tasks);  
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);  
        }  
    }

    private async Task ProcessKpiAsync(KPI kpi, IServiceProvider services,   
        CancellationToken ct)  
    {  
        var context \= services.GetRequiredService\<MonitoringContext\>();  
        var emailService \= services.GetRequiredService\<EmailService\>();  
        var smsService \= services.GetRequiredService\<SmsService\>();  
          
        try  
        {  
            var (key, current, historical) \= await ExecuteKpiSp(kpi.SpName, kpi.Frequency);  
              
            var deviation \= CalculateDeviation(current, historical);  
            var absoluteChange \= Math.Abs(current \- historical);  
              
            bool shouldAlert \= (deviation \> kpi.Deviation) ||   
                              (kpi.MinimumThreshold.HasValue &&   
                               current \< kpi.MinimumThreshold);  
              
            if (shouldAlert && \!IsInCooldown(kpi))  
            {  
                var contacts \= await GetContactsForKpi(kpi.KpiId);  
                var alertResult \= await SendAlerts(kpi, contacts, current, historical,   
                    emailService, smsService);  
                  
                await LogAlert(kpi, contacts, current, historical,   
                    alertResult.Message);  
            }  
              
            kpi.LastRun \= DateTime.UtcNow;  
            await context.SaveChangesAsync(ct);  
        }  
        catch (Exception ex)  
        {  
            \_logger.LogError(ex, $"KPI {kpi.Indicator} processing failed");  
        }  
    }  
      
    // Helper methods implemented below...  
}  
\`\`\`

\#\#\# 4\. Core Components

\*\*KPI Execution Service:\*\*  
\`\`\`csharp  
public async Task\<(string Key, decimal Current, decimal Historical)\>   
    ExecuteKpiSp(string spName, int minutes)  
{  
    using var conn \= new SqlConnection(\_config.DbConnectionString);  
    using var cmd \= new SqlCommand(spName, conn) {  
        CommandType \= CommandType.StoredProcedure  
    };  
      
    cmd.Parameters.AddWithValue("@ForLastMinutes", minutes);  
    cmd.Parameters.Add("@Key", SqlDbType.NVarChar, 255).Direction \= ParameterDirection.Output;  
    cmd.Parameters.Add("@CurrentValue", SqlDbType.Decimal).Direction \= ParameterDirection.Output;  
    cmd.Parameters.Add("@HistoricalValue", SqlDbType.Decimal).Direction \= ParameterDirection.Output;  
      
    await conn.OpenAsync();  
    await cmd.ExecuteNonQueryAsync();  
      
    return (  
        cmd.Parameters\["@Key"\].Value.ToString(),  
        (decimal)cmd.Parameters\["@CurrentValue"\].Value,  
        (decimal)cmd.Parameters\["@HistoricalValue"\].Value  
    );  
}  
\`\`\`

\*\*Alert Service:\*\*  
\`\`\`csharp  
public async Task\<AlertResult\> SendAlerts(KPI kpi, List\<Contact\> contacts,  
    decimal current, decimal historical,   
    EmailService emailService, SmsService smsService)  
{  
    var subject \= BuildTemplate(kpi.SubjectTemplate, kpi, current, historical);  
    var body \= BuildTemplate(kpi.DescriptionTemplate, kpi, current, historical);  
    var result \= new AlertResult();  
      
    foreach (var contact in contacts.Where(c \=\> c.IsActive))  
    {  
        try  
        {  
            if (kpi.Priority \== 1 && \_config.EnableSms && \!string.IsNullOrEmpty(contact.Phone))  
            {  
                await smsService.SendAsync(  
                    contact.Phone,   
                    subject,  
                    \_config.SmsGateway);  
                result.SmsSent++;  
            }  
              
            if (kpi.Priority \== 2 && \_config.EnableEmail && \!string.IsNullOrEmpty(contact.Email))  
            {  
                await emailService.SendAsync(  
                    contact.Email,  
                    subject,  
                    body);  
                result.EmailsSent++;  
            }  
        }  
        catch (Exception ex)  
        {  
            \_logger.LogWarning($"Alert failed for {contact.Name}: {ex.Message}");  
        }  
    }  
      
    return result;  
}

private string BuildTemplate(string template, KPI kpi, decimal current, decimal historical)  
{  
    return template  
        .Replace("{frequency}", kpi.Frequency.ToString())  
        .Replace("{current}", current.ToString("N2"))  
        .Replace("{historical}", historical.ToString("N2"))  
        .Replace("{deviation}", CalculateDeviation(current, historical).ToString("N2"));  
}  
\`\`\`

\#\#\# 5\. Robustness Features

1\. \*\*Parallel Processing:\*\*  
   \- Configurable max parallel executions  
   \- Async/await pattern  
   \- CancellationToken support

2\. \*\*Alert Management:\*\*  
   \- Cooldown periods (prevent alert flooding)  
   \- Absolute value thresholds  
   \- Multi-channel retry logic  
   \- Detailed alert history with resolution tracking

3\. \*\*Failure Handling:\*\*  
   \- Transient fault handling with retries  
   \- Isolated KPI processing (errors don't block others)  
   \- Dead-letter queue for failed alerts  
   \- Comprehensive logging

4\. \*\*Toggleable Features:\*\*  
   \`\`\`json  
   "Monitoring": {  
     "EnableSms": true,  
     "EnableEmail": true,  
     "EnableHistoricalComparison": true,  
     "EnableAbsoluteThresholds": true  
   }  
   \`\`\`

5\. \*\*Scalability:\*\*  
   \- Parameterized queries  
   \- Async database operations  
   \- Configurable batch sizes  
   \- Connection pooling

\#\#\# 6\. Deployment Considerations

1\. \*\*Database Optimization:\*\*  
   \- Index historical data table  
   \- Partition large tables  
   \- Use read-only replica for queries

2\. \*\*High Availability:\*\*  
   \- Containerized deployment  
   \- Health checks endpoint  
   \- Kubernetes deployment  
   \- Load-balanced instances

3\. \*\*Monitoring:\*\*  
   \- Integrate Application Insights  
   \- Dashboard for KPIs status  
   \- Alert volume monitoring  
   \- Performance metrics

4\. \*\*Security:\*\*  
   \- Encrypt connection strings  
   \- Use Managed Identity for DB access  
   \- Validate all SP inputs  
   \- Principle of least privilege

This solution provides:  
\- A configurable, extensible architecture  
\- Production-grade resilience  
\- Comprehensive alert management  
\- Detailed historical tracking  
\- Toggleable features through configuration  
\- Parallel processing capabilities  
\- Modern .NET 8 implementation  
\- SQL Server best practices

The system can be extended with additional features like:  
\- Webhook notifications  
\- Escalation policies  
\- Machine learning-based anomaly detection  
\- Dashboard integration  
\- Mobile push notifications  
