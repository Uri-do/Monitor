# PowerShell script to inspect the stored procedure parameters
# This script will check what parameters [stats].[stp_MonitorTransactions] expects

$ServerInstance = "192.168.166.11,1433"
$Database = "ProgressPlayDBTest"
$Username = "saturn"
$Password = "Vt0zXXc800"

Write-Host "Inspecting stored procedure: [stats].[stp_MonitorTransactions]" -ForegroundColor Green
Write-Host ""

try {
    # Create connection string
    $ConnectionString = "Data Source=$ServerInstance;Initial Catalog=$Database;User ID=$Username;Password=$Password;TrustServerCertificate=true"
    
    # Create SQL connection
    $Connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $Connection.Open()
    
    Write-Host "Connected to ProgressPlayDBTest database successfully." -ForegroundColor Green
    
    # Query to get stored procedure parameters
    $Query = @"
SELECT 
    p.parameter_id,
    p.name AS parameter_name,
    TYPE_NAME(p.user_type_id) AS parameter_type,
    p.max_length,
    p.precision,
    p.scale,
    p.is_output,
    p.has_default_value,
    p.default_value
FROM sys.procedures sp
INNER JOIN sys.parameters p ON sp.object_id = p.object_id
WHERE sp.name = 'stp_MonitorTransactions' 
    AND SCHEMA_NAME(sp.schema_id) = 'stats'
ORDER BY p.parameter_id
"@
    
    $Command = New-Object System.Data.SqlClient.SqlCommand($Query, $Connection)
    $Reader = $Command.ExecuteReader()
    
    Write-Host "Parameters for [stats].[stp_MonitorTransactions]:" -ForegroundColor Yellow
    Write-Host "================================================================"
    
    $HasParameters = $false
    while ($Reader.Read()) {
        $HasParameters = $true
        $ParamId = $Reader["parameter_id"]
        $ParamName = $Reader["parameter_name"]
        $ParamType = $Reader["parameter_type"]
        $MaxLength = $Reader["max_length"]
        $Precision = $Reader["precision"]
        $Scale = $Reader["scale"]
        $IsOutput = $Reader["is_output"]
        $HasDefault = $Reader["has_default_value"]
        $DefaultValue = $Reader["default_value"]
        
        $Direction = if ($IsOutput) { "OUTPUT" } else { "INPUT" }
        $TypeInfo = $ParamType
        if ($Precision -gt 0) {
            $TypeInfo += "($Precision,$Scale)"
        } elseif ($MaxLength -gt 0 -and $MaxLength -ne -1) {
            $TypeInfo += "($MaxLength)"
        }
        
        Write-Host "$ParamId. $ParamName - $TypeInfo [$Direction]" -ForegroundColor Cyan
        if ($HasDefault) {
            Write-Host "   Default: $DefaultValue" -ForegroundColor Gray
        }
    }
    
    $Reader.Close()
    
    if (-not $HasParameters) {
        Write-Host "No parameters found for this stored procedure." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "================================================================"
    
    # Also check if the stored procedure exists
    $ExistsQuery = @"
SELECT COUNT(*) as sp_count
FROM sys.procedures sp
WHERE sp.name = 'stp_MonitorTransactions' 
    AND SCHEMA_NAME(sp.schema_id) = 'stats'
"@
    
    $ExistsCommand = New-Object System.Data.SqlClient.SqlCommand($ExistsQuery, $Connection)
    $Count = $ExistsCommand.ExecuteScalar()
    
    if ($Count -eq 0) {
        Write-Host "⚠️  Stored procedure [stats].[stp_MonitorTransactions] does not exist!" -ForegroundColor Red
        
        # Let's check what stored procedures do exist in the stats schema
        $ListQuery = @"
SELECT 
    SCHEMA_NAME(sp.schema_id) as schema_name,
    sp.name as procedure_name,
    sp.create_date,
    sp.modify_date
FROM sys.procedures sp
WHERE SCHEMA_NAME(sp.schema_id) = 'stats'
ORDER BY sp.name
"@
        
        $ListCommand = New-Object System.Data.SqlClient.SqlCommand($ListQuery, $Connection)
        $ListReader = $ListCommand.ExecuteReader()
        
        Write-Host ""
        Write-Host "Available stored procedures in [stats] schema:" -ForegroundColor Yellow
        Write-Host "================================================"
        
        $HasProcs = $false
        while ($ListReader.Read()) {
            $HasProcs = $true
            $SchemaName = $ListReader["schema_name"]
            $ProcName = $ListReader["procedure_name"]
            $CreateDate = $ListReader["create_date"]
            $ModifyDate = $ListReader["modify_date"]
            
            Write-Host "[$SchemaName].[$ProcName] (Created: $CreateDate, Modified: $ModifyDate)" -ForegroundColor Cyan
        }
        
        $ListReader.Close()
        
        if (-not $HasProcs) {
            Write-Host "No stored procedures found in [stats] schema." -ForegroundColor Yellow
        }
    } else {
        Write-Host "✅ Stored procedure [stats].[stp_MonitorTransactions] exists." -ForegroundColor Green
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)" -ForegroundColor Red
} finally {
    if ($Connection -and $Connection.State -eq [System.Data.ConnectionState]::Open) {
        $Connection.Close()
        Write-Host ""
        Write-Host "Database connection closed." -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
