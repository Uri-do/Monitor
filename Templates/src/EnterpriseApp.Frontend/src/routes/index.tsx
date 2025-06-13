import { Routes, Route, Navigate } from 'react-router-dom'
import { Suspense, lazy } from 'react'

import { Layout } from '@/components/layout/Layout'
import { LoadingSpinner } from '@/components/ui/LoadingSpinner'
import { ProtectedRoute } from '@/components/auth/ProtectedRoute'
import { NotFoundPage } from '@/pages/NotFoundPage'

// Lazy load pages for code splitting
const DashboardPage = lazy(() => import('@/pages/DashboardPage'))
const DomainEntitiesPage = lazy(() => import('@/pages/DomainEntitiesPage'))
const DomainEntityDetailPage = lazy(() => import('@/pages/DomainEntityDetailPage'))
const DomainEntityCreatePage = lazy(() => import('@/pages/DomainEntityCreatePage'))
const DomainEntityEditPage = lazy(() => import('@/pages/DomainEntityEditPage'))
const StatisticsPage = lazy(() => import('@/pages/StatisticsPage'))
const SettingsPage = lazy(() => import('@/pages/SettingsPage'))
const ProfilePage = lazy(() => import('@/pages/ProfilePage'))

<!--#if (enableAuth)-->
const LoginPage = lazy(() => import('@/pages/auth/LoginPage'))
const RegisterPage = lazy(() => import('@/pages/auth/RegisterPage'))
const ForgotPasswordPage = lazy(() => import('@/pages/auth/ForgotPasswordPage'))
const ResetPasswordPage = lazy(() => import('@/pages/auth/ResetPasswordPage'))
<!--#endif-->

// Admin pages
const AdminPage = lazy(() => import('@/pages/admin/AdminPage'))
const UsersPage = lazy(() => import('@/pages/admin/UsersPage'))
const RolesPage = lazy(() => import('@/pages/admin/RolesPage'))
const AuditLogsPage = lazy(() => import('@/pages/admin/AuditLogsPage'))
const SystemHealthPage = lazy(() => import('@/pages/admin/SystemHealthPage'))

// Worker pages
const WorkerPage = lazy(() => import('@/pages/worker/WorkerPage'))
const JobsPage = lazy(() => import('@/pages/worker/JobsPage'))
const SchedulesPage = lazy(() => import('@/pages/worker/SchedulesPage'))

// Loading component for suspense
const PageLoader = () => (
  <div className="flex items-center justify-center min-h-[400px]">
    <LoadingSpinner size="lg" />
  </div>
)

