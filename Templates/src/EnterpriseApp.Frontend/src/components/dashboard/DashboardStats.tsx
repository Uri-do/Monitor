import { useState, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { StatsCard } from '@/components/ui/Card'
import { Icon } from '@/components/ui/Icon'
import { LoadingSpinner, Skeleton } from '@/components/ui/LoadingSpinner'
import { Alert } from '@/components/ui/Alert'

// Mock API service - replace with actual API calls
const fetchDashboardStats = async () => {
  // Simulate API delay
  await new Promise(resolve => setTimeout(resolve, 1000))
  
  // Mock data - replace with actual API response
  return {
    totalDomainEntities: 1247,
    activeDomainEntities: 1089,
    totalUsers: 156,
    activeUsers: 89,
    systemHealth: 98.5,
    jobsProcessed: 2847,
    trends: {
      domainEntities: { value: 12, direction: 'up' as const },
      users: { value: 8, direction: 'up' as const },
      health: { value: 2, direction: 'down' as const },
      jobs: { value: 23, direction: 'up' as const },
    }
  }
}

export function DashboardStats() {
  const { data: stats, isLoading, error, refetch } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: fetchDashboardStats,
    refetchInterval: 30000, // Refetch every 30 seconds
    staleTime: 10000, // Consider data stale after 10 seconds
  })

  if (error) {
    return (
      <Alert
        variant="error"
        title="Failed to load statistics"
        description="Unable to fetch dashboard statistics. Please try again."
        actions={
          <button
            onClick={() => refetch()}
            className="text-sm font-medium text-error-600 hover:text-error-500"
          >
            Retry
          </button>
        }
      />
    )
  }

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="rounded-lg border border-gray-200 p-6 dark:border-gray-700">
            <div className="space-y-4">
              <Skeleton className="h-4 w-1/3" />
              <Skeleton className="h-8 w-1/2" />
              <Skeleton className="h-3 w-2/3" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6">
      {/* Total Domain Entities */}
      <StatsCard
        title="Total Entities"
        value={stats?.totalDomainEntities?.toLocaleString() || '0'}
        description={`${stats?.activeDomainEntities || 0} active`}
        icon={<Icon name="database" className="h-4 w-4" />}
        trend={stats?.trends.domainEntities ? {
          value: stats.trends.domainEntities.value,
          direction: stats.trends.domainEntities.direction,
          label: 'from last month'
        } : undefined}
      />

      {/* Total Users */}
      <StatsCard
        title="Total Users"
        value={stats?.totalUsers?.toLocaleString() || '0'}
        description={`${stats?.activeUsers || 0} active today`}
        icon={<Icon name="users" className="h-4 w-4" />}
        trend={stats?.trends.users ? {
          value: stats.trends.users.value,
          direction: stats.trends.users.direction,
          label: 'new this week'
        } : undefined}
      />

      {/* System Health */}
      <StatsCard
        title="System Health"
        value={`${stats?.systemHealth || 0}%`}
        description="Overall system status"
        icon={<Icon name="heart" className="h-4 w-4" />}
        trend={stats?.trends.health ? {
          value: stats.trends.health.value,
          direction: stats.trends.health.direction,
          label: 'from last check'
        } : undefined}
      />

      {/* Jobs Processed */}
      <StatsCard
        title="Jobs Processed"
        value={stats?.jobsProcessed?.toLocaleString() || '0'}
        description="Background jobs today"
        icon={<Icon name="cog" className="h-4 w-4" />}
        trend={stats?.trends.jobs ? {
          value: stats.trends.jobs.value,
          direction: stats.trends.jobs.direction,
          label: 'from yesterday'
        } : undefined}
      />
    </div>
  )
}

// Real-time stats component with WebSocket updates
export function RealTimeStats() {
  const [stats, setStats] = useState<any>(null)
  const [isConnected, setIsConnected] = useState(false)

  useEffect(() => {
    // Mock WebSocket connection - replace with actual WebSocket
    const mockWebSocket = () => {
      setIsConnected(true)
      
      const interval = setInterval(() => {
        // Simulate real-time updates
        setStats((prev: any) => ({
          ...prev,
          activeUsers: Math.floor(Math.random() * 100) + 50,
          jobsProcessed: (prev?.jobsProcessed || 0) + Math.floor(Math.random() * 5),
          systemHealth: 95 + Math.random() * 5,
        }))
      }, 5000)

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
          Connecting to real-time updates...
        </span>
      </div>
    )
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div className="text-center p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
        <div className="text-2xl font-bold text-green-600 dark:text-green-400">
          {stats?.activeUsers || 0}
        </div>
        <div className="text-sm text-green-600 dark:text-green-400">
          Active Users
        </div>
      </div>

      <div className="text-center p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
        <div className="text-2xl font-bold text-blue-600 dark:text-blue-400">
          {stats?.jobsProcessed || 0}
        </div>
        <div className="text-sm text-blue-600 dark:text-blue-400">
          Jobs Processed
        </div>
      </div>

      <div className="text-center p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
        <div className="text-2xl font-bold text-purple-600 dark:text-purple-400">
          {stats?.systemHealth?.toFixed(1) || 0}%
        </div>
        <div className="text-sm text-purple-600 dark:text-purple-400">
          System Health
        </div>
      </div>
    </div>
  )
}

// Compact stats for smaller spaces
export function CompactStats() {
  const { data: stats, isLoading } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: fetchDashboardStats,
    refetchInterval: 60000, // Refetch every minute
  })

  if (isLoading) {
    return (
      <div className="flex space-x-4">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-8 w-16" />
        ))}
      </div>
    )
  }

  return (
    <div className="flex items-center space-x-6 text-sm">
      <div className="flex items-center space-x-2">
        <Icon name="database" className="h-4 w-4 text-gray-500" />
        <span className="font-medium">{stats?.totalDomainEntities || 0}</span>
        <span className="text-gray-500">entities</span>
      </div>
      
      <div className="flex items-center space-x-2">
        <Icon name="users" className="h-4 w-4 text-gray-500" />
        <span className="font-medium">{stats?.activeUsers || 0}</span>
        <span className="text-gray-500">active</span>
      </div>
      
      <div className="flex items-center space-x-2">
        <Icon name="heart" className="h-4 w-4 text-gray-500" />
        <span className="font-medium">{stats?.systemHealth || 0}%</span>
        <span className="text-gray-500">health</span>
      </div>
    </div>
  )
}
