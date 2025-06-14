import React from 'react';
import { Navigate } from 'react-router-dom';

// Lazy loaded components for code splitting
const Dashboard = React.lazy(() => import('@/pages/Dashboard/Dashboard'));

// Indicator Management
const IndicatorList = React.lazy(() => import('@/pages/Indicator/IndicatorList'));
const IndicatorDetail = React.lazy(() => import('@/pages/Indicator/IndicatorDetail'));
const IndicatorCreate = React.lazy(() => import('@/pages/Indicator/IndicatorCreate'));
const IndicatorEdit = React.lazy(() => import('@/pages/Indicator/IndicatorEdit'));

// Scheduler Management
const SchedulerList = React.lazy(() => import('@/pages/Scheduler/SchedulerList'));
const SchedulerDetail = React.lazy(() => import('@/pages/Scheduler/SchedulerDetail'));
const SchedulerEdit = React.lazy(() => import('@/pages/Scheduler/SchedulerEdit'));
const SchedulerCreate = React.lazy(() => import('@/pages/Scheduler/SchedulerCreate'));

// Contact Management
const ContactList = React.lazy(() => import('@/pages/Contact/ContactList'));
const ContactDetail = React.lazy(() => import('@/pages/Contact/ContactDetail'));
const ContactCreate = React.lazy(() => import('@/pages/Contact/ContactCreate'));

// Alert Management
const AlertList = React.lazy(() => import('@/pages/Alert/AlertList'));
const AlertDetail = React.lazy(() => import('@/pages/Alert/AlertDetail'));
const AlertEdit = React.lazy(() => import('@/pages/Alert/AlertEdit'));

// Other Pages
const Analytics = React.lazy(() => import('@/pages/Analytics/Analytics'));
const Statistics = React.lazy(() => import('@/pages/Statistics/StatisticsPage'));
const Settings = React.lazy(() => import('@/pages/Settings/Settings'));
const UserProfile = React.lazy(() => import('@/pages/User/UserProfile'));
const UserManagement = React.lazy(() => import('@/pages/Users/UserManagement'));
const RoleManagement = React.lazy(() => import('@/pages/Admin/RoleManagement'));
const SystemSettings = React.lazy(() => import('@/pages/Admin/SystemSettings'));
const Administration = React.lazy(() => import('@/pages/Admin/Administration'));
const ExecutionHistoryList = React.lazy(() => import('@/pages/ExecutionHistory/ExecutionHistoryList'));
const ExecutionHistoryDetail = React.lazy(() => import('@/pages/ExecutionHistory/ExecutionHistoryDetail'));
const WorkerManagement = React.lazy(() => import('@/pages/Worker/WorkerManagement'));

// Route configuration interface
export interface RouteConfig {
  path: string;
  element: React.ComponentType<any> | React.ReactElement;
  requiredPermissions?: string[];
  requiredRoles?: string[];
  children?: RouteConfig[];
}

// Main route configuration
export const routeConfig: RouteConfig[] = [
  {
    path: '/',
    element: <Navigate to="/dashboard" replace />,
  },
  {
    path: '/dashboard',
    element: Dashboard,
  },
  
  // Indicator Management
  {
    path: '/indicators',
    element: IndicatorList,
  },
  {
    path: '/indicators/create',
    element: IndicatorCreate,
  },
  {
    path: '/indicators/:id',
    element: IndicatorDetail,
  },
  {
    path: '/indicators/:id/edit',
    element: IndicatorEdit,
  },

  // Scheduler Management
  {
    path: '/schedulers',
    element: SchedulerList,
  },
  {
    path: '/schedulers/create',
    element: SchedulerCreate,
  },
  {
    path: '/schedulers/:id',
    element: SchedulerDetail,
  },
  {
    path: '/schedulers/:id/edit',
    element: SchedulerEdit,
  },

  // Contact Management
  {
    path: '/contacts',
    element: ContactList,
  },
  {
    path: '/contacts/create',
    element: ContactCreate,
  },
  {
    path: '/contacts/:id',
    element: ContactDetail,
  },
  {
    path: '/contacts/:id/edit',
    element: ContactCreate, // Reusing create component for edit
  },

  // Alert Management
  {
    path: '/alerts',
    element: AlertList,
  },
  {
    path: '/alerts/:id',
    element: AlertDetail,
  },
  {
    path: '/alerts/:id/edit',
    element: AlertEdit,
  },

  // Analytics & Statistics
  {
    path: '/analytics',
    element: Analytics,
  },
  {
    path: '/statistics',
    element: Statistics,
  },

  // Execution History
  {
    path: '/execution-history',
    element: ExecutionHistoryList,
  },
  {
    path: '/execution-history/:id',
    element: ExecutionHistoryDetail,
  },

  // Worker Management
  {
    path: '/worker',
    element: WorkerManagement,
  },

  // User Profile
  {
    path: '/profile',
    element: UserProfile,
  },

  // Administration
  {
    path: '/admin',
    element: Administration,
  },
  {
    path: '/admin/security',
    element: Administration,
  },
  {
    path: '/admin/api-keys',
    element: Administration,
  },
  {
    path: '/admin/audit',
    element: Administration,
  },
  {
    path: '/admin/users',
    element: UserManagement,
    requiredRoles: ['Admin'],
  },
  {
    path: '/admin/roles',
    element: RoleManagement,
    requiredRoles: ['Admin'],
  },
  {
    path: '/admin/settings',
    element: SystemSettings,
    requiredRoles: ['Admin'],
  },

  // Settings
  {
    path: '/settings',
    element: Settings,
  },
];

// Legacy route redirects
export const legacyRedirects: RouteConfig[] = [
  {
    path: '/kpis',
    element: <Navigate to="/indicators" replace />,
  },
  {
    path: '/kpis/create',
    element: <Navigate to="/indicators/create" replace />,
  },
  {
    path: '/kpis/:id',
    element: <Navigate to="/indicators/:id" replace />,
  },
  {
    path: '/kpis/:id/edit',
    element: <Navigate to="/indicators/:id/edit" replace />,
  },
  {
    path: '/administration',
    element: <Navigate to="/admin" replace />,
  },
];

// Public routes (no authentication required)
export const publicRoutes: RouteConfig[] = [
  {
    path: '/login',
    element: React.lazy(() => import('@/pages/Auth/Login')),
  },
  {
    path: '/register',
    element: React.lazy(() => import('@/pages/Auth/Register')),
  },
];
