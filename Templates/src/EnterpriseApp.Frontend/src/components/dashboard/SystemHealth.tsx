import { useState, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Icon, IconName } from '@/components/ui/Icon'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Progress } from '@/components/ui/Progress'
import { LoadingSpinner, Skeleton } from '@/components/ui/LoadingSpinner'
import { Alert } from '@/components/ui/Alert'

interface HealthMetric {
  id: string
  name: string
  status: 'healthy' | 'warning' | 'critical' | 'unknown'
  value: number
  unit: string
  threshold: {
    warning: number
    critical: number
  }
  description: string
  lastChecked: string
}

interface SystemHealthData {
  overall: 'healthy' | 'warning' | 'critical'
  score: number
  metrics: HealthMetric[]
  uptime: string
  lastUpdate: string
}

// Mock API service - replace with actual API calls
const fetchSystemHealth = async (): Promise<SystemHealthData> => {
  // Simulate API delay
  await new Promise(resolve => setTimeout(resolve, 1200))
  
  // Mock data - replace with actual API response
  return {
    overall: 'healthy',
    score: 98.5,
    uptime: '15 days, 4 hours',
    lastUpdate: new Date().toISOString(),
    metrics: [
      {
        id: 'cpu',
        name: 'CPU Usage',
        status: 'healthy',
        value: 23.5,
        unit: '%',
        threshold: { warning: 70, critical: 90 },
        description: 'Current CPU utilization',
        lastChecked: new Date(Date.now() - 30000).toISOString()
      },
      {
        id: 'memory',
        name: 'Memory Usage',
        status: 'healthy',
        value: 45.2,
        unit: '%',
        threshold: { warning: 80, critical: 95 },
        description: 'RAM utilization',
        lastChecked: new Date(Date.now() - 30000).toISOString()
      },
      {
        id: 'disk',
        name: 'Disk Space',
        status: 'warning',
        value: 78.9,
        unit: '%',
        threshold: { warning: 75, critical: 90 },
        description: 'Storage utilization',
        lastChecked: new Date(Date.now() - 30000).toISOString()
      },
      {
        id: 'database',
        name: 'Database',
        status: 'healthy',
        value: 12.3,
        unit: 'ms',
        threshold: { warning: 100, critical: 500 },
        description: 'Average query response time',
        lastChecked: new Date(Date.now() - 30000).toISOString()
      },
      {
        id: 'api',
        name: 'API Response',
        status: 'healthy',
        value: 245,
        unit: 'ms',
        threshold: { warning: 1000, critical: 3000 },
        description: 'Average API response time',
        lastChecked: new Date(Date.now() - 30000).toISOString()
      }
    ]
  }
}

