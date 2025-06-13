import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { formatDistanceToNow } from 'date-fns'
import { Icon, IconName } from '@/components/ui/Icon'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Avatar } from '@/components/ui/Avatar'
import { LoadingSpinner, Skeleton } from '@/components/ui/LoadingSpinner'
import { Alert } from '@/components/ui/Alert'

interface ActivityItem {
  id: string
  type: 'user' | 'system' | 'domain-entity' | 'job' | 'security'
  action: string
  description: string
  user?: {
    id: string
    name: string
    avatar?: string
  }
  timestamp: string
  metadata?: Record<string, any>
  severity?: 'low' | 'medium' | 'high' | 'critical'
}

// Mock API service - replace with actual API calls
const fetchRecentActivity = async (): Promise<ActivityItem[]> => {
  // Simulate API delay
  await new Promise(resolve => setTimeout(resolve, 800))
  
  // Mock data - replace with actual API response
  return [
    {
      id: '1',
      type: 'user',
      action: 'User Login',
      description: 'John Doe logged in from Chrome on Windows',
      user: { id: '1', name: 'John Doe', avatar: undefined },
      timestamp: new Date(Date.now() - 5 * 60 * 1000).toISOString(), // 5 minutes ago
      severity: 'low'
    },
    {
      id: '2',
      type: 'domain-entity',
      action: 'Entity Created',
      description: 'New domain entity "Customer Analytics" was created',
      user: { id: '2', name: 'Jane Smith', avatar: undefined },
      timestamp: new Date(Date.now() - 15 * 60 * 1000).toISOString(), // 15 minutes ago
      severity: 'medium'
    },
    {
      id: '3',
      type: 'job',
      action: 'Job Completed',
      description: 'Data export job completed successfully (2,847 records)',
      timestamp: new Date(Date.now() - 30 * 60 * 1000).toISOString(), // 30 minutes ago
      severity: 'low'
    },
    {
      id: '4',
      type: 'system',
      action: 'System Update',
      description: 'Application updated to version 2.1.3',
      timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
      severity: 'medium'
    },
    {
      id: '5',
      type: 'security',
      action: 'Failed Login Attempt',
      description: 'Multiple failed login attempts detected from IP 192.168.1.100',
      timestamp: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(), // 3 hours ago
      severity: 'high'
    },
    {
      id: '6',
      type: 'domain-entity',
      action: 'Entity Updated',
      description: 'Domain entity "Sales Report" was modified',
      user: { id: '3', name: 'Mike Johnson', avatar: undefined },
      timestamp: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(), // 4 hours ago
      severity: 'low'
    }
  ]
}

