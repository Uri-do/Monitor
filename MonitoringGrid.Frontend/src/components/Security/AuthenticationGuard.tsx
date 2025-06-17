import React, { useEffect, useState } from 'react';
import { Box, CircularProgress, Typography, Alert, Button } from '@mui/material';
import { Lock as LockIcon, Refresh as RefreshIcon } from '@mui/icons-material';
import { useAuth } from '@/hooks/useAuth';
import { useNavigate, useLocation } from 'react-router-dom';

interface AuthenticationGuardProps {
  children: React.ReactNode;
  requireAuth?: boolean;
  fallbackPath?: string;
}

/**
 * Enhanced Authentication Guard Component
 * Ensures proper authentication enforcement across the application
 */
export const AuthenticationGuard: React.FC<AuthenticationGuardProps> = ({
  children,
  requireAuth = true,
  fallbackPath = '/login'
}) => {
  const { isAuthenticated, isLoading, user, error } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [retryCount, setRetryCount] = useState(0);
  const [showError, setShowError] = useState(false);

  useEffect(() => {
    // Force authentication check on mount
    if (!isLoading && requireAuth && !isAuthenticated) {
      console.warn('Authentication required but user is not authenticated');
      
      // Add delay to prevent immediate redirect loops
      const timer = setTimeout(() => {
        navigate(fallbackPath, { 
          state: { from: location },
          replace: true 
        });
      }, 100);

      return () => clearTimeout(timer);
    }
  }, [isAuthenticated, isLoading, requireAuth, navigate, fallbackPath, location]);

  useEffect(() => {
    // Show error after multiple failed attempts
    if (error && retryCount > 2) {
      setShowError(true);
    }
  }, [error, retryCount]);

  // Handle retry authentication
  const handleRetry = () => {
    setRetryCount(prev => prev + 1);
    setShowError(false);
    window.location.reload();
  };

  // Show loading state
  if (isLoading) {
    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          gap: 2,
          backgroundColor: 'background.default',
        }}
      >
        <CircularProgress size={40} />
        <Typography variant="body1" color="text.secondary">
          Verifying authentication...
        </Typography>
      </Box>
    );
  }

  // Show authentication error
  if (showError || (error && requireAuth)) {
    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          gap: 3,
          p: 3,
          backgroundColor: 'background.default',
        }}
      >
        <LockIcon sx={{ fontSize: 64, color: 'error.main' }} />
        <Typography variant="h5" color="error" textAlign="center">
          Authentication Required
        </Typography>
        <Alert severity="error" sx={{ maxWidth: 400 }}>
          <Typography variant="body2">
            {error || 'You must be logged in to access this application.'}
          </Typography>
        </Alert>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button
            variant="contained"
            onClick={() => navigate(fallbackPath)}
            startIcon={<LockIcon />}
          >
            Go to Login
          </Button>
          <Button
            variant="outlined"
            onClick={handleRetry}
            startIcon={<RefreshIcon />}
          >
            Retry ({retryCount}/3)
          </Button>
        </Box>
      </Box>
    );
  }

  // Show unauthorized access
  if (requireAuth && !isAuthenticated) {
    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          gap: 3,
          p: 3,
          backgroundColor: 'background.default',
        }}
      >
        <LockIcon sx={{ fontSize: 64, color: 'warning.main' }} />
        <Typography variant="h5" color="warning.main" textAlign="center">
          Access Denied
        </Typography>
        <Typography variant="body1" color="text.secondary" textAlign="center">
          Please log in to continue.
        </Typography>
        <Button
          variant="contained"
          onClick={() => navigate(fallbackPath)}
          startIcon={<LockIcon />}
        >
          Go to Login
        </Button>
      </Box>
    );
  }

  // Render children if authenticated or auth not required
  return <>{children}</>;
};

export default AuthenticationGuard;
