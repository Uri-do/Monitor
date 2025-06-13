import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { formatDistanceToNow } from 'date-fns'
import { Icon, IconName } from '@/components/ui/Icon'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Progress } from '@/components/ui/Progress'
import { LoadingSpinner, Skeleton } from '@/components/ui/LoadingSpinner'
import { Alert } from '@/components/ui/Alert'

interface WorkerInfo {
  id: string
  name: string
  status: 'running' | 'stopped' | 'error' | 'starting' | 'stopping'
  uptime?: string
  lastHeartbeat?: string
  jobsProcessed: number
  jobsQueued: number
  jobsFailed: number
  cpuUsage: number
  memoryUsage: number
  errorMessage?: string
}

interface JobInfo {
  id: string
  name: string
  status: 'running' | 'queued' | 'completed' | 'failed'
  progress?: number
  startedAt?: string
  completedAt?: string
  duration?: number
  errorMessage?: string
}

// Mock API service - replace with actual API calls
const fetchWorkerStatus = async (): Promise<{
  workers: WorkerInfo[]
  recentJobs: JobInfo[]
  totalJobsToday: number
  averageJobTime: number
}> => {
  // Simulate API delay
  await new Promise(resolve => setTimeout(resolve, 800))
  
  return {
    workers: [
      {
        id: 'worker-1',
        name: 'Data Processing Worker',
        status: 'running',
        uptime: '2 days, 14 hours',
        lastHeartbeat: new Date(Date.now() - 30000).toISOString(),
        jobsProcessed: 1247,
        jobsQueued: 5,
        jobsFailed: 3,
        cpuUsage: 23.5,
        memoryUsage: 45.2,
      },
      {
        id: 'worker-2',
        name: 'Email Service Worker',
        status: 'running',
        uptime: '1 day, 8 hours',
        lastHeartbeat: new Date(Date.now() - 15000).toISOString(),
        jobsProcessed: 892,
        jobsQueued: 2,
        jobsFailed: 1,
        cpuUsage: 12.1,
        memoryUsage: 28.7,
      },
      {
        id: 'worker-3',
        name: 'Report Generator',
        status: 'error',
        uptime: '0 minutes',
        lastHeartbeat: new Date(Date.now() - 300000).toISOString(),
        jobsProcessed: 0,
        jobsQueued: 8,
        jobsFailed: 2,
        cpuUsage: 0,
        memoryUsage: 0,
        errorMessage: 'Connection to database failed',
      },
    ],
    recentJobs: [
      {
        id: 'job-1',
        name: 'Daily Report Generation',
        status: 'running',
        progress: 65,
        startedAt: new Date(Date.now() - 300000).toISOString(),
      },
      {
        id: 'job-2',
        name: 'Data Export',
        status: 'completed',
        startedAt: new Date(Date.now() - 600000).toISOString(),
        completedAt: new Date(Date.now() - 120000).toISOString(),
        duration: 480000,
      },
      {
        id: 'job-3',
        name: 'Email Notifications',
        status: 'queued',
      },
      {
        id: 'job-4',
        name: 'Database Cleanup',
        status: 'failed',
        startedAt: new Date(Date.now() - 900000).toISOString(),
        errorMessage: 'Insufficient permissions',
      },
    ],
    totalJobsToday: 156,
    averageJobTime: 2.3,
  }
}

