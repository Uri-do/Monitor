import React, { Suspense } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
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

// Auth Provider
import { AuthProvider } from '@/hooks/useAuth';

// Theme Provider
import { CustomThemeProvider } from '@/hooks/useTheme';

// Loading fallback component for lazy loaded routes
const LoadingFallback: React.FC = () => (
  <Box
    sx={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: '60vh',
      gap: 2,
    }}
  >
    <CircularProgress size={40} />
    <Typography variant="body1" color="text.secondary">
      Loading...
    </Typography>
  </Box>
);

// Helper component to wrap lazy loaded routes
const LazyRoute: React.FC<{
  children: React.ReactNode;
  requiredPermissions?: string[];
  requiredRoles?: string[];
}> = ({ children, requiredPermissions, requiredRoles }) => (
  <ProtectedRoute requiredPermissions={requiredPermissions} requiredRoles={requiredRoles}>
    <Layout>
      <Suspense fallback={<LoadingFallback />}>
        {children}
      </Suspense>
    </Layout>
  </ProtectedRoute>
);

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
      <CustomThemeProvider>
        <CssBaseline />
        <AuthProvider>
          <Router>
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
                <ProtectedRoute>
                  <Layout>
                    <ContactList />
                  </Layout>
                </ProtectedRoute>
              } />
              <Route path="/contacts/create" element={
                <ProtectedRoute>
                  <Layout>
                    <ContactCreate />
                  </Layout>
                </ProtectedRoute>
              } />
              <Route path="/contacts/:id" element={
                <ProtectedRoute>
                  <Layout>
                    <ContactDetail />
                  </Layout>
                </ProtectedRoute>
              } />
              <Route path="/contacts/:id/edit" element={
                <ProtectedRoute>
                  <Layout>
                    <ContactCreate />
                  </Layout>
                </ProtectedRoute>
              } />

              {/* Alert Management */}
              <Route path="/alerts" element={
                <ProtectedRoute>
                  <Layout>
                    <AlertList />
                  </Layout>
                </ProtectedRoute>
              } />
              <Route path="/alerts/:id" element={
                <ProtectedRoute>
                  <Layout>
                    <AlertDetail />
                  </Layout>
                </ProtectedRoute>
              } />

              {/* Analytics */}
              <Route path="/analytics" element={
                <ProtectedRoute>
                  <Layout>
                    <Analytics />
                  </Layout>
                </ProtectedRoute>
              } />

              {/* Execution History */}
              <Route path="/execution-history" element={
                <ProtectedRoute>
                  <Layout>
                    <ExecutionHistoryList />
                  </Layout>
                </ProtectedRoute>
              } />
              <Route path="/execution-history/:id" element={
                <ProtectedRoute>
                  <Layout>
                    <ExecutionHistoryDetail />
                  </Layout>
                </ProtectedRoute>
              } />

              {/* User Profile */}
              <Route path="/profile" element={
                <ProtectedRoute>
                  <Layout>
                    <UserProfile />
                  </Layout>
                </ProtectedRoute>
              } />

              {/* Administration Routes */}
              <Route path="/administration" element={
                <ProtectedRoute>
                  <Layout>
                    <Administration />
                  </Layout>
                </ProtectedRoute>
              } />

              <Route path="/administration/security" element={
                <ProtectedRoute>
                  <Layout>
                    <Administration />
                  </Layout>
                </ProtectedRoute>
              } />

              <Route path="/administration/api-keys" element={
                <ProtectedRoute>
                  <Layout>
                    <Administration />
                  </Layout>
                </ProtectedRoute>
              } />

              <Route path="/administration/audit" element={
                <ProtectedRoute>
                  <Layout>
                    <Administration />
                  </Layout>
                </ProtectedRoute>
              } />

              {/* Admin Routes */}
              <Route path="/admin" element={
                <ProtectedRoute requiredPermissions={['System:Admin']}>
                  <Layout>
                    <AdminDashboard />
                  </Layout>
                </ProtectedRoute>
              } />

              <Route path="/admin/users" element={
                <ProtectedRoute requiredPermissions={['User:Read']}>
                  <Layout>
                    <UserManagement />
                  </Layout>
                </ProtectedRoute>
              } />

              <Route path="/admin/roles" element={
                <ProtectedRoute requiredPermissions={['Role:Read']}>
                  <Layout>
                    <RoleManagement />
                  </Layout>
                </ProtectedRoute>
              } />

              <Route path="/admin/settings" element={
                <ProtectedRoute requiredPermissions={['System:Admin']}>
                  <Layout>
                    <SystemSettings />
                  </Layout>
                </ProtectedRoute>
              } />

              {/* Settings */}
              <Route path="/settings" element={
                <ProtectedRoute>
                  <Layout>
                    <Settings />
                  </Layout>
                </ProtectedRoute>
              } />

              {/* Catch all route */}
              <Route path="*" element={<Navigate to="/dashboard" replace />} />
            </Routes>
          </Router>
        </AuthProvider>

        {/* Toast notifications */}
        <ThemedToaster />
      </CustomThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