export function SystemHealth() {
  const { data: health, isLoading, error, refetch } = useQuery({
    queryKey: ['system-health'],
    queryFn: fetchSystemHealth,
    refetchInterval: 30000, // Refetch every 30 seconds
  })

  const getStatusIcon = (status: HealthMetric['status']): IconName => {
    switch (status) {
      case 'healthy': return 'check-circle'
      case 'warning': return 'alert-triangle'
      case 'critical': return 'x-circle'
      case 'unknown': return 'help-circle'
      default: return 'help-circle'
    }
  }

  const getStatusColor = (status: HealthMetric['status']) => {
    switch (status) {
      case 'healthy': return 'text-green-600 dark:text-green-400'
      case 'warning': return 'text-yellow-600 dark:text-yellow-400'
      case 'critical': return 'text-red-600 dark:text-red-400'
      case 'unknown': return 'text-gray-600 dark:text-gray-400'
      default: return 'text-gray-600 dark:text-gray-400'
    }
  }

  const getProgressColor = (status: HealthMetric['status']) => {
    switch (status) {
      case 'healthy': return 'bg-green-500'
      case 'warning': return 'bg-yellow-500'
      case 'critical': return 'bg-red-500'
      case 'unknown': return 'bg-gray-500'
      default: return 'bg-gray-500'
    }
  }

  if (error) {
    return (
      <Alert
        variant="error"
        title="Health check failed"
        description="Unable to fetch system health data."
        actions={
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Retry
          </Button>
        }
      />
    )
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Skeleton className="h-6 w-32" />
          <Skeleton className="h-6 w-16" />
        </div>
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="space-y-2">
            <div className="flex items-center justify-between">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-12" />
            </div>
            <Skeleton className="h-2 w-full" />
          </div>
        ))}
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Overall Health Status */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-2">
          <Icon 
            name={getStatusIcon(health?.overall || 'unknown')} 
            className={`h-5 w-5 ${getStatusColor(health?.overall || 'unknown')}`}
          />
          <span className="font-medium text-gray-900 dark:text-white">
            System Health
          </span>
        </div>
        <div className="flex items-center space-x-2">
          <span className="text-lg font-bold text-gray-900 dark:text-white">
            {health?.score}%
          </span>
          <Badge 
            variant={
              health?.overall === 'healthy' ? 'success' :
              health?.overall === 'warning' ? 'warning' : 'error'
            }
          >
            {health?.overall}
          </Badge>
        </div>
      </div>

      {/* Uptime */}
      <div className="text-sm text-gray-600 dark:text-gray-400">
        <span className="font-medium">Uptime:</span> {health?.uptime}
      </div>

      {/* Health Metrics */}
      <div className="space-y-3">
        {health?.metrics.map((metric) => (
          <div key={metric.id} className="space-y-2">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <Icon 
                  name={getStatusIcon(metric.status)} 
                  className={`h-4 w-4 ${getStatusColor(metric.status)}`}
                />
                <span className="text-sm font-medium text-gray-900 dark:text-white">
                  {metric.name}
                </span>
              </div>
              <span className="text-sm text-gray-600 dark:text-gray-400">
                {metric.value}{metric.unit}
              </span>
            </div>
            
            <div className="relative">
              <div className="w-full bg-gray-200 rounded-full h-2 dark:bg-gray-700">
                <div
                  className={`h-2 rounded-full transition-all duration-300 ${getProgressColor(metric.status)}`}
                  style={{ 
                    width: `${Math.min((metric.value / metric.threshold.critical) * 100, 100)}%` 
                  }}
                />
              </div>
              
              {/* Threshold indicators */}
              <div className="absolute top-0 h-2 flex items-center">
                <div 
                  className="w-0.5 h-full bg-yellow-400"
                  style={{ 
                    marginLeft: `${(metric.threshold.warning / metric.threshold.critical) * 100}%` 
                  }}
                />
                <div 
                  className="w-0.5 h-full bg-red-400 ml-auto"
                  style={{ 
                    marginRight: '0%' 
                  }}
                />
              </div>
            </div>
            
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {metric.description}
            </p>
          </div>
        ))}
      </div>

      {/* Last Update */}
      <div className="pt-3 border-t border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400">
          <span>
            Last updated: {health?.lastUpdate ? 
              new Date(health.lastUpdate).toLocaleTimeString() : 
              'Unknown'
            }
          </span>
          <Button
            variant="ghost"
            size="sm"
            leftIcon="refresh"
            onClick={() => refetch()}
            className="h-6 px-2 text-xs"
          >
            Refresh
          </Button>
        </div>
      </div>
    </div>
  )
}

// Real-time health monitor with WebSocket updates
export function RealTimeHealthMonitor() {
  const [health, setHealth] = useState<SystemHealthData | null>(null)
  const [isConnected, setIsConnected] = useState(false)

  useEffect(() => {
    // Mock WebSocket connection - replace with actual WebSocket
    const mockWebSocket = () => {
      setIsConnected(true)
      
      const interval = setInterval(() => {
        // Simulate real-time health updates
        setHealth(prev => {
          if (!prev) return null
          
          return {
            ...prev,
            score: 95 + Math.random() * 5,
            metrics: prev.metrics.map(metric => ({
              ...metric,
              value: metric.value + (Math.random() - 0.5) * 5,
              status: metric.value > metric.threshold.critical ? 'critical' :
                     metric.value > metric.threshold.warning ? 'warning' : 'healthy'
            }))
          }
        })
      }, 3000)

      return () => {
        clearInterval(interval)
        setIsConnected(false)
      }
    }

    const cleanup = mockWebSocket()
    return cleanup
  }, [])

  if (!isConnected) {
    return (
      <div className="flex items-center justify-center p-4">
        <LoadingSpinner size="sm" />
        <span className="ml-2 text-sm text-gray-600 dark:text-gray-400">
          Connecting to health monitor...
        </span>
      </div>
    )
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium">Real-time Health</span>
        <div className="flex items-center space-x-1">
          <div className="h-2 w-2 bg-green-500 rounded-full animate-pulse" />
          <span className="text-xs text-green-600">Live</span>
        </div>
      </div>
      
      {health && (
        <div className="text-2xl font-bold text-center">
          {health.score.toFixed(1)}%
        </div>
      )}
    </div>
  )
}