export function RecentActivity() {
  const [filter, setFilter] = useState<'all' | 'user' | 'system' | 'domain-entity' | 'job' | 'security'>('all')
  
  const { data: activities, isLoading, error, refetch } = useQuery({
    queryKey: ['recent-activity', filter],
    queryFn: fetchRecentActivity,
    refetchInterval: 30000, // Refetch every 30 seconds
  })

  const getActivityIcon = (type: ActivityItem['type']): IconName => {
    switch (type) {
      case 'user': return 'user'
      case 'system': return 'settings'
      case 'domain-entity': return 'database'
      case 'job': return 'cog'
      case 'security': return 'shield'
      default: return 'activity'
    }
  }

  const getActivityColor = (type: ActivityItem['type']) => {
    switch (type) {
      case 'user': return 'text-blue-600 dark:text-blue-400'
      case 'system': return 'text-purple-600 dark:text-purple-400'
      case 'domain-entity': return 'text-green-600 dark:text-green-400'
      case 'job': return 'text-orange-600 dark:text-orange-400'
      case 'security': return 'text-red-600 dark:text-red-400'
      default: return 'text-gray-600 dark:text-gray-400'
    }
  }

  const getSeverityBadge = (severity?: ActivityItem['severity']) => {
    if (!severity) return null
    
    const variants = {
      low: 'secondary' as const,
      medium: 'warning' as const,
      high: 'error' as const,
      critical: 'error' as const,
    }

    return <Badge variant={variants[severity]} size="sm">{severity}</Badge>
  }

  const filteredActivities = activities?.filter(activity => 
    filter === 'all' || activity.type === filter
  ) || []

  if (error) {
    return (
      <Alert
        variant="error"
        title="Failed to load activity"
        description="Unable to fetch recent activity. Please try again."
        actions={
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Retry
          </Button>
        }
      />
    )
  }

  return (
    <div className="space-y-4">
      {/* Filter Tabs */}
      <div className="flex flex-wrap gap-2">
        {[
          { key: 'all', label: 'All Activity', icon: 'activity' },
          { key: 'user', label: 'Users', icon: 'user' },
          { key: 'system', label: 'System', icon: 'settings' },
          { key: 'domain-entity', label: 'Entities', icon: 'database' },
          { key: 'job', label: 'Jobs', icon: 'cog' },
          { key: 'security', label: 'Security', icon: 'shield' },
        ].map(({ key, label, icon }) => (
          <Button
            key={key}
            variant={filter === key ? 'default' : 'ghost'}
            size="sm"
            leftIcon={icon as IconName}
            onClick={() => setFilter(key as any)}
          >
            {label}
          </Button>
        ))}
      </div>

      {/* Activity List */}
      <div className="space-y-3">
        {isLoading ? (
          // Loading skeleton
          Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex items-start space-x-3 p-3 rounded-lg border border-gray-200 dark:border-gray-700">
              <Skeleton className="h-8 w-8 rounded-full" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-4 w-1/3" />
                <Skeleton className="h-3 w-2/3" />
                <Skeleton className="h-3 w-1/4" />
              </div>
            </div>
          ))
        ) : filteredActivities.length === 0 ? (
          <div className="text-center py-8 text-gray-500 dark:text-gray-400">
            <Icon name="activity" className="mx-auto h-8 w-8 mb-2" />
            <p>No recent activity found</p>
          </div>
        ) : (
          filteredActivities.map((activity) => (
            <div
              key={activity.id}
              className="flex items-start space-x-3 p-3 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
            >
              {/* Activity Icon or User Avatar */}
              <div className="flex-shrink-0">
                {activity.user ? (
                  <Avatar
                    src={activity.user.avatar}
                    name={activity.user.name}
                    size="sm"
                  />
                ) : (
                  <div className={`p-2 rounded-full bg-gray-100 dark:bg-gray-800 ${getActivityColor(activity.type)}`}>
                    <Icon name={getActivityIcon(activity.type)} className="h-4 w-4" />
                  </div>
                )}
              </div>

              {/* Activity Content */}
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between">
                  <p className="text-sm font-medium text-gray-900 dark:text-white">
                    {activity.action}
                  </p>
                  <div className="flex items-center space-x-2">
                    {getSeverityBadge(activity.severity)}
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                      {formatDistanceToNow(new Date(activity.timestamp), { addSuffix: true })}
                    </span>
                  </div>
                </div>
                
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  {activity.description}
                </p>

                {activity.user && (
                  <p className="text-xs text-gray-500 dark:text-gray-500 mt-1">
                    by {activity.user.name}
                  </p>
                )}
              </div>
            </div>
          ))
        )}
      </div>

      {/* Load More Button */}
      {!isLoading && filteredActivities.length > 0 && (
        <div className="text-center pt-4">
          <Button variant="outline" size="sm">
            Load More Activity
          </Button>
        </div>
      )}
    </div>
  )
}

// Compact activity feed for smaller spaces
export function CompactActivity() {
  const { data: activities, isLoading } = useQuery({
    queryKey: ['recent-activity'],
    queryFn: fetchRecentActivity,
  })

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="flex items-center space-x-2">
            <Skeleton className="h-6 w-6 rounded-full" />
            <Skeleton className="h-3 flex-1" />
            <Skeleton className="h-3 w-16" />
          </div>
        ))}
      </div>
    )
  }

  const recentActivities = activities?.slice(0, 5) || []

  return (
    <div className="space-y-2">
      {recentActivities.map((activity) => (
        <div key={activity.id} className="flex items-center space-x-2 text-sm">
          <Icon 
            name={getActivityIcon(activity.type)} 
            className={`h-4 w-4 ${getActivityColor(activity.type)}`} 
          />
          <span className="flex-1 truncate text-gray-900 dark:text-white">
            {activity.action}
          </span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {formatDistanceToNow(new Date(activity.timestamp), { addSuffix: true })}
          </span>
        </div>
      ))}
    </div>
  )
}
