import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { Box, CircularProgress, Typography } from '@mui/material';
import { useAuth } from '../../hooks/useAuth';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredPermissions?: string[];
  requiredRoles?: string[];
  fallbackPath?: string;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredPermissions = [],
  requiredRoles = [],
  fallbackPath = '/login'
}) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  const location = useLocation();

  // Show loading spinner while checking authentication
  if (isLoading) {
    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          gap: 2
        }}
      >
        <CircularProgress size={40} />
        <Typography variant="body1" color="text.secondary">
          Loading...
        </Typography>
      </Box>
    );
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated || !user) {
    return (
      <Navigate
        to={fallbackPath}
        state={{ from: location }}
        replace
      />
    );
  }

  // Check if user has required permissions
  if (requiredPermissions.length > 0) {
    const hasRequiredPermissions = requiredPermissions.every(permission =>
      user.permissions.includes(permission)
    );

    if (!hasRequiredPermissions) {
      return (
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            minHeight: '100vh',
            gap: 2,
            p: 3
          }}
        >
          <Typography variant="h5" color="error">
            Access Denied
          </Typography>
          <Typography variant="body1" color="text.secondary" textAlign="center">
            You don't have the required permissions to access this page.
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Required permissions: {requiredPermissions.join(', ')}
          </Typography>
        </Box>
      );
    }
  }

  // Check if user has required roles
  if (requiredRoles.length > 0) {
    const hasRequiredRoles = requiredRoles.some(role =>
      user.roles.some(userRole => userRole.name === role)
    );

    if (!hasRequiredRoles) {
      return (
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            minHeight: '100vh',
            gap: 2,
            p: 3
          }}
        >
          <Typography variant="h5" color="error">
            Access Denied
          </Typography>
          <Typography variant="body1" color="text.secondary" textAlign="center">
            You don't have the required role to access this page.
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Required roles: {requiredRoles.join(', ')}
          </Typography>
        </Box>
      );
    }
  }

  // User is authenticated and has required permissions/roles
  return <>{children}</>;
};

export default ProtectedRoute;
