import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  TextField,
  Button,
  Typography,
  Alert,
  Divider,
  Paper,
  Grid,
} from '@mui/material';
import { securityApi, kpiApi, systemApi } from '@/services/api';

const AuthTest: React.FC = () => {
  const [credentials, setCredentials] = useState({
    username: 'testuser',
    password: 'Test123!',
  });

  const [results, setResults] = useState<{
    login?: any;
    dashboard?: any;
    health?: any;
    alertDashboard?: any;
    error?: string;
  }>({});

  const [isLoading, setIsLoading] = useState(false);

  const testLogin = async () => {
    setIsLoading(true);
    setResults({});

    try {
      // Test 1: Login
      console.log('Testing login with:', credentials);
      const loginResponse = await securityApi.login(credentials);
      console.log('Login response:', loginResponse);

      setResults(prev => ({ ...prev, login: loginResponse }));

      if (loginResponse.isSuccess && loginResponse.token) {
        // Store token for subsequent requests
        localStorage.setItem('auth_token', loginResponse.token.accessToken);

        // Test 2: Get Dashboard (protected endpoint)
        try {
          console.log('Testing dashboard access...');
          const dashboardResponse = await kpiApi.getDashboard();
          console.log('Dashboard response:', dashboardResponse);
          setResults(prev => ({ ...prev, dashboard: dashboardResponse }));
        } catch (dashboardError) {
          console.error('Dashboard error:', dashboardError);
          setResults(prev => ({
            ...prev,
            dashboard: {
              error: dashboardError instanceof Error ? dashboardError.message : 'Dashboard failed',
            },
          }));
        }
      }
    } catch (error) {
      console.error('Login error:', error);
      setResults(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Login failed',
      }));
    } finally {
      setIsLoading(false);
    }
  };

  const testHealth = async () => {
    try {
      console.log('Testing health endpoint...');
      const healthResponse = await systemApi.getHealth();
      console.log('Health response:', healthResponse);
      setResults(prev => ({ ...prev, health: healthResponse }));
    } catch (error) {
      console.error('Health error:', error);
      setResults(prev => ({
        ...prev,
        health: { error: error instanceof Error ? error.message : 'Health check failed' },
      }));
    }
  };

  const testAlertDashboard = async () => {
    try {
      const alertResponse = await kpiApi.getDashboard();
      setResults(prev => ({ ...prev, alertDashboard: alertResponse }));
    } catch (error) {
      setResults(prev => ({
        ...prev,
        alertDashboard: {
          error: error instanceof Error ? error.message : 'Alert dashboard failed',
        },
      }));
    }
  };

  const clearResults = () => {
    setResults({});
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
  };

  return (
    <Box sx={{ p: 3, maxWidth: 1200, mx: 'auto' }}>
      <Typography variant="h4" gutterBottom>
        Authentication Flow Test
      </Typography>

      <Grid container spacing={3}>
        {/* Test Controls */}
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Test Credentials
              </Typography>

              <TextField
                fullWidth
                label="Username"
                value={credentials.username}
                onChange={e => setCredentials(prev => ({ ...prev, username: e.target.value }))}
                margin="normal"
              />

              <TextField
                fullWidth
                label="Password"
                type="password"
                value={credentials.password}
                onChange={e => setCredentials(prev => ({ ...prev, password: e.target.value }))}
                margin="normal"
              />

              <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Button variant="contained" onClick={testLogin} disabled={isLoading} fullWidth>
                  {isLoading ? 'Testing...' : 'Test Login & Dashboard'}
                </Button>

                <Button variant="outlined" onClick={testHealth} fullWidth>
                  Test Health (Public)
                </Button>

                <Button variant="outlined" onClick={testAlertDashboard} fullWidth>
                  Test Alert Dashboard
                </Button>

                <Button variant="text" onClick={clearResults} fullWidth>
                  Clear Results
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Test Results */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Test Results
              </Typography>

              {results.error && (
                <Alert severity="error" sx={{ mb: 2 }}>
                  <strong>Error:</strong> {results.error}
                </Alert>
              )}

              {results.login && (
                <Paper sx={{ p: 2, mb: 2 }}>
                  <Typography variant="subtitle1" gutterBottom>
                    🔐 Login Response:
                  </Typography>
                  <pre style={{ fontSize: '12px', overflow: 'auto' }}>
                    {JSON.stringify(results.login, null, 2)}
                  </pre>
                </Paper>
              )}

              {results.dashboard && (
                <Paper sx={{ p: 2, mb: 2 }}>
                  <Typography variant="subtitle1" gutterBottom>
                    📊 Dashboard Response:
                  </Typography>
                  <pre style={{ fontSize: '12px', overflow: 'auto' }}>
                    {JSON.stringify(results.dashboard, null, 2)}
                  </pre>
                </Paper>
              )}

              {results.health && (
                <Paper sx={{ p: 2, mb: 2 }}>
                  <Typography variant="subtitle1" gutterBottom>
                    ❤️ Health Response:
                  </Typography>
                  <pre style={{ fontSize: '12px', overflow: 'auto' }}>
                    {JSON.stringify(results.health, null, 2)}
                  </pre>
                </Paper>
              )}

              {results.alertDashboard && (
                <Paper sx={{ p: 2, mb: 2 }}>
                  <Typography variant="subtitle1" gutterBottom>
                    🚨 Alert Dashboard Response:
                  </Typography>
                  <pre style={{ fontSize: '12px', overflow: 'auto' }}>
                    {JSON.stringify(results.alertDashboard, null, 2)}
                  </pre>
                </Paper>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Divider sx={{ my: 3 }} />

      <Typography variant="h6" gutterBottom>
        Test Instructions:
      </Typography>
      <Typography variant="body2" component="div">
        <ol>
          <li>First, run the test user creation script in the database</li>
          <li>Make sure the API is running with global authentication enabled</li>
          <li>Click "Test Health" to verify public endpoints work</li>
          <li>Click "Test Login & Dashboard" to verify authentication flow</li>
          <li>Check the browser console for detailed logs</li>
        </ol>
      </Typography>
    </Box>
  );
};

export default AuthTest;
