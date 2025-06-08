import React, { Suspense, Component, ErrorInfo, ReactNode, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CssBaseline, Box, CircularProgress, Typography } from '@mui/material';
import { Toaster } from 'react-hot-toast';
import { useTheme } from '@mui/material/styles';

// Core Components (not lazy loaded for better initial performance)
import Layout from '@/components/Layout/Layout';
import ProtectedRoute from '@/components/Auth/ProtectedRoute';
import Login from '@/pages/Auth/Login';
import Register from '@/pages/Auth/Register';

// Lazy loaded components for code splitting
const Dashboard = React.lazy(() => import('@/pages/Dashboard/Dashboard'));
const KpiList = React.lazy(() => import('@/pages/KPI/KpiList'));
const KpiDetail = React.lazy(() => import('@/pages/KPI/KpiDetail'));
const KpiCreate = React.lazy(() => import('@/pages/KPI/KpiCreate'));
const ContactList = React.lazy(() => import('@/pages/Contact/ContactList'));
const ContactDetail = React.lazy(() => import('@/pages/Contact/ContactDetail'));
const ContactCreate = React.lazy(() => import('@/pages/Contact/ContactCreate'));
const AlertList = React.lazy(() => import('@/pages/Alert/AlertList'));
const AlertDetail = React.lazy(() => import('@/pages/Alert/AlertDetail'));
const Analytics = React.lazy(() => import('@/pages/Analytics/Analytics'));
const Settings = React.lazy(() => import('@/pages/Settings/Settings'));
const UserProfile = React.lazy(() => import('@/pages/User/UserProfile'));
const UserManagement = React.lazy(() => import('@/pages/Users/UserManagement'));
const RoleManagement = React.lazy(() => import('@/pages/Admin/RoleManagement'));
const AdminDashboard = React.lazy(() => import('@/pages/Admin/AdminDashboard'));
const SystemSettings = React.lazy(() => import('@/pages/Admin/SystemSettings'));
const Administration = React.lazy(() => import('@/pages/Administration/Administration'));
const ExecutionHistoryList = React.lazy(() => import('@/pages/ExecutionHistory/ExecutionHistoryList'));
const ExecutionHistoryDetail = React.lazy(() => import('@/pages/ExecutionHistory/ExecutionHistoryDetail'));
const WorkerManagement = React.lazy(() => import('@/components/Worker/WorkerManagement'));
const WorkerDebug = React.lazy(() => import('@/pages/Debug/WorkerDebug'));
const ComponentShowcase = React.lazy(() => import('@/pages/Demo/ComponentShowcase'));
const VisualizationShowcase = React.lazy(() => import('@/pages/Demo/VisualizationShowcase'));
const InteractiveShowcase = React.lazy(() => import('@/pages/Demo/InteractiveShowcase'));

// Auth Provider
import { AuthProvider } from '@/hooks/useAuth';

// Theme Provider
import { CustomThemeProvider } from '@/hooks/useTheme';

// Error Boundary for catching routing errors
interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

class ErrorBoundary extends Component<{ children: ReactNode }, ErrorBoundaryState> {
  constructor(props: { children: ReactNode }) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Route Error Boundary caught an error:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            minHeight: '100vh',
            gap: 2,
            p: 3,
          }}
        >
          <Typography variant="h5" color="error">
            Something went wrong
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {this.state.error?.message || 'An unexpected error occurred'}
          </Typography>
          <button onClick={() => window.location.reload()}>
            Reload Page
          </button>
        </Box>
      );
    }

    return this.props.children;
  }
}

// Loading fallback component for lazy loaded routes
const LoadingFallback: React.FC = () => {
  console.log('LoadingFallback rendered'); // Debug log
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '60vh',
        gap: 2,
        backgroundColor: 'background.default',
        width: '100%',
      }}
    >
      <CircularProgress size={40} />
      <Typography variant="body1" color="text.secondary">
        Loading page...
      </Typography>
    </Box>
  );
};

// Helper component to wrap lazy loaded routes
const LazyRoute: React.FC<{
  children: React.ReactNode;
  requiredPermissions?: string[];
  requiredRoles?: string[];
}> = ({ children, requiredPermissions, requiredRoles }) => {
  console.log('LazyRoute rendered with permissions:', requiredPermissions); // Debug log
  return (
    <ProtectedRoute requiredPermissions={requiredPermissions} requiredRoles={requiredRoles}>
      <Layout>
        <Suspense fallback={<LoadingFallback />}>
          {children}
        </Suspense>
      </Layout>
    </ProtectedRoute>
  );
};

// Theme-aware Toaster component
const ThemedToaster: React.FC = () => {
  const theme = useTheme();

  return (
    <Toaster
      position="top-right"
      toastOptions={{
        duration: 4000,
        style: {
          background: theme.palette.mode === 'light' ? '#363636' : '#1a1d29',
          color: theme.palette.mode === 'light' ? '#fff' : '#ffffff',
          border: theme.palette.mode === 'dark' ? '1px solid rgba(255, 255, 255, 0.08)' : 'none',
        },
        success: {
          duration: 3000,
          iconTheme: {
            primary: '#4caf50',
            secondary: '#fff',
          },
        },
        error: {
          duration: 5000,
          iconTheme: {
            primary: '#f44336',
            secondary: '#fff',
          },
        },
      }}
    />
  );
};