export function WorkerStatus() {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['worker-status'],
    queryFn: fetchWorkerStatus,
    refetchInterval: 10000, // Refetch every 10 seconds
  })

  const getStatusIcon = (status: WorkerInfo['status']): IconName => {
    switch (status) {
      case 'running': return 'play'
      case 'stopped': return 'stop'
      case 'error': return 'x-circle'
      case 'starting': return 'refresh'
      case 'stopping': return 'pause'
      default: return 'help-circle'
    }
  }

  const getStatusColor = (status: WorkerInfo['status']) => {
    switch (status) {
      case 'running': return 'text-green-600 dark:text-green-400'
      case 'stopped': return 'text-gray-600 dark:text-gray-400'
      case 'error': return 'text-red-600 dark:text-red-400'
      case 'starting': return 'text-blue-600 dark:text-blue-400'
      case 'stopping': return 'text-yellow-600 dark:text-yellow-400'
      default: return 'text-gray-600 dark:text-gray-400'
    }
  }

  const getJobStatusBadge = (status: JobInfo['status']) => {
    const variants = {
      running: 'default' as const,
      queued: 'secondary' as const,
      completed: 'success' as const,
      failed: 'error' as const,
    }
    return <Badge variant={variants[status]}>{status}</Badge>
  }

  if (error) {
    return (
      <Alert
        variant="error"
        title="Failed to load worker status"
        description="Unable to fetch worker information."
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
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="space-y-2">
            <div className="flex items-center justify-between">
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-6 w-16" />
            </div>
            <Skeleton className="h-2 w-full" />
            <div className="flex justify-between text-xs">
              <Skeleton className="h-3 w-20" />
              <Skeleton className="h-3 w-16" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  const runningWorkers = data?.workers.filter(w => w.status === 'running').length || 0
  const totalWorkers = data?.workers.length || 0

  return (
    <div className="space-y-6">
      {/* Worker Overview */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-2">
          <Icon name="cog" className="h-5 w-5 text-gray-600 dark:text-gray-400" />
          <span className="font-medium text-gray-900 dark:text-white">
            Workers ({runningWorkers}/{totalWorkers} running)
          </span>
        </div>
        <Button
          variant="ghost"
          size="sm"
          leftIcon="refresh"
          onClick={() => refetch()}
        >
          Refresh
        </Button>
      </div>

      {/* Workers List */}
      <div className="space-y-3">
        {data?.workers.map((worker) => (
          <div
            key={worker.id}
            className="p-3 rounded-lg border border-gray-200 dark:border-gray-700 space-y-3"
          >
            {/* Worker Header */}
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <Icon 
                  name={getStatusIcon(worker.status)} 
                  className={`h-4 w-4 ${getStatusColor(worker.status)} ${worker.status === 'starting' ? 'animate-spin' : ''}`}
                />
                <span className="text-sm font-medium text-gray-900 dark:text-white">
                  {worker.name}
                </span>
              </div>
              <Badge 
                variant={
                  worker.status === 'running' ? 'success' :
                  worker.status === 'error' ? 'error' : 'secondary'
                }
              >
                {worker.status}
              </Badge>
            </div>

            {/* Worker Stats */}
            <div className="grid grid-cols-2 gap-4 text-xs">
              <div>
                <span className="text-gray-600 dark:text-gray-400">Jobs Processed:</span>
                <span className="ml-1 font-medium text-gray-900 dark:text-white">
                  {worker.jobsProcessed}
                </span>
              </div>
              <div>
                <span className="text-gray-600 dark:text-gray-400">Queued:</span>
                <span className="ml-1 font-medium text-gray-900 dark:text-white">
                  {worker.jobsQueued}
                </span>
              </div>
              <div>
                <span className="text-gray-600 dark:text-gray-400">Failed:</span>
                <span className="ml-1 font-medium text-gray-900 dark:text-white">
                  {worker.jobsFailed}
                </span>
              </div>
              <div>
                <span className="text-gray-600 dark:text-gray-400">Uptime:</span>
                <span className="ml-1 font-medium text-gray-900 dark:text-white">
                  {worker.uptime}
                </span>
              </div>
            </div>

            {/* Resource Usage */}
            {worker.status === 'running' && (
              <div className="space-y-2">
                <div className="flex items-center justify-between text-xs">
                  <span className="text-gray-600 dark:text-gray-400">CPU Usage</span>
                  <span className="text-gray-900 dark:text-white">{worker.cpuUsage}%</span>
                </div>
                <Progress 
                  value={worker.cpuUsage} 
                  variant={worker.cpuUsage > 80 ? 'error' : worker.cpuUsage > 60 ? 'warning' : 'success'}
                  size="sm"
                />

                <div className="flex items-center justify-between text-xs">
                  <span className="text-gray-600 dark:text-gray-400">Memory Usage</span>
                  <span className="text-gray-900 dark:text-white">{worker.memoryUsage}%</span>
                </div>
                <Progress 
                  value={worker.memoryUsage} 
                  variant={worker.memoryUsage > 80 ? 'error' : worker.memoryUsage > 60 ? 'warning' : 'success'}
                  size="sm"
                />
              </div>
            )}

            {/* Error Message */}
            {worker.errorMessage && (
              <Alert
                variant="error"
                description={worker.errorMessage}
                className="text-xs"
              />
            )}

            {/* Last Heartbeat */}
            {worker.lastHeartbeat && (
              <div className="text-xs text-gray-500 dark:text-gray-400">
                Last heartbeat: {formatDistanceToNow(new Date(worker.lastHeartbeat), { addSuffix: true })}
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Recent Jobs */}
      <div>
        <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">
          Recent Jobs
        </h4>
        <div className="space-y-2">
          {data?.recentJobs.map((job) => (
            <div
              key={job.id}
              className="flex items-center justify-between p-2 rounded border border-gray-200 dark:border-gray-700"
            >
              <div className="flex-1 min-w-0">
                <div className="flex items-center space-x-2">
                  <span className="text-sm font-medium text-gray-900 dark:text-white truncate">
                    {job.name}
                  </span>
                  {getJobStatusBadge(job.status)}
                </div>
                
                {job.progress !== undefined && (
                  <div className="mt-1">
                    <Progress value={job.progress} size="sm" />
                  </div>
                )}
                
                {job.errorMessage && (
                  <p className="text-xs text-red-600 dark:text-red-400 mt-1">
                    {job.errorMessage}
                  </p>
                )}
                
                <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                  {job.startedAt && (
                    <span>
                      Started {formatDistanceToNow(new Date(job.startedAt), { addSuffix: true })}
                    </span>
                  )}
                  {job.duration && (
                    <span className="ml-2">
                      Duration: {Math.round(job.duration / 1000)}s
                    </span>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Summary Stats */}
      <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div className="text-center">
            <div className="text-lg font-bold text-gray-900 dark:text-white">
              {data?.totalJobsToday}
            </div>
            <div className="text-gray-600 dark:text-gray-400">
              Jobs Today
            </div>
          </div>
          <div className="text-center">
            <div className="text-lg font-bold text-gray-900 dark:text-white">
              {data?.averageJobTime}s
            </div>
            <div className="text-gray-600 dark:text-gray-400">
              Avg. Job Time
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
