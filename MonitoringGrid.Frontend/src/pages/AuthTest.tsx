import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Alert,
  Divider,
  Grid,
  Chip,
} from '@mui/material';
import { format } from 'date-fns';
import { useAuth } from '@/hooks/useAuth';
import { authService } from '@/services/authService';

const AuthTest: React.FC = () => {
  const { user, token, isAuthenticated, isLoading, refreshToken } = useAuth();
  const [testResults, setTestResults] = useState<string[]>([]);
  const [isTestingRefresh, setIsTestingRefresh] = useState(false);

  const addTestResult = (message: string) => {
    setTestResults(prev => [...prev, `${format(new Date(), 'HH:mm:ss')} - ${message}`]);
  };

  const testTokenValidation = async () => {
    try {
      addTestResult('Testing token validation...');
      const currentUser = await authService.getCurrentUser();
      addTestResult(`✅ Token validation successful: ${currentUser.username}`);
    } catch (error: any) {
      addTestResult(`❌ Token validation failed: ${error.message}`);
    }
  };

  const testTokenRefresh = async () => {
    setIsTestingRefresh(true);
    try {
      addTestResult('Testing token refresh...');
      await refreshToken();
      addTestResult('✅ Token refresh successful');
    } catch (error: any) {
      addTestResult(`❌ Token refresh failed: ${error.message}`);
    } finally {
      setIsTestingRefresh(false);
    }
  };

  const testTokenExpiration = () => {
    const currentToken = authService.getToken();
    if (!currentToken) {
      addTestResult('❌ No token available');
      return;
    }

    try {
      const payload = JSON.parse(atob(currentToken.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      const expiresAt = new Date(payload.exp * 1000);
      const timeUntilExpiry = payload.exp - currentTime;
      
      addTestResult(`Token expires at: ${format(expiresAt, 'PPpp')}`);
      addTestResult(`Time until expiry: ${Math.floor(timeUntilExpiry / 60)} minutes`);
      addTestResult(`Should refresh: ${authService.shouldRefreshToken() ? 'Yes' : 'No'}`);
    } catch (error: any) {
      addTestResult(`❌ Failed to decode token: ${error.message}`);
    }
  };

  const clearTestResults = () => {
    setTestResults([]);
  };

  if (isLoading) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Loading authentication test...</Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3, maxWidth: 1200, mx: 'auto' }}>
      <Typography variant="h4" gutterBottom>
        Authentication Test Page
      </Typography>
      
      <Grid container spacing={3}>
        {/* Authentication Status */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Authentication Status
              </Typography>
              
              <Box sx={{ mb: 2 }}>
                <Chip 
                  label={isAuthenticated ? 'Authenticated' : 'Not Authenticated'}
                  color={isAuthenticated ? 'success' : 'error'}
                  sx={{ mr: 1 }}
                />
                {isLoading && <Chip label="Loading" color="warning" />}
              </Box>

              {user && (
                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2">
                    <strong>User:</strong> {user.username}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Email:</strong> {user.email}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Roles:</strong> {user.roles?.map(r => r.name).join(', ') || 'None'}
                  </Typography>
                </Box>
              )}

              {token && (
                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2">
                    <strong>Token (first 50 chars):</strong>
                  </Typography>
                  <Typography variant="caption" sx={{ fontFamily: 'monospace', wordBreak: 'break-all' }}>
                    {token.substring(0, 50)}...
                  </Typography>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Test Controls */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Test Controls
              </Typography>
              
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Button 
                  variant="outlined" 
                  onClick={testTokenValidation}
                  disabled={!isAuthenticated}
                >
                  Test Token Validation
                </Button>
                
                <Button 
                  variant="outlined" 
                  onClick={testTokenRefresh}
                  disabled={!isAuthenticated || isTestingRefresh}
                >
                  {isTestingRefresh ? 'Refreshing...' : 'Test Token Refresh'}
                </Button>
                
                <Button 
                  variant="outlined" 
                  onClick={testTokenExpiration}
                  disabled={!isAuthenticated}
                >
                  Check Token Expiration
                </Button>
                
                <Divider />
                
                <Button 
                  variant="outlined" 
                  color="secondary"
                  onClick={clearTestResults}
                >
                  Clear Test Results
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Test Results */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Test Results
              </Typography>
              
              {testResults.length === 0 ? (
                <Alert severity="info">
                  No test results yet. Click the test buttons above to run authentication tests.
                </Alert>
              ) : (
                <Box 
                  sx={{ 
                    backgroundColor: 'grey.100', 
                    p: 2, 
                    borderRadius: 1,
                    fontFamily: 'monospace',
                    fontSize: '0.875rem',
                    maxHeight: 400,
                    overflow: 'auto'
                  }}
                >
                  {testResults.map((result, index) => (
                    <div key={index}>{result}</div>
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Box sx={{ mt: 3 }}>
        <Alert severity="info">
          <Typography variant="body2">
            <strong>Instructions:</strong> Use this page to test authentication functionality.
            Try refreshing the page to see if the authentication state is maintained properly.
            The token should automatically refresh when it's close to expiring.
          </Typography>
        </Alert>
      </Box>
    </Box>
  );
};

export default AuthTest;