// Create a client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 2,
      refetchOnWindowFocus: false,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
});

// Route change detector for debugging
const RouteChangeDetector: React.FC = () => {
  const location = useLocation();

  useEffect(() => {
    console.log('Route changed to:', location.pathname);
  }, [location]);

  return null;
};

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <CustomThemeProvider>
        <CssBaseline />
        <AuthProvider>
          <ErrorBoundary>
            <Router>
              <RouteChangeDetector />
              <Routes>
              {/* Public Routes */}
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Register />} />

              {/* Protected Routes */}
              <Route path="/" element={
                <ProtectedRoute>
                  <Navigate to="/dashboard" replace />
                </ProtectedRoute>
              } />

              <Route path="/dashboard" element={
                <LazyRoute>
                  <Dashboard />
                </LazyRoute>
              } />

              {/* KPI Management */}
              <Route path="/kpis" element={
                <LazyRoute>
                  <KpiList />
                </LazyRoute>
              } />
              <Route path="/kpis/create" element={
                <LazyRoute>
                  <KpiCreate />
                </LazyRoute>
              } />
              <Route path="/kpis/:id" element={
                <LazyRoute>
                  <KpiDetail />
                </LazyRoute>
              } />
              <Route path="/kpis/:id/edit" element={
                <LazyRoute>
                  <KpiCreate />
                </LazyRoute>
              } />

              {/* Contact Management */}
              <Route path="/contacts" element={
                <LazyRoute>
                  <ContactList />
                </LazyRoute>
              } />
              <Route path="/contacts/create" element={
                <LazyRoute>
                  <ContactCreate />
                </LazyRoute>
              } />
              <Route path="/contacts/:id" element={
                <LazyRoute>
                  <ContactDetail />
                </LazyRoute>
              } />
              <Route path="/contacts/:id/edit" element={
                <LazyRoute>
                  <ContactCreate />
                </LazyRoute>
              } />

              {/* Alert Management */}
              <Route path="/alerts" element={
                <LazyRoute>
                  <AlertList />
                </LazyRoute>
              } />
              <Route path="/alerts/:id" element={
                <LazyRoute>
                  <AlertDetail />
                </LazyRoute>
              } />

              {/* Analytics */}
              <Route path="/analytics" element={
                <LazyRoute>
                  <Analytics />
                </LazyRoute>
              } />

              {/* Execution History */}
              <Route path="/execution-history" element={
                <LazyRoute>
                  <ExecutionHistoryList />
                </LazyRoute>
              } />
              <Route path="/execution-history/:id" element={
                <LazyRoute>
                  <ExecutionHistoryDetail />
                </LazyRoute>
              } />

              {/* Worker Management */}
              <Route path="/worker" element={
                <LazyRoute>
                  <WorkerManagement />
                </LazyRoute>
              } />

              {/* Worker Debug */}
              <Route path="/worker-debug" element={
                <LazyRoute>
                  <WorkerDebug />
                </LazyRoute>
              } />

              {/* User Profile */}
              <Route path="/profile" element={
                <LazyRoute>
                  <UserProfile />
                </LazyRoute>
              } />

              {/* Administration Routes */}
              <Route path="/administration" element={
                <LazyRoute>
                  <Administration />
                </LazyRoute>
              } />

              <Route path="/administration/security" element={
                <LazyRoute>
                  <Administration />
                </LazyRoute>
              } />

              <Route path="/administration/api-keys" element={
                <LazyRoute>
                  <Administration />
                </LazyRoute>
              } />

              <Route path="/administration/audit" element={
                <LazyRoute>
                  <Administration />
                </LazyRoute>
              } />

              {/* Admin Routes */}
              <Route path="/admin" element={
                <LazyRoute requiredPermissions={['System:Admin']}>
                  <AdminDashboard />
                </LazyRoute>
              } />

              <Route path="/admin/users" element={
                <LazyRoute requiredPermissions={['User:Read']}>
                  <UserManagement />
                </LazyRoute>
              } />

              <Route path="/admin/roles" element={
                <LazyRoute requiredPermissions={['Role:Read']}>
                  <RoleManagement />
                </LazyRoute>
              } />

              <Route path="/admin/settings" element={
                <LazyRoute requiredPermissions={['System:Admin']}>
                  <SystemSettings />
                </LazyRoute>
              } />

              {/* Settings */}
              <Route path="/settings" element={
                <LazyRoute>
                  <Settings />
                </LazyRoute>
              } />

              {/* Component Showcase (Development/Demo) */}
              <Route path="/showcase" element={
                <LazyRoute>
                  <ComponentShowcase />
                </LazyRoute>
              } />

              {/* Visualization Showcase (Development/Demo) */}
              <Route path="/visualizations" element={
                <LazyRoute>
                  <VisualizationShowcase />
                </LazyRoute>
              } />

              {/* Interactive Showcase (Development/Demo) */}
              <Route path="/interactions" element={
                <LazyRoute>
                  <InteractiveShowcase />
                </LazyRoute>
              } />



              {/* Catch all route */}
              <Route path="*" element={<Navigate to="/dashboard" replace />} />
              </Routes>
            </Router>
          </ErrorBoundary>
        </AuthProvider>

        {/* Toast notifications */}
        <ThemedToaster />
      </CustomThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
