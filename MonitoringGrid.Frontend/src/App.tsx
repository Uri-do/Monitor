import React, { Suspense, Component, ErrorInfo, ReactNode } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CssBaseline, Box, CircularProgress, Typography } from '@mui/material';
import { Toaster } from 'react-hot-toast';
import { useTheme } from '@mui/material/styles';
import { I18nextProvider } from 'react-i18next';
import i18n from '@/i18n';

// Core Components (not lazy loaded for better initial performance)
import Layout from '@/components/Layout/Layout';
import ProtectedRoute from '@/components/Auth/ProtectedRoute';
import Login from '@/pages/Auth/Login';
import Register from '@/pages/Auth/Register';

// Lazy loaded components for code splitting
const Dashboard = React.lazy(() => import('@/pages/Dashboard/Dashboard'));

// Indicator Management (New System)
const IndicatorList = React.lazy(() => import('@/pages/Indicator/IndicatorList'));
const IndicatorDetail = React.lazy(() => import('@/pages/Indicator/IndicatorDetail'));
const IndicatorCreate = React.lazy(() => import('@/pages/Indicator/IndicatorCreate'));

// Legacy KPI imports removed - using Indicator system instead
const ContactList = React.lazy(() => import('@/pages/Contact/ContactList'));
const ContactDetail = React.lazy(() => import('@/pages/Contact/ContactDetail'));
const ContactCreate = React.lazy(() => import('@/pages/Contact/ContactCreate'));
const AlertList = React.lazy(() => import('@/pages/Alert/AlertList'));
const AlertDetail = React.lazy(() => import('@/pages/Alert/AlertDetail'));
const Analytics = React.lazy(() => import('@/pages/Analytics/Analytics'));
const Statistics = React.lazy(() => import('@/pages/Statistics/StatisticsPage'));
const Settings = React.lazy(() => import('@/pages/Settings/Settings'));
const UserProfile = React.lazy(() => import('@/pages/User/UserProfile'));
const UserManagement = React.lazy(() => import('@/pages/Users/UserManagement'));
const RoleManagement = React.lazy(() => import('@/pages/Admin/RoleManagement'));

const SystemSettings = React.lazy(() => import('@/pages/Admin/SystemSettings'));
const Administration = React.lazy(() => import('@/pages/Admin/Administration'));
const ExecutionHistoryList = React.lazy(
  () => import('@/pages/ExecutionHistory/ExecutionHistoryList')
);
const ExecutionHistoryDetail = React.lazy(
  () => import('@/pages/ExecutionHistory/ExecutionHistoryDetail')
);
const WorkerManagement = React.lazy(() => import('@/pages/Worker/WorkerManagement'));
const AuthTest = React.lazy(() => import('@/pages/AuthTest'));

// Auth Provider
import { AuthProvider } from '@/hooks/useAuth';

// Theme Provider
import { CustomThemeProvider } from '@/hooks/useTheme';

// Realtime Provider
import { RealtimeProvider } from '@/contexts/RealtimeContext';

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

  componentDidCatch(_error: Error, _errorInfo: ErrorInfo) {
    // Error logged to error boundary for debugging
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
          <button onClick={() => window.location.reload()}>Reload Page</button>
        </Box>
      );
    }

    return this.props.children;
  }
}

