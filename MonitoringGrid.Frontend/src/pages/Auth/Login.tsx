import React, { useState } from 'react';
import {
  Box,
  CardContent,
  Typography,
  Alert,
  Checkbox,
  FormControlLabel,
  Link,
  Divider,
  IconButton,
  InputAdornment,
  CircularProgress,
} from '@mui/material';
import { Visibility, VisibilityOff, Microsoft, Google, PlayArrow } from '@mui/icons-material';
import mgLogo from '../../assets/images/mglogo.png';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { Card, InputField, Button } from '@/components';

const loginSchema = yup.object({
  username: yup.string().required('Username is required'),
  password: yup.string().required('Password is required'),
  rememberMe: yup.boolean().default(false),
  twoFactorCode: yup.string().optional(),
});

interface LoginFormData {
  username: string;
  password: string;
  rememberMe: boolean;
  twoFactorCode?: string;
}

export const Login: React.FC = () => {
  const [showPassword, setShowPassword] = useState(false);
  const [requiresTwoFactor, setRequiresTwoFactor] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();

  const [successMessage, setSuccessMessage] = useState<string | null>(
    location.state?.message || null
  );

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: yupResolver(loginSchema),
    defaultValues: {
      rememberMe: false,
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    setError(null);

    try {
      await login({
        username: data.username,
        password: data.password,
        rememberMe: data.rememberMe,
        twoFactorCode: data.twoFactorCode,
      });

      navigate('/dashboard');
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'An unexpected error occurred. Please try again.'
      );
      console.error('Login error:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSsoLogin = async (provider: 'microsoft' | 'google') => {
    setIsLoading(true);
    try {
      // SSO functionality to be implemented
      setError(`${provider} SSO login coming soon`);
    } catch (err) {
      setError(`Failed to initiate ${provider} login`);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDemoMode = () => {
    // Navigate directly to demo without authentication
    navigate('/demo');
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'background.default',
        padding: 2,
      }}
    >
      <Card
        sx={{
          maxWidth: 380,
          width: '100%',
          backgroundColor: 'background.paper',
          boxShadow: 3
        }}
      >
        <CardContent sx={{ p: 3 }}>
          <Box sx={{ textAlign: 'center', mb: 2 }}>
            <Box
              component="img"
              src={mgLogo}
              alt="MonitoringGrid Logo"
              sx={{
                width: '200px',
                maxWidth: '100%',
                mb: 1,
              }}
            />
            <Typography variant="body2" color="text.secondary">
              Sign in to your account
            </Typography>
          </Box>

          {successMessage && (
            <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccessMessage(null)}>
              {successMessage}
            </Alert>
          )}

          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          <form onSubmit={handleSubmit(onSubmit)}>
            <InputField
              {...register('username')}
              fullWidth
              label="Username"
              margin="dense"
              error={!!errors.username}
              helperText={errors.username?.message}
              disabled={isLoading}
              autoComplete="username"
              autoFocus
              sx={{ mb: 1 }}
            />

            <InputField
              {...register('password')}
              fullWidth
              label="Password"
              type={showPassword ? 'text' : 'password'}
              margin="dense"
              error={!!errors.password}
              helperText={errors.password?.message}
              disabled={isLoading}
              autoComplete="current-password"
              sx={{ mb: 1 }}
              InputProps={{
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      onClick={() => setShowPassword(!showPassword)}
                      edge="end"
                      disabled={isLoading}
                    >
                      {showPassword ? <VisibilityOff /> : <Visibility />}
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />

            {requiresTwoFactor && (
              <InputField
                {...register('twoFactorCode')}
                fullWidth
                label="Two-Factor Code"
                margin="dense"
                error={!!errors.twoFactorCode}
                helperText={errors.twoFactorCode?.message}
                disabled={isLoading}
                autoComplete="one-time-code"
                placeholder="Enter 6-digit code"
                sx={{ mb: 1 }}
              />
            )}

            <FormControlLabel
              control={<Checkbox {...register('rememberMe')} disabled={isLoading} />}
              label="Remember me"
              sx={{ mt: 0.5, mb: 1.5 }}
            />

            <Button
              type="submit"
              fullWidth
              gradient="primary"
              size="large"
              disabled={isLoading}
              sx={{ mb: 1.5 }}
            >
              {isLoading ? <CircularProgress size={24} color="inherit" /> : 'Sign In'}
            </Button>

            <Box sx={{ textAlign: 'center', mb: 1 }}>
              <Link
                component="button"
                type="button"
                variant="body2"
                onClick={() => navigate('/auth/forgot-password')}
                disabled={isLoading}
              >
                Forgot your password?
              </Link>
            </Box>

            <Box sx={{ textAlign: 'center', mb: 1.5 }}>
              <Typography variant="body2" color="text.secondary">
                Don't have an account?{' '}
                <Link
                  component="button"
                  type="button"
                  variant="body2"
                  onClick={() => navigate('/register')}
                  disabled={isLoading}
                  sx={{ textDecoration: 'none' }}
                >
                  Sign up here
                </Link>
              </Typography>
            </Box>

            <Divider sx={{ my: 1.5 }}>
              <Typography variant="body2" color="text.secondary">
                Or continue with
              </Typography>
            </Divider>

            <Box sx={{ display: 'flex', gap: 1, mb: 1.5 }}>
              <Button
                fullWidth
                variant="outlined"
                gradient="info"
                startIcon={<Microsoft />}
                onClick={() => handleSsoLogin('microsoft')}
                disabled={isLoading}
                size="small"
              >
                Microsoft
              </Button>
              <Button
                fullWidth
                variant="outlined"
                gradient="warning"
                startIcon={<Google />}
                onClick={() => handleSsoLogin('google')}
                disabled={isLoading}
                size="small"
              >
                Google
              </Button>
            </Box>

            <Divider sx={{ my: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Or explore without signing in
              </Typography>
            </Divider>

            <Button
              fullWidth
              variant="outlined"
              gradient="secondary"
              startIcon={<PlayArrow />}
              onClick={handleDemoMode}
              disabled={isLoading}
            >
              Try Demo Mode
            </Button>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
};

export default Login;
