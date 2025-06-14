import React, { Suspense } from 'react';
import { Routes, Route } from 'react-router-dom';
import { Box, CircularProgress, Typography } from '@mui/material';
import Layout from '@/components/Layout/Layout';
import ProtectedRoute from '@/components/Auth/ProtectedRoute';
import { RouteConfig } from '@/config/routes';

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

// Route renderer component
interface RouteRendererProps {
  routes: RouteConfig[];
  isProtected?: boolean;
}

export const RouteRenderer: React.FC<RouteRendererProps> = ({ routes, isProtected = true }) => {
  const renderRoute = (route: RouteConfig) => {
    const { path, element, requiredPermissions, requiredRoles } = route;
    
    // Handle React elements (like Navigate)
    if (React.isValidElement(element)) {
      return (
        <Route
          key={path}
          path={path}
          element={isProtected ? (
            <ProtectedRoute requiredPermissions={requiredPermissions} requiredRoles={requiredRoles}>
              {element}
            </ProtectedRoute>
          ) : element}
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
          isProtected ? (
            <LazyRoute requiredPermissions={requiredPermissions} requiredRoles={requiredRoles}>
              <Component />
            </LazyRoute>
          ) : (
            <Suspense fallback={<LoadingFallback />}>
              <Component />
            </Suspense>
          )
        }
      />
    );
  };

  return (
    <Routes>
      {routes.map(renderRoute)}
    </Routes>
  );
};

export default RouteRenderer;
