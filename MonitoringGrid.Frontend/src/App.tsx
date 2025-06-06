import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline } from '@mui/material';
import { Toaster } from 'react-hot-toast';

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

// Auth Provider
import { AuthProvider } from '@/hooks/useAuth';

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

// Create theme
const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1976d2',
      light: '#42a5f5',
      dark: '#1565c0',
    },
    secondary: {
      main: '#dc004e',
      light: '#ff5983',
      dark: '#9a0036',
    },
    background: {
      default: '#f8fafc',
      paper: '#ffffff',
    },
    error: {
      main: '#f44336',
      light: '#ef5350',
      dark: '#d32f2f',
    },
    warning: {
      main: '#ff9800',
      light: '#ffb74d',
      dark: '#f57c00',
    },
    success: {
      main: '#4caf50',
      light: '#66bb6a',
      dark: '#388e3c',
    },
    info: {
      main: '#2196f3',
      light: '#64b5f6',
      dark: '#1976d2',
    },
    grey: {
      50: '#fafafa',
      100: '#f5f5f5',
      200: '#eeeeee',
      300: '#e0e0e0',
      400: '#bdbdbd',
      500: '#9e9e9e',
      600: '#757575',
      700: '#616161',
      800: '#424242',
      900: '#212121',
    },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h1: {
      fontWeight: 700,
      fontSize: '2.5rem',
      lineHeight: 1.2,
    },
    h2: {
      fontWeight: 700,
      fontSize: '2rem',
      lineHeight: 1.3,
    },
    h3: {
      fontWeight: 600,
      fontSize: '1.75rem',
      lineHeight: 1.3,
    },
    h4: {
      fontWeight: 600,
      fontSize: '1.5rem',
      lineHeight: 1.4,
    },
    h5: {
      fontWeight: 600,
      fontSize: '1.25rem',
      lineHeight: 1.4,
    },
    h6: {
      fontWeight: 600,
      fontSize: '1.125rem',
      lineHeight: 1.4,
    },
    subtitle1: {
      fontWeight: 500,
      fontSize: '1rem',
      lineHeight: 1.5,
    },
    subtitle2: {
      fontWeight: 500,
      fontSize: '0.875rem',
      lineHeight: 1.5,
    },
    body1: {
      fontSize: '1rem',
      lineHeight: 1.6,
    },
    body2: {
      fontSize: '0.875rem',
      lineHeight: 1.6,
    },
    caption: {
      fontSize: '0.75rem',
      lineHeight: 1.5,
      color: '#6b7280',
    },
  },
  shape: {
    borderRadius: 12,
  },
  shadows: [
    'none',
    '0px 1px 3px rgba(0, 0, 0, 0.05)',
    '0px 1px 6px rgba(0, 0, 0, 0.08)',
    '0px 3px 12px rgba(0, 0, 0, 0.1)',
    '0px 4px 16px rgba(0, 0, 0, 0.12)',
    '0px 6px 20px rgba(0, 0, 0, 0.14)',
    '0px 8px 24px rgba(0, 0, 0, 0.16)',
    '0px 12px 28px rgba(0, 0, 0, 0.18)',
    '0px 16px 32px rgba(0, 0, 0, 0.2)',
    '0px 20px 36px rgba(0, 0, 0, 0.22)',
    '0px 24px 40px rgba(0, 0, 0, 0.24)',
    '0px 28px 44px rgba(0, 0, 0, 0.26)',
    '0px 32px 48px rgba(0, 0, 0, 0.28)',
    '0px 36px 52px rgba(0, 0, 0, 0.3)',
    '0px 40px 56px rgba(0, 0, 0, 0.32)',
    '0px 44px 60px rgba(0, 0, 0, 0.34)',
    '0px 48px 64px rgba(0, 0, 0, 0.36)',
    '0px 52px 68px rgba(0, 0, 0, 0.38)',
    '0px 56px 72px rgba(0, 0, 0, 0.4)',
    '0px 60px 76px rgba(0, 0, 0, 0.42)',
    '0px 64px 80px rgba(0, 0, 0, 0.44)',
    '0px 68px 84px rgba(0, 0, 0, 0.46)',
    '0px 72px 88px rgba(0, 0, 0, 0.48)',
    '0px 76px 92px rgba(0, 0, 0, 0.5)',
    '0px 80px 96px rgba(0, 0, 0, 0.52)',
  ],
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 16,
          boxShadow: '0px 2px 8px rgba(0, 0, 0, 0.08)',
          border: '1px solid rgba(0, 0, 0, 0.04)',
          transition: 'all 0.2s ease-in-out',
          '&:hover': {
            boxShadow: '0px 4px 16px rgba(0, 0, 0, 0.12)',
            transform: 'translateY(-2px)',
          },
        },
      },
    },
    MuiCardContent: {
      styleOverrides: {
        root: {
          padding: '24px',
          '&:last-child': {
            paddingBottom: '24px',
          },
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 500,
          borderRadius: 8,
        },
        sizeSmall: {
          height: 24,
          fontSize: '0.75rem',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          textTransform: 'none',
          fontWeight: 600,
          boxShadow: 'none',
          '&:hover': {
            boxShadow: '0px 2px 8px rgba(0, 0, 0, 0.15)',
          },
        },
      },
    },
    MuiIconButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
        },
      },
    },
    MuiListItem: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          marginBottom: 4,
          '&:hover': {
            backgroundColor: 'rgba(0, 0, 0, 0.02)',
          },
        },
      },
    },
    MuiLinearProgress: {
      styleOverrides: {
        root: {
          borderRadius: 4,
          height: 6,
        },
      },
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
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
        <Toaster
          position="top-right"
          toastOptions={{
            duration: 4000,
            style: {
              background: '#363636',
              color: '#fff',
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
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
