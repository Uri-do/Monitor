import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConfigProvider } from 'antd';
import { I18nextProvider } from 'react-i18next';
import { Toaster } from 'react-hot-toast';
import i18n from '@/i18n';

// Route configuration
import { routeConfig, legacyRedirects, publicRoutes } from '@/config/routes';
import RouteRenderer from '@/components/Router/RouteRenderer';

// Auth Provider
import { AuthProvider } from '@/hooks/useAuth';

// Theme Provider
import { CustomThemeProvider, useTheme } from '@/hooks/useTheme';

// Realtime Provider
import { RealtimeProvider } from '@/contexts/RealtimeContext';

// Security Provider
import { SecurityProvider } from '@/contexts/SecurityContext';
import { SecurityHeaders, CSPViolationReporter } from '@/components/Security/SecurityHeaders';

// Import the comprehensive ErrorBoundary
import { ErrorBoundary } from '@/components/Common/ErrorBoundary';

// Simple Toaster component
const ThemedToaster: React.FC = () => {
  return (
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

// Component that uses theme and provides it to Ant Design
const ThemedApp: React.FC = () => {
  const { theme } = useTheme();

  return (
    <ConfigProvider theme={theme}>
      <SecurityProvider>
        <SecurityHeaders />
        <CSPViolationReporter />
        <ErrorBoundary>
          <AuthProvider>
            <Router future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
              <Routes>
                {/* Public Routes */}
                {publicRoutes.map(route => {
                  const { path, element, requiredPermissions, requiredRoles } = route;

                  // Handle React elements (like Navigate)
                  if (React.isValidElement(element)) {
                    return (
                      <Route
                        key={path}
                        path={path}
                        element={element}
                      />
                    );
                  }

                  // Handle component types
                  const Component = element as React.ComponentType<any>;

                  return (
                    <Route
                      key={path}
                      path={path}
                      element={
                        <React.Suspense fallback={<div>Loading...</div>}>
                          <Component />
                        </React.Suspense>
                      }
                    />
                  );
                })}

                {/* Authenticated Routes */}
                <Route
                  path="/*"
                  element={
                    <RealtimeProvider>
                      <RouteRenderer routes={[...routeConfig, ...legacyRedirects]} />
                    </RealtimeProvider>
                  }
                />

                {/* Catch all route */}
                <Route path="*" element={<Navigate to="/dashboard" replace />} />
              </Routes>
            </Router>
          </AuthProvider>
        </ErrorBoundary>

        {/* Toast notifications */}
        <ThemedToaster />
      </SecurityProvider>
    </ConfigProvider>
  );
};

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <I18nextProvider i18n={i18n}>
        <CustomThemeProvider>
          <ThemedApp />
        </CustomThemeProvider>
      </I18nextProvider>
    </QueryClientProvider>
  );
}

export default App;
