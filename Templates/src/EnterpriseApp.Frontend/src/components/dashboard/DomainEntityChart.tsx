import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts'
import { Button } from '@/components/ui/Button'
import { Select } from '@/components/ui/Select'
import { Icon } from '@/components/ui/Icon'
import { LoadingSpinner, Skeleton } from '@/components/ui/LoadingSpinner'
import { Alert } from '@/components/ui/Alert'

interface ChartData {
  date: string
  created: number
  updated: number
  deleted: number
  total: number
}

interface EntityTypeData {
  name: string
  count: number
  color: string
}

// Mock API service - replace with actual API calls
const fetchChartData = async (timeRange: string): Promise<{
  timeline: ChartData[]
  entityTypes: EntityTypeData[]
}> => {
  // Simulate API delay
  await new Promise(resolve => setTimeout(resolve, 1000))
  
  // Generate mock timeline data
  const days = timeRange === '7d' ? 7 : timeRange === '30d' ? 30 : 90
  const timeline: ChartData[] = []
  
  for (let i = days - 1; i >= 0; i--) {
    const date = new Date()
    date.setDate(date.getDate() - i)
    
    timeline.push({
      date: date.toISOString().split('T')[0],
      created: Math.floor(Math.random() * 20) + 5,
      updated: Math.floor(Math.random() * 30) + 10,
      deleted: Math.floor(Math.random() * 5) + 1,
      total: Math.floor(Math.random() * 100) + 1000 + i * 10,
    })
  }

  // Mock entity types data
  const entityTypes: EntityTypeData[] = [
    { name: 'Customer', count: 342, color: '#3B82F6' },
    { name: 'Order', count: 256, color: '#10B981' },
    { name: 'Product', count: 189, color: '#F59E0B' },
    { name: 'Invoice', count: 167, color: '#EF4444' },
    { name: 'Report', count: 98, color: '#8B5CF6' },
    { name: 'Other', count: 195, color: '#6B7280' },
  ]

  return { timeline, entityTypes }
}