// Loading fallback component for lazy loaded routes
const LoadingFallback: React.FC = () => {
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
  return (
    <ProtectedRoute requiredPermissions={requiredPermissions} requiredRoles={requiredRoles}>
      <Layout>
        <Suspense fallback={<LoadingFallback />}>{children}</Suspense>
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

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <I18nextProvider i18n={i18n}>
        <CustomThemeProvider>
          <CssBaseline />
          <ErrorBoundary>
            <AuthProvider>
              <Router future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
                <Routes>
                  {/* Public Routes */}
                  <Route path="/login" element={<Login />} />
                  <Route path="/register" element={<Register />} />
                  <Route path="/auth-test" element={<React.Suspense fallback={<div>Loading...</div>}><AuthTest /></React.Suspense>} />

                  {/* Authenticated Routes */}
                  <Route
                    path="/*"
                    element={
                      <RealtimeProvider>
                        <Routes>
                          {/* Protected Routes */}
                          <Route
                            path="/"
                            element={
                              <ProtectedRoute>
                                <Navigate to="/dashboard" replace />
                              </ProtectedRoute>
                            }
                          />

                          <Route
                            path="/dashboard"
                            element={
                              <LazyRoute>
                                <Dashboard />
                              </LazyRoute>
                            }
                          />

                          {/* Indicator Management (New System) */}
                          <Route
                            path="/indicators"
                            element={
                              <LazyRoute>
                                <IndicatorList />
                              </LazyRoute>
                            }
                          />
                          <Route
                            path="/indicators/create"
                            element={
                              <LazyRoute>
                                <IndicatorCreate />
                              </LazyRoute>
                            }
                          />
                          <Route
                            path="/indicators/:id"
                            element={
                              <LazyRoute>
                                <IndicatorDetail />
                              </LazyRoute>
                            }
                          />
                          <Route
                            path="/indicators/:id/edit"
                            element={
                              <LazyRoute>
                                <IndicatorCreate />
                              </LazyRoute>
                            }
                          />

                          {/* Legacy KPI routes removed - redirecting to Indicators */}
                          <Route path="/kpis" element={<Navigate to="/indicators" replace />} />
                          <Route path="/kpis/create" element={<Navigate to="/indicators/create" replace />} />
                          <Route path="/kpis/:id" element={<Navigate to="/indicators/:id" replace />} />
                          <Route path="/kpis/:id/edit" element={<Navigate to="/indicators/:id/edit" replace />} />

                          {/* Contact Management */}
                          <Route
                            path="/contacts"
                            element={
                              <LazyRoute>
                                <ContactList />
                              </LazyRoute>
                            }
                          />
                          <Route
                            path="/contacts/create"
                            element={
                              <LazyRoute>
                                <ContactCreate />
                              </LazyRoute>
                            }
                          />
                          <Route
                            path="/contacts/:id"
                            element={
                              <LazyRoute>
                                <ContactDetail />
                              </LazyRoute>
                            }
                          />
                          <Route
                            path="/contacts/:id/edit"
                            element={
                              <LazyRoute>
                                <ContactCreate />
                              </LazyRoute>
                            }
                          />

                          {/* Alert Management */}
                          <Route
                            path="/alerts"
                            element={
                              <LazyRoute>
                                <AlertList />
                              </LazyRoute>
                            }
                          />
                          <Route
                            path="/alerts/:id"
                            element={
                              <LazyRoute>
                                <AlertDetail />
                              </LazyRoute>
                            }
                          />

                          {/* Analytics */}
                          <Route
                            path="/analytics"
                            element={
                              <LazyRoute>
                                <Analytics />
                              </LazyRoute>
                            }
                          />

                          {/* Statistics */}
                          <Route
                            path="/statistics"
                            element={
                              <LazyRoute>
                                <Statistics />
                              </LazyRoute>
                            }
                          />

                          {/* Execution History */}
                          <Route
                            path="/execution-history"
                            element={
                              <LazyRoute>
                                <ExecutionHistoryList />
                              </LazyRoute>
                            }
                          />
                          <Route
                            path="/execution-history/:id"
                            element={
                              <LazyRoute>
                                <ExecutionHistoryDetail />
                              </LazyRoute>
                            }
                          />

                          {/* Worker Management */}
                          <Route
                            path="/worker"
                            element={
                              <LazyRoute>
                                <WorkerManagement />
                              </LazyRoute>
                            }
                          />

                          {/* User Profile */}
                          <Route
                            path="/profile"
                            element={
                              <LazyRoute>
                                <UserProfile />
                              </LazyRoute>
                            }
                          />

                          {/* Administration Routes */}
                          <Route
                            path="/admin"
                            element={
                              <LazyRoute>
                                <Administration />
                              </LazyRoute>
                            }
                          />

                          <Route
                            path="/admin/security"
                            element={
                              <LazyRoute>
                                <Administration />
                              </LazyRoute>
                            }
                          />

                          <Route
                            path="/admin/api-keys"
                            element={
                              <LazyRoute>
                                <Administration />
                              </LazyRoute>
                            }
                          />

                          <Route
                            path="/admin/audit"
                            element={
                              <LazyRoute>
                                <Administration />
                              </LazyRoute>
                            }
                          />

                          {/* Legacy administration route redirect */}
                          <Route
                            path="/administration"
                            element={<Navigate to="/admin" replace />}
                          />

                          <Route
                            path="/admin/users"
                            element={
                              <LazyRoute>
                                <UserManagement />
                              </LazyRoute>
                            }
                          />

                          <Route
                            path="/admin/roles"
                            element={
                              <LazyRoute>
                                <RoleManagement />
                              </LazyRoute>
                            }
                          />

                          <Route
                            path="/admin/settings"
                            element={
                              <LazyRoute>
                                <SystemSettings />
                              </LazyRoute>
                            }
                          />

                          {/* Settings */}
                          <Route
                            path="/settings"
                            element={
                              <LazyRoute>
                                <Settings />
                              </LazyRoute>
                            }
                          />

                          {/* Catch all route */}
                          <Route path="*" element={<Navigate to="/dashboard" replace />} />
                        </Routes>
                      </RealtimeProvider>
                    }
                  />
                </Routes>
              </Router>
            </AuthProvider>
          </ErrorBoundary>

          {/* Toast notifications */}
          <ThemedToaster />
        </CustomThemeProvider>
      </I18nextProvider>
    </QueryClientProvider>
  );
}

export default App;
