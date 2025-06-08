import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  TextField,
  Button,
  Typography,
  Alert,
  Checkbox,
  FormControlLabel,
  Link,
  Divider,
  IconButton,
  InputAdornment,
  CircularProgress
} from '@mui/material';
import {
  Visibility,
  VisibilityOff,
  Microsoft,
  Google
} from '@mui/icons-material';
import mgLogo from '../../assets/images/mglogo.png';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

const loginSchema = yup.object({
  username: yup.string().required('Username is required'),
  password: yup.string().required('Password is required'),
  rememberMe: yup.boolean().default(false),
  twoFactorCode: yup.string().optional()
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
    formState: { errors }
  } = useForm<LoginFormData>({
    resolver: yupResolver(loginSchema),
    defaultValues: {
      rememberMe: false
    }
  });

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    setError(null);

    try {
      await login({
        username: data.username,
        password: data.password,
        rememberMe: data.rememberMe,
        twoFactorCode: data.twoFactorCode
      });

      navigate('/dashboard');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unexpected error occurred. Please try again.');
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

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        padding: 2
      }}
    >
      <Card sx={{ maxWidth: 400, width: '100%' }}>
        <CardContent sx={{ p: 4 }}>
          <Box sx={{ textAlign: 'center', mb: 3 }}>
            <Box
              component="img"
              src={mgLogo}
              alt="MonitoringGrid Logo"
              sx={{
                width: '100%',
                mb: 2
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
            <TextField
              {...register('username')}
              fullWidth
              label="Username"
              margin="normal"
              error={!!errors.username}
              helperText={errors.username?.message}
              disabled={isLoading}
              autoComplete="username"
              autoFocus
            />

            <TextField
              {...register('password')}
              fullWidth
              label="Password"
              type={showPassword ? 'text' : 'password'}
              margin="normal"
              error={!!errors.password}
              helperText={errors.password?.message}
              disabled={isLoading}
              autoComplete="current-password"
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
                )
              }}
            />

            {requiresTwoFactor && (
              <TextField
                {...register('twoFactorCode')}
                fullWidth
                label="Two-Factor Code"
                margin="normal"
                error={!!errors.twoFactorCode}
                helperText={errors.twoFactorCode?.message}
                disabled={isLoading}
                autoComplete="one-time-code"
                placeholder="Enter 6-digit code"
              />
            )}

            <FormControlLabel
              control={
                <Checkbox
                  {...register('rememberMe')}
                  disabled={isLoading}
                />
              }
              label="Remember me"
              sx={{ mt: 1, mb: 2 }}
            />

            <Button
              type="submit"
              fullWidth
              variant="contained"
              size="large"
              disabled={isLoading}
              sx={{ mb: 2 }}
            >
              {isLoading ? (
                <CircularProgress size={24} color="inherit" />
              ) : (
                'Sign In'
              )}
            </Button>

            <Box sx={{ textAlign: 'center', mb: 2 }}>
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

            <Box sx={{ textAlign: 'center', mb: 2 }}>
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

            <Divider sx={{ my: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Or continue with
              </Typography>
            </Divider>

            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button
                fullWidth
                variant="outlined"
                startIcon={<Microsoft />}
                onClick={() => handleSsoLogin('microsoft')}
                disabled={isLoading}
              >
                Microsoft
              </Button>
              <Button
                fullWidth
                variant="outlined"
                startIcon={<Google />}
                onClick={() => handleSsoLogin('google')}
                disabled={isLoading}
              >
                Google
              </Button>
            </Box>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
};

export default Login;