export function DomainEntityChart() {
  const [timeRange, setTimeRange] = useState('30d')
  const [chartType, setChartType] = useState<'line' | 'area' | 'bar'>('area')

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['domain-entity-chart', timeRange],
    queryFn: () => fetchChartData(timeRange),
    refetchInterval: 60000, // Refetch every minute
  })

  const timeRangeOptions = [
    { value: '7d', label: 'Last 7 days' },
    { value: '30d', label: 'Last 30 days' },
    { value: '90d', label: 'Last 90 days' },
  ]

  const chartTypeOptions = [
    { value: 'line', label: 'Line Chart' },
    { value: 'area', label: 'Area Chart' },
    { value: 'bar', label: 'Bar Chart' },
  ]

  if (error) {
    return (
      <Alert
        variant="error"
        title="Failed to load chart data"
        description="Unable to fetch domain entity statistics."
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
      {/* Chart Controls */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="flex items-center space-x-4">
          <Select
            value={timeRange}
            onChange={setTimeRange}
            options={timeRangeOptions}
            size="sm"
          />
          <Select
            value={chartType}
            onChange={(value) => setChartType(value as any)}
            options={chartTypeOptions}
            size="sm"
          />
        </div>
        
        <div className="flex items-center space-x-2">
          <Button
            variant="ghost"
            size="sm"
            leftIcon="refresh"
            onClick={() => refetch()}
          >
            Refresh
          </Button>
          <Button
            variant="ghost"
            size="sm"
            leftIcon="download"
          >
            Export
          </Button>
        </div>
      </div>

      {/* Main Chart */}
      <div className="h-80">
        {isLoading ? (
          <div className="flex items-center justify-center h-full">
            <LoadingSpinner size="lg" />
          </div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            {chartType === 'line' && (
              <LineChart data={data?.timeline}>
                <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
                <XAxis 
                  dataKey="date" 
                  tick={{ fontSize: 12 }}
                  tickFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip 
                  labelFormatter={(value) => new Date(value).toLocaleDateString()}
                  contentStyle={{
                    backgroundColor: 'var(--background)',
                    border: '1px solid var(--border)',
                    borderRadius: '6px',
                  }}
                />
                <Legend />
                <Line 
                  type="monotone" 
                  dataKey="created" 
                  stroke="#10B981" 
                  strokeWidth={2}
                  name="Created"
                  dot={{ fill: '#10B981', strokeWidth: 2, r: 4 }}
                />
                <Line 
                  type="monotone" 
                  dataKey="updated" 
                  stroke="#3B82F6" 
                  strokeWidth={2}
                  name="Updated"
                  dot={{ fill: '#3B82F6', strokeWidth: 2, r: 4 }}
                />
                <Line 
                  type="monotone" 
                  dataKey="deleted" 
                  stroke="#EF4444" 
                  strokeWidth={2}
                  name="Deleted"
                  dot={{ fill: '#EF4444', strokeWidth: 2, r: 4 }}
                />
              </LineChart>
            )}

            {chartType === 'area' && (
              <AreaChart data={data?.timeline}>
                <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
                <XAxis 
                  dataKey="date" 
                  tick={{ fontSize: 12 }}
                  tickFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip 
                  labelFormatter={(value) => new Date(value).toLocaleDateString()}
                  contentStyle={{
                    backgroundColor: 'var(--background)',
                    border: '1px solid var(--border)',
                    borderRadius: '6px',
                  }}
                />
                <Legend />
                <Area 
                  type="monotone" 
                  dataKey="created" 
                  stackId="1"
                  stroke="#10B981" 
                  fill="#10B981"
                  fillOpacity={0.6}
                  name="Created"
                />
                <Area 
                  type="monotone" 
                  dataKey="updated" 
                  stackId="1"
                  stroke="#3B82F6" 
                  fill="#3B82F6"
                  fillOpacity={0.6}
                  name="Updated"
                />
                <Area 
                  type="monotone" 
                  dataKey="deleted" 
                  stackId="1"
                  stroke="#EF4444" 
                  fill="#EF4444"
                  fillOpacity={0.6}
                  name="Deleted"
                />
              </AreaChart>
            )}

            {chartType === 'bar' && (
              <BarChart data={data?.timeline}>
                <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
                <XAxis 
                  dataKey="date" 
                  tick={{ fontSize: 12 }}
                  tickFormatter={(value) => new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip 
                  labelFormatter={(value) => new Date(value).toLocaleDateString()}
                  contentStyle={{
                    backgroundColor: 'var(--background)',
                    border: '1px solid var(--border)',
                    borderRadius: '6px',
                  }}
                />
                <Legend />
                <Bar dataKey="created" fill="#10B981" name="Created" />
                <Bar dataKey="updated" fill="#3B82F6" name="Updated" />
                <Bar dataKey="deleted" fill="#EF4444" name="Deleted" />
              </BarChart>
            )}
          </ResponsiveContainer>
        )}
      </div>

      {/* Entity Types Distribution */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-8">
        {/* Pie Chart */}
        <div>
          <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-4">
            Entity Types Distribution
          </h4>
          <div className="h-64">
            {isLoading ? (
              <Skeleton className="h-full w-full rounded" />
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={data?.entityTypes}
                    cx="50%"
                    cy="50%"
                    outerRadius={80}
                    dataKey="count"
                    label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                  >
                    {data?.entityTypes.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        {/* Entity Types List */}
        <div>
          <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-4">
            Entity Counts
          </h4>
          <div className="space-y-3">
            {isLoading ? (
              Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <Skeleton className="h-4 w-4 rounded" />
                    <Skeleton className="h-4 w-20" />
                  </div>
                  <Skeleton className="h-4 w-12" />
                </div>
              ))
            ) : (
              data?.entityTypes.map((type) => (
                <div key={type.name} className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <div 
                      className="h-4 w-4 rounded"
                      style={{ backgroundColor: type.color }}
                    />
                    <span className="text-sm text-gray-900 dark:text-white">
                      {type.name}
                    </span>
                  </div>
                  <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    {type.count.toLocaleString()}
                  </span>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
