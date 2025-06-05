# MonitoringGrid Deployment Script
# This script automates the deployment of MonitoringGrid to various environments

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("local", "staging", "production")]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,
    
    [Parameter(Mandatory=$false)]
    [switch]$Rollback,
    
    [Parameter(Mandatory=$false)]
    [string]$Namespace = "monitoring-grid"
)

# Configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors for output
$Red = "`e[31m"
$Green = "`e[32m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = $Reset)
    Write-Host "$Color$Message$Reset"
}

function Write-Step {
    param([string]$Message)
    Write-ColorOutput "🚀 $Message" $Blue
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✅ $Message" $Green
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠️  $Message" $Yellow
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "❌ $Message" $Red
}

function Test-Prerequisites {
    Write-Step "Checking prerequisites..."
    
    # Check Docker
    try {
        docker --version | Out-Null
        Write-Success "Docker is available"
    }
    catch {
        Write-Error "Docker is not installed or not in PATH"
        exit 1
    }
    
    # Check kubectl for Kubernetes deployments
    if ($Environment -ne "local") {
        try {
            kubectl version --client | Out-Null
            Write-Success "kubectl is available"
        }
        catch {
            Write-Error "kubectl is not installed or not in PATH"
            exit 1
        }
    }
    
    # Check .NET SDK
    try {
        dotnet --version | Out-Null
        Write-Success ".NET SDK is available"
    }
    catch {
        Write-Error ".NET SDK is not installed or not in PATH"
        exit 1
    }
    
    # Check Node.js
    try {
        node --version | Out-Null
        Write-Success "Node.js is available"
    }
    catch {
        Write-Error "Node.js is not installed or not in PATH"
        exit 1
    }
}

function Build-Application {
    if ($SkipBuild) {
        Write-Warning "Skipping build step"
        return
    }
    
    Write-Step "Building application..."
    
    # Build backend
    Write-Step "Building .NET backend..."
    dotnet restore
    dotnet build --configuration Release --no-restore
    
    # Build frontend
    Write-Step "Building React frontend..."
    Set-Location "MonitoringGrid.Frontend"
    npm ci
    npm run build
    Set-Location ".."
    
    Write-Success "Application built successfully"
}

function Run-Tests {
    if ($SkipTests) {
        Write-Warning "Skipping tests"
        return
    }
    
    Write-Step "Running tests..."
    
    # Backend tests
    Write-Step "Running backend tests..."
    dotnet test --configuration Release --no-build --verbosity normal
    
    # Frontend tests
    Write-Step "Running frontend tests..."
    Set-Location "MonitoringGrid.Frontend"
    npm run test:ci
    Set-Location ".."
    
    Write-Success "All tests passed"
}

function Build-DockerImage {
    Write-Step "Building Docker image..."
    
    $imageName = "monitoring-grid"
    $fullImageName = "${imageName}:${ImageTag}"
    
    docker build -t $fullImageName .
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker build failed"
        exit 1
    }
    
    Write-Success "Docker image built: $fullImageName"
    return $fullImageName
}

function Deploy-Local {
    Write-Step "Deploying to local environment..."
    
    # Stop existing containers
    docker-compose down
    
    # Start services
    docker-compose up -d
    
    # Wait for services to be ready
    Write-Step "Waiting for services to be ready..."
    Start-Sleep -Seconds 30
    
    # Health check
    $maxRetries = 10
    $retryCount = 0
    
    do {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                Write-Success "Application is healthy"
                break
            }
        }
        catch {
            $retryCount++
            if ($retryCount -ge $maxRetries) {
                Write-Error "Application failed to start"
                exit 1
            }
            Write-Warning "Waiting for application to start... (attempt $retryCount/$maxRetries)"
            Start-Sleep -Seconds 10
        }
    } while ($retryCount -lt $maxRetries)
    
    Write-Success "Local deployment completed"
    Write-ColorOutput "Application URL: http://localhost:8080" $Green
    Write-ColorOutput "Grafana URL: http://localhost:3000" $Green
    Write-ColorOutput "Prometheus URL: http://localhost:9090" $Green
}

function Deploy-Kubernetes {
    param([string]$Environment)
    
    Write-Step "Deploying to $Environment environment..."
    
    # Create namespace if it doesn't exist
    kubectl create namespace $Namespace --dry-run=client -o yaml | kubectl apply -f -
    
    # Apply configurations
    Write-Step "Applying Kubernetes configurations..."
    
    # Apply in order
    kubectl apply -f k8s/namespace.yaml
    kubectl apply -f k8s/configmap.yaml
    kubectl apply -f k8s/secrets.yaml
    kubectl apply -f k8s/pvc.yaml
    kubectl apply -f k8s/service.yaml
    kubectl apply -f k8s/deployment.yaml
    kubectl apply -f k8s/ingress.yaml
    
    # Wait for deployment to be ready
    Write-Step "Waiting for deployment to be ready..."
    kubectl rollout status deployment/monitoring-grid-api -n $Namespace --timeout=300s
    
    # Verify deployment
    $pods = kubectl get pods -n $Namespace -l app=monitoring-grid-api -o jsonpath='{.items[*].status.phase}'
    if ($pods -notcontains "Running") {
        Write-Error "Some pods are not running"
        kubectl get pods -n $Namespace
        exit 1
    }
    
    Write-Success "$Environment deployment completed"
    
    # Get service URL
    if ($Environment -eq "production") {
        $ingressUrl = kubectl get ingress monitoring-grid-ingress -n $Namespace -o jsonpath='{.spec.rules[0].host}'
        Write-ColorOutput "Application URL: https://$ingressUrl" $Green
    }
}

function Rollback-Deployment {
    Write-Step "Rolling back deployment..."
    
    if ($Environment -eq "local") {
        docker-compose down
        # Restore previous version logic here
        Write-Success "Local rollback completed"
    }
    else {
        kubectl rollout undo deployment/monitoring-grid-api -n $Namespace
        kubectl rollout status deployment/monitoring-grid-api -n $Namespace --timeout=300s
        Write-Success "Kubernetes rollback completed"
    }
}

function Show-DeploymentInfo {
    Write-Step "Deployment Information"
    
    Write-ColorOutput "Environment: $Environment" $Blue
    Write-ColorOutput "Image Tag: $ImageTag" $Blue
    Write-ColorOutput "Namespace: $Namespace" $Blue
    
    if ($Environment -eq "local") {
        Write-ColorOutput "Services:" $Blue
        docker-compose ps
    }
    else {
        Write-ColorOutput "Kubernetes Resources:" $Blue
        kubectl get all -n $Namespace
    }
}

# Main execution
try {
    Write-ColorOutput "🚀 MonitoringGrid Deployment Script" $Blue
    Write-ColorOutput "Environment: $Environment" $Blue
    Write-ColorOutput "Image Tag: $ImageTag" $Blue
    
    if ($Rollback) {
        Rollback-Deployment
        exit 0
    }
    
    Test-Prerequisites
    Build-Application
    Run-Tests
    
    if ($Environment -eq "local") {
        Build-DockerImage
        Deploy-Local
    }
    else {
        Build-DockerImage
        Deploy-Kubernetes -Environment $Environment
    }
    
    Show-DeploymentInfo
    
    Write-Success "🎉 Deployment completed successfully!"
}
catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    exit 1
}