export function AppRoutes() {
  return (
    <Routes>
<!--#if (enableAuth)-->
      {/* Public routes */}
      <Route path="/login" element={
        <Suspense fallback={<PageLoader />}>
          <LoginPage />
        </Suspense>
      } />
      <Route path="/register" element={
        <Suspense fallback={<PageLoader />}>
          <RegisterPage />
        </Suspense>
      } />
      <Route path="/forgot-password" element={
        <Suspense fallback={<PageLoader />}>
          <ForgotPasswordPage />
        </Suspense>
      } />
      <Route path="/reset-password" element={
        <Suspense fallback={<PageLoader />}>
          <ResetPasswordPage />
        </Suspense>
      } />
<!--#endif-->

      {/* Protected routes with layout */}
      <Route path="/" element={
        <ProtectedRoute>
          <Layout />
        </ProtectedRoute>
      }>
        {/* Dashboard */}
        <Route index element={
          <Suspense fallback={<PageLoader />}>
            <DashboardPage />
          </Suspense>
        } />

        {/* Domain Entities */}
        <Route path="domain-entities" element={
          <Suspense fallback={<PageLoader />}>
            <DomainEntitiesPage />
          </Suspense>
        } />
        <Route path="domain-entities/create" element={
          <ProtectedRoute requiredPermission="DomainEntity:Create">
            <Suspense fallback={<PageLoader />}>
              <DomainEntityCreatePage />
            </Suspense>
          </ProtectedRoute>
        } />
        <Route path="domain-entities/:id" element={
          <Suspense fallback={<PageLoader />}>
            <DomainEntityDetailPage />
          </Suspense>
        } />
        <Route path="domain-entities/:id/edit" element={
          <ProtectedRoute requiredPermission="DomainEntity:Update">
            <Suspense fallback={<PageLoader />}>
              <DomainEntityEditPage />
            </Suspense>
          </ProtectedRoute>
        } />

        {/* Statistics */}
        <Route path="statistics" element={
          <Suspense fallback={<PageLoader />}>
            <StatisticsPage />
          </Suspense>
        } />

        {/* Worker Management */}
        <Route path="worker" element={
          <ProtectedRoute requiredRole="Admin">
            <Suspense fallback={<PageLoader />}>
              <WorkerPage />
            </Suspense>
          </ProtectedRoute>
        } />
        <Route path="worker/jobs" element={
          <ProtectedRoute requiredRole="Admin">
            <Suspense fallback={<PageLoader />}>
              <JobsPage />
            </Suspense>
          </ProtectedRoute>
        } />
        <Route path="worker/schedules" element={
          <ProtectedRoute requiredRole="Admin">
            <Suspense fallback={<PageLoader />}>
              <SchedulesPage />
            </Suspense>
          </ProtectedRoute>
        } />

        {/* Administration */}
        <Route path="admin" element={
          <ProtectedRoute requiredRole="Admin">
            <Suspense fallback={<PageLoader />}>
              <AdminPage />
            </Suspense>
          </ProtectedRoute>
        } />
        <Route path="admin/users" element={
          <ProtectedRoute requiredRole="Admin">
            <Suspense fallback={<PageLoader />}>
              <UsersPage />
            </Suspense>
          </ProtectedRoute>
        } />
        <Route path="admin/roles" element={
          <ProtectedRoute requiredRole="Admin">
            <Suspense fallback={<PageLoader />}>
              <RolesPage />
            </Suspense>
          </ProtectedRoute>
        } />
        <Route path="admin/audit-logs" element={
          <ProtectedRoute requiredRole="Admin">
            <Suspense fallback={<PageLoader />}>
              <AuditLogsPage />
            </Suspense>
          </ProtectedRoute>
        } />
        <Route path="admin/system-health" element={
          <ProtectedRoute requiredRole="Admin">
            <Suspense fallback={<PageLoader />}>
              <SystemHealthPage />
            </Suspense>
          </ProtectedRoute>
        } />

        {/* User Profile & Settings */}
        <Route path="profile" element={
          <Suspense fallback={<PageLoader />}>
            <ProfilePage />
          </Suspense>
        } />
        <Route path="settings" element={
          <Suspense fallback={<PageLoader />}>
            <SettingsPage />
          </Suspense>
        } />

        {/* Catch all for 404 within layout */}
        <Route path="*" element={<NotFoundPage />} />
      </Route>

      {/* Catch all for 404 outside layout */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

// Route configuration for navigation
export const routes = {
  dashboard: '/',
  domainEntities: '/domain-entities',
  domainEntityCreate: '/domain-entities/create',
  domainEntityDetail: (id: string | number) => `/domain-entities/${id}`,
  domainEntityEdit: (id: string | number) => `/domain-entities/${id}/edit`,
  statistics: '/statistics',
  worker: '/worker',
  workerJobs: '/worker/jobs',
  workerSchedules: '/worker/schedules',
  admin: '/admin',
  adminUsers: '/admin/users',
  adminRoles: '/admin/roles',
  adminAuditLogs: '/admin/audit-logs',
  adminSystemHealth: '/admin/system-health',
  profile: '/profile',
  settings: '/settings',
<!--#if (enableAuth)-->
  login: '/login',
  register: '/register',
  forgotPassword: '/forgot-password',
  resetPassword: '/reset-password',
<!--#endif-->
} as const

// Navigation items for sidebar
export const navigationItems = [
  {
    name: 'Dashboard',
    href: routes.dashboard,
    icon: 'LayoutDashboard',
    description: 'Overview and key metrics',
  },
  {
    name: 'Domain Entities',
    href: routes.domainEntities,
    icon: 'Database',
    description: 'Manage domain entities',
    children: [
      {
        name: 'All Entities',
        href: routes.domainEntities,
        icon: 'List',
      },
      {
        name: 'Create New',
        href: routes.domainEntityCreate,
        icon: 'Plus',
        requiredPermission: 'DomainEntity:Create',
      },
    ],
  },
  {
    name: 'Statistics',
    href: routes.statistics,
    icon: 'BarChart3',
    description: 'Analytics and reports',
  },
  {
    name: 'Worker',
    href: routes.worker,
    icon: 'Cog',
    description: 'Background job management',
    requiredRole: 'Admin',
    children: [
      {
        name: 'Overview',
        href: routes.worker,
        icon: 'Activity',
      },
      {
        name: 'Jobs',
        href: routes.workerJobs,
        icon: 'Play',
      },
      {
        name: 'Schedules',
        href: routes.workerSchedules,
        icon: 'Calendar',
      },
    ],
  },
  {
    name: 'Administration',
    href: routes.admin,
    icon: 'Shield',
    description: 'System administration',
    requiredRole: 'Admin',
    children: [
      {
        name: 'Overview',
        href: routes.admin,
        icon: 'Settings',
      },
      {
        name: 'Users',
        href: routes.adminUsers,
        icon: 'Users',
      },
      {
        name: 'Roles',
        href: routes.adminRoles,
        icon: 'UserCheck',
      },
      {
        name: 'Audit Logs',
        href: routes.adminAuditLogs,
        icon: 'FileText',
      },
      {
        name: 'System Health',
        href: routes.adminSystemHealth,
        icon: 'Heart',
      },
    ],
  },
] as const

// Breadcrumb configuration
export const getBreadcrumbs = (pathname: string) => {
  const segments = pathname.split('/').filter(Boolean)
  const breadcrumbs = [{ name: 'Home', href: routes.dashboard }]

  if (segments.length === 0) return breadcrumbs

  // Build breadcrumbs based on path segments
  let currentPath = ''
  for (let i = 0; i < segments.length; i++) {
    currentPath += `/${segments[i]}`
    
    switch (segments[i]) {
      case 'domain-entities':
        breadcrumbs.push({ name: 'Domain Entities', href: routes.domainEntities })
        break
      case 'create':
        if (segments[i - 1] === 'domain-entities') {
          breadcrumbs.push({ name: 'Create', href: routes.domainEntityCreate })
        }
        break
      case 'edit':
        if (segments[i - 1] && segments[i - 2] === 'domain-entities') {
          breadcrumbs.push({ name: 'Edit', href: currentPath })
        }
        break
      case 'statistics':
        breadcrumbs.push({ name: 'Statistics', href: routes.statistics })
        break
      case 'worker':
        breadcrumbs.push({ name: 'Worker', href: routes.worker })
        break
      case 'jobs':
        if (segments[i - 1] === 'worker') {
          breadcrumbs.push({ name: 'Jobs', href: routes.workerJobs })
        }
        break
      case 'schedules':
        if (segments[i - 1] === 'worker') {
          breadcrumbs.push({ name: 'Schedules', href: routes.workerSchedules })
        }
        break
      case 'admin':
        breadcrumbs.push({ name: 'Administration', href: routes.admin })
        break
      case 'users':
        if (segments[i - 1] === 'admin') {
          breadcrumbs.push({ name: 'Users', href: routes.adminUsers })
        }
        break
      case 'roles':
        if (segments[i - 1] === 'admin') {
          breadcrumbs.push({ name: 'Roles', href: routes.adminRoles })
        }
        break
      case 'audit-logs':
        if (segments[i - 1] === 'admin') {
          breadcrumbs.push({ name: 'Audit Logs', href: routes.adminAuditLogs })
        }
        break
      case 'system-health':
        if (segments[i - 1] === 'admin') {
          breadcrumbs.push({ name: 'System Health', href: routes.adminSystemHealth })
        }
        break
      case 'profile':
        breadcrumbs.push({ name: 'Profile', href: routes.profile })
        break
      case 'settings':
        breadcrumbs.push({ name: 'Settings', href: routes.settings })
        break
      default:
        // Handle dynamic segments (like IDs)
        if (segments[i - 1] === 'domain-entities' && !isNaN(Number(segments[i]))) {
          breadcrumbs.push({ name: `Entity ${segments[i]}`, href: currentPath })
        }
        break
    }
  }

  return breadcrumbs
}
