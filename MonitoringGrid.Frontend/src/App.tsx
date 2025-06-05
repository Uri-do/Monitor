import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline, Box } from '@mui/material';
import { Toaster } from 'react-hot-toast';

// Components
import Layout from '@/components/Layout/Layout';
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
      default: '#f5f5f5',
      paper: '#ffffff',
    },
    error: {
      main: '#f44336',
    },
    warning: {
      main: '#ff9800',
    },
    success: {
      main: '#4caf50',
    },
  },
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    h4: {
      fontWeight: 600,
    },
    h5: {
      fontWeight: 600,
    },
    h6: {
      fontWeight: 600,
    },
  },
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
          borderRadius: 8,
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          borderRadius: 6,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 6,
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
        <Box sx={{ display: 'flex', minHeight: '100vh' }}>
          <Router>
            <Layout>
              <Routes>
                {/* Dashboard */}
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route path="/dashboard" element={<Dashboard />} />
                
                {/* KPI Management */}
                <Route path="/kpis" element={<KpiList />} />
                <Route path="/kpis/create" element={<KpiCreate />} />
                <Route path="/kpis/:id" element={<KpiDetail />} />
                <Route path="/kpis/:id/edit" element={<KpiCreate />} />
                
                {/* Contact Management */}
                <Route path="/contacts" element={<ContactList />} />
                <Route path="/contacts/create" element={<ContactCreate />} />
                <Route path="/contacts/:id" element={<ContactDetail />} />
                <Route path="/contacts/:id/edit" element={<ContactCreate />} />
                
                {/* Alert Management */}
                <Route path="/alerts" element={<AlertList />} />
                <Route path="/alerts/:id" element={<AlertDetail />} />
                
                {/* Analytics */}
                <Route path="/analytics" element={<Analytics />} />
                
                {/* Settings */}
                <Route path="/settings" element={<Settings />} />
                
                {/* Catch all route */}
                <Route path="*" element={<Navigate to="/dashboard" replace />} />
              </Routes>
            </Layout>
          </Router>
        </Box>
        
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
