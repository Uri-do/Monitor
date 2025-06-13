import { Helmet } from 'react-helmet-async'
import { useAuth } from '@/hooks/useAuth'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/Card'
import { StatsCard } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Badge } from '@/components/ui/Badge'
import { Icon } from '@/components/ui/Icon'
import { Alert } from '@/components/ui/Alert'
import { DashboardStats } from '@/components/dashboard/DashboardStats'
import { RecentActivity } from '@/components/dashboard/RecentActivity'
import { QuickActions } from '@/components/dashboard/QuickActions'
import { SystemHealth } from '@/components/dashboard/SystemHealth'
import { DomainEntityChart } from '@/components/dashboard/DomainEntityChart'
import { WorkerStatus } from '@/components/dashboard/WorkerStatus'

export default function DashboardPage() {
  const { user, hasRole } = useAuth()

  return (
    <>
      <Helmet>
        <title>Dashboard - EnterpriseApp</title>
        <meta name="description" content="Enterprise Application Dashboard" />
      </Helmet>

      <div className="space-y-6">
        {/* Welcome Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
              Welcome back, {user?.firstName || user?.name || 'User'}!
            </h1>
            <p className="mt-2 text-gray-600 dark:text-gray-400">
              Here's what's happening with your application today.
            </p>
          </div>
          <div className="flex items-center space-x-3">
            <Badge variant="success" dot>
              System Healthy
            </Badge>
            <Button leftIcon="refresh" variant="outline" size="sm">
              Refresh
            </Button>
          </div>
        </div>

        {/* Email Verification Alert */}
        {user && !user.isEmailVerified && (
          <Alert
            variant="warning"
            title="Email Verification Required"
            description="Please verify your email address to access all features."
            actions={
              <Button size="sm" variant="outline">
                Resend Verification Email
              </Button>
            }
          />
        )}

        {/* Key Statistics */}
        <DashboardStats />

        {/* Main Content Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left Column - Charts and Analytics */}
          <div className="lg:col-span-2 space-y-6">
            {/* Domain Entity Overview */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Icon name="bar-chart" className="mr-2 h-5 w-5" />
                  Domain Entity Overview
                </CardTitle>
                <CardDescription>
                  Activity and trends for your domain entities
                </CardDescription>
              </CardHeader>
              <CardContent>
                <DomainEntityChart />
              </CardContent>
            </Card>

            {/* Recent Activity */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Icon name="activity" className="mr-2 h-5 w-5" />
                  Recent Activity
                </CardTitle>
                <CardDescription>
                  Latest actions and updates in your system
                </CardDescription>
              </CardHeader>
              <CardContent>
                <RecentActivity />
              </CardContent>
            </Card>
          </div>

          {/* Right Column - Quick Actions and Status */}
          <div className="space-y-6">
            {/* Quick Actions */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Icon name="zap" className="mr-2 h-5 w-5" />
                  Quick Actions
                </CardTitle>
                <CardDescription>
                  Common tasks and shortcuts
                </CardDescription>
              </CardHeader>
              <CardContent>
                <QuickActions />
              </CardContent>
            </Card>

            {/* System Health */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Icon name="heart" className="mr-2 h-5 w-5" />
                  System Health
                </CardTitle>
                <CardDescription>
                  Current system status and metrics
                </CardDescription>
              </CardHeader>
              <CardContent>
                <SystemHealth />
              </CardContent>
            </Card>

            {/* Worker Status - Admin Only */}
            {hasRole('Admin') && (
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center">
                    <Icon name="cog" className="mr-2 h-5 w-5" />
                    Worker Status
                  </CardTitle>
                  <CardDescription>
                    Background job processing status
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <WorkerStatus />
                </CardContent>
              </Card>
            )}

            {/* User Info Card */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Icon name="user" className="mr-2 h-5 w-5" />
                  Your Account
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center space-x-3">
                  <div className="h-10 w-10 rounded-full bg-brand-100 dark:bg-brand-900 flex items-center justify-center">
                    <span className="text-sm font-medium text-brand-600 dark:text-brand-400">
                      {user?.firstName?.charAt(0) || user?.name?.charAt(0) || 'U'}
                    </span>
                  </div>
                  <div>
                    <p className="text-sm font-medium text-gray-900 dark:text-white">
                      {user?.firstName && user?.lastName 
                        ? `${user.firstName} ${user.lastName}`
                        : user?.name || 'User'
                      }
                    </p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      {user?.email}
                    </p>
                  </div>
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-600 dark:text-gray-400">Role</span>
                    <Badge variant="outline">
                      {user?.roles?.[0] || 'User'}
                    </Badge>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-600 dark:text-gray-400">Status</span>
                    <Badge variant={user?.isEmailVerified ? 'success' : 'warning'}>
                      {user?.isEmailVerified ? 'Verified' : 'Unverified'}
                    </Badge>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-600 dark:text-gray-400">Last Login</span>
                    <span className="text-gray-900 dark:text-white text-xs">
                      {user?.lastLoginAt 
                        ? new Date(user.lastLoginAt).toLocaleDateString()
                        : 'Today'
                      }
                    </span>
                  </div>
                </div>

                <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
                  <Button variant="outline" size="sm" className="w-full">
                    <Icon name="settings" className="mr-2 h-4 w-4" />
                    Account Settings
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>

        {/* Bottom Section - Additional Widgets */}
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6">
          {/* Performance Metrics */}
          <StatsCard
            title="Response Time"
            value="245ms"
            description="Average API response time"
            icon={<Icon name="zap" className="h-4 w-4" />}
            trend={{ value: 12, direction: 'down', label: 'faster than last week' }}
          />

          <StatsCard
            title="Uptime"
            value="99.9%"
            description="System availability"
            icon={<Icon name="activity" className="h-4 w-4" />}
            trend={{ value: 0.1, direction: 'up', label: 'this month' }}
          />

          <StatsCard
            title="Storage Used"
            value="2.4 GB"
            description="of 10 GB available"
            icon={<Icon name="database" className="h-4 w-4" />}
            trend={{ value: 8, direction: 'up', label: 'this month' }}
          />

          <StatsCard
            title="Active Sessions"
            value="24"
            description="Current user sessions"
            icon={<Icon name="users" className="h-4 w-4" />}
            trend={{ value: 15, direction: 'up', label: 'from yesterday' }}
          />
        </div>
      </div>
    </>
  )
}
