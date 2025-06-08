import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CssBaseline } from '@mui/material';
import { Toaster } from 'react-hot-toast';
import { useTheme } from '@mui/material/styles';

// Components
import Layout from '@/components/Layout/Layout';
import ProtectedRoute from '@/components/Auth/ProtectedRoute';
import Dashboard from '@/pages/Dashboard/Dashboard';
import KpiList from '@/pages/KPI/KpiList';
import KpiDetail from '@/pages/KPI/KpiDetail';
import KpiCreate from '@/pages/KPI/KpiCreate';
import ContactList from '@/pages/Contact/ContactList';
import ContactDetail from '@/pages/Contact/ContactDetail';
import ContactCreate from '@/pages/Contact/ContactCreate';
import AlertList from '@/pages/Alert/AlertList';
import AlertDetail from '@/pages/Alert/AlertDetail';
import Analytics from '@/pages/Analytics/Analytics';
import Settings from '@/pages/Settings/Settings';
import Login from '@/pages/Auth/Login';
import Register from '@/pages/Auth/Register';
import UserProfile from '@/pages/User/UserProfile';
import UserManagement from '@/pages/Users/UserManagement';
import RoleManagement from '@/pages/Admin/RoleManagement';
import AdminDashboard from '@/pages/Admin/AdminDashboard';
import SystemSettings from '@/pages/Admin/SystemSettings';
import Administration from '@/pages/Administration/Administration';
import ExecutionHistoryList from '@/pages/ExecutionHistory/ExecutionHistoryList';
import ExecutionHistoryDetail from '@/pages/ExecutionHistory/ExecutionHistoryDetail';

// Auth Provider
import { AuthProvider } from '@/hooks/useAuth';

// Theme Provider
import { CustomThemeProvider } from '@/hooks/useTheme';

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
                <ProtectedRoute>
                  <Layout>
                    <Dashboard />
                  </Layout>
                </ProtectedRoute>
              } />

              {/* KPI Management */}
              <Route path="/kpis" element={
                <ProtectedRoute>
                  <Layout>
                    <KpiList />
                  </Layout>
                </ProtectedRoute>
              } />
              <Route path="/kpis/create" element={
                <ProtectedRoute>
                  <Layout>
                    <KpiCreate />
                  </Layout>
                </ProtectedRoute>
              } />
              <Route path="/kpis/:id" element={
                <ProtectedRoute>
                  <Layout>
                    <KpiDetail />
                  </Layout>
                </ProtectedRoute>
              } />
              <Route path="/kpis/:id/edit" element={
                <ProtectedRoute>
                  <Layout>
                    <KpiCreate />
                  </Layout>
                </ProtectedRoute>
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
