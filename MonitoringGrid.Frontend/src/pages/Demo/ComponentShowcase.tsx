import React, { useState } from 'react';
import {
  Box,
  Container,
  Typography,
  Grid,
  Paper,
  Divider,
  Card,
  CardContent,
  CardHeader,
  Button,
  Chip,
  alpha,
  useTheme,
  styled,
  keyframes,
} from '@mui/material';
import {
  Dashboard,
  TrendingUp,
  TrendingDown,
  Warning,
  Error,
  CheckCircle,
  Add,
  Refresh,
  Download,
  Edit,
  Delete,
  Visibility,
} from '@mui/icons-material';
import PageHeader from '../../components/Common/PageHeader';

// Enhanced animations
const pulse = keyframes`
  0% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.05); opacity: 0.8; }
  100% { transform: scale(1); opacity: 1; }
`;

const slideIn = keyframes`
  from { transform: translateY(20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
`;

// Enhanced styled components
const EnhancedCard = styled(Card)(({ theme }) => ({
  borderRadius: '16px',
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  cursor: 'pointer',
  '&:hover': {
    transform: 'translateY(-4px)',
    boxShadow: theme.shadows[8],
  },
  animation: `${slideIn} 0.6s ease-out`,
}));

const EnhancedButton = styled(Button)(({ theme }) => ({
  borderRadius: '12px',
  fontWeight: 600,
  textTransform: 'none',
  padding: '12px 24px',
  transition: 'all 0.3s ease',
  '&:hover': {
    transform: 'translateY(-2px)',
  },
}));

const AnimatedChip = styled(Chip)(({ theme }) => ({
  fontWeight: 600,
  borderRadius: '8px',
  transition: 'all 0.3s ease',
  '&:hover': {
    transform: 'scale(1.05)',
  },
  '&.pulse': {
    animation: `${pulse} 2s ease-in-out infinite`,
  },
}));

const ComponentShowcase: React.FC = () => {
  const theme = useTheme();
  const [loading, setLoading] = useState(false);

  return (
    <Container maxWidth="xl" sx={{ py: 4 }}>
      <PageHeader
        title="Enhanced Component Showcase"
        subtitle="Modern UI components with animations and enhanced styling"
        icon={<Dashboard />}
        actions={[
          {
            label: loading ? 'Stop Loading' : 'Show Loading',
            onClick: () => setLoading(!loading),
            variant: 'outlined',
          },
        ]}
      />

      {/* Enhanced Cards Section */}
      <Box sx={{ mb: 6 }}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Enhanced Cards with Animations
        </Typography>
        <Grid container spacing={3}>
          <Grid item xs={12} md={4}>
            <EnhancedCard>
              <CardHeader
                avatar={
                  <Box
                    sx={{
                      width: 48,
                      height: 48,
                      borderRadius: '12px',
                      backgroundColor: alpha(theme.palette.primary.main, 0.1),
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      color: theme.palette.primary.main,
                    }}
                  >
                    <Dashboard />
                  </Box>
                }
                title={
                  <Typography variant="h6" fontWeight={600}>
                    Total KPIs
                  </Typography>
                }
                subheader="Active monitoring indicators"
              />
              <CardContent>
                <Typography variant="h4" fontWeight={700} color="primary" sx={{ mb: 1 }}>
                  24
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <TrendingUp fontSize="small" color="success" />
                  <Typography variant="body2" color="success.main" fontWeight={600}>
                    +9.1%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    vs 22
                  </Typography>
                </Box>
              </CardContent>
            </EnhancedCard>
          </Grid>
          <Grid item xs={12} md={4}>
            <EnhancedCard
              sx={{
                background: `linear-gradient(135deg, ${alpha(theme.palette.warning.main, 0.1)} 0%, ${alpha(theme.palette.warning.main, 0.05)} 100%)`,
                border: `1px solid ${alpha(theme.palette.warning.main, 0.2)}`,
              }}
            >
              <CardHeader
                avatar={
                  <Box
                    sx={{
                      width: 48,
                      height: 48,
                      borderRadius: '12px',
                      backgroundColor: alpha(theme.palette.warning.main, 0.1),
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      color: theme.palette.warning.main,
                    }}
                  >
                    <Warning />
                  </Box>
                }
                title={
                  <Typography variant="h6" fontWeight={600}>
                    Active Alerts
                  </Typography>
                }
                subheader="Requires attention"
              />
              <CardContent>
                <Typography variant="h4" fontWeight={700} color="warning.main" sx={{ mb: 1 }}>
                  3
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <TrendingDown fontSize="small" color="success" />
                  <Typography variant="body2" color="success.main" fontWeight={600}>
                    -40%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    vs 5
                  </Typography>
                </Box>
              </CardContent>
            </EnhancedCard>
          </Grid>
          <Grid item xs={12} md={4}>
            <EnhancedCard
              sx={{
                border: `2px solid ${theme.palette.success.main}`,
                backgroundColor: 'transparent',
              }}
            >
              <CardHeader
                avatar={
                  <Box
                    sx={{
                      width: 48,
                      height: 48,
                      borderRadius: '12px',
                      backgroundColor: alpha(theme.palette.success.main, 0.1),
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      color: theme.palette.success.main,
                    }}
                  >
                    <CheckCircle />
                  </Box>
                }
                title={
                  <Typography variant="h6" fontWeight={600}>
                    System Health
                  </Typography>
                }
                subheader="Overall system status"
              />
              <CardContent>
                <Typography variant="h4" fontWeight={700} color="success.main" sx={{ mb: 1 }}>
                  98.5%
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <TrendingUp fontSize="small" color="success" />
                  <Typography variant="body2" color="success.main" fontWeight={600}>
                    +2.3%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    this week
                  </Typography>
                </Box>
              </CardContent>
            </EnhancedCard>
          </Grid>
        </Grid>
      </Box>

      {/* Enhanced Buttons Section */}
      <Box sx={{ mb: 6 }}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Enhanced Buttons with Hover Effects
        </Typography>
        <Paper sx={{ p: 3, borderRadius: '16px' }}>
          <Grid container spacing={2} alignItems="center">
            <Grid item>
              <EnhancedButton
                variant="contained"
                startIcon={<Add />}
                disabled={loading}
                sx={{
                  background: `linear-gradient(135deg, ${theme.palette.primary.main} 0%, ${theme.palette.primary.dark} 100%)`,
                  boxShadow: `0 4px 12px ${alpha(theme.palette.primary.main, 0.3)}`,
                  '&:hover': {
                    boxShadow: `0 6px 16px ${alpha(theme.palette.primary.main, 0.4)}`,
                  },
                }}
              >
                {loading ? 'Creating...' : 'Create KPI'}
              </EnhancedButton>
            </Grid>
            <Grid item>
              <EnhancedButton
                variant="contained"
                color="secondary"
                startIcon={<TrendingUp />}
                sx={{
                  borderRadius: '50px',
                  background: `linear-gradient(135deg, ${theme.palette.secondary.main} 0%, ${theme.palette.secondary.dark} 100%)`,
                }}
              >
                Gradient Button
              </EnhancedButton>
            </Grid>
            <Grid item>
              <EnhancedButton
                variant="contained"
                color="warning"
                startIcon={<Warning />}
                sx={{
                  backgroundColor: alpha(theme.palette.warning.main, 0.1),
                  color: theme.palette.warning.main,
                  border: `1px solid ${alpha(theme.palette.warning.main, 0.2)}`,
                  '&:hover': {
                    backgroundColor: alpha(theme.palette.warning.main, 0.15),
                    borderColor: alpha(theme.palette.warning.main, 0.3),
                  },
                }}
              >
                Soft Warning
              </EnhancedButton>
            </Grid>
            <Grid item>
              <EnhancedButton
                variant="outlined"
                endIcon={<Refresh />}
                size="large"
                sx={{
                  borderWidth: '2px',
                  '&:hover': {
                    borderWidth: '2px',
                    boxShadow: `0 4px 12px ${alpha(theme.palette.primary.main, 0.2)}`,
                  },
                }}
              >
                Refresh Data
              </EnhancedButton>
            </Grid>
            <Grid item>
              <EnhancedButton variant="text" startIcon={<Download />} size="small">
                Export
              </EnhancedButton>
            </Grid>
          </Grid>
        </Paper>
      </Box>

      {/* Status Indicators Section */}
      <Box sx={{ mb: 6 }}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Enhanced Status Indicators with Animations
        </Typography>
        <Paper sx={{ p: 3, borderRadius: '16px' }}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                Animated Chip Variants
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
                <AnimatedChip
                  icon={<CheckCircle />}
                  label="Healthy"
                  color="success"
                  sx={{ fontWeight: 600 }}
                />
                <AnimatedChip
                  icon={<Warning />}
                  label="Warning"
                  color="warning"
                  sx={{ fontWeight: 600 }}
                />
                <AnimatedChip
                  icon={<Error />}
                  label="Error"
                  color="error"
                  className="pulse"
                  sx={{ fontWeight: 600 }}
                />
                <AnimatedChip
                  icon={<Dashboard />}
                  label="Running"
                  color="info"
                  sx={{ fontWeight: 600 }}
                />
                <AnimatedChip
                  icon={<Dashboard />}
                  label="Never Run"
                  color="secondary"
                  sx={{ fontWeight: 600 }}
                />
              </Box>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                Badge Variants
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
                <Box
                  sx={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: 1,
                    px: 1.5,
                    py: 0.5,
                    borderRadius: '12px',
                    backgroundColor: alpha(theme.palette.success.main, 0.1),
                    color: theme.palette.success.main,
                    fontSize: '0.875rem',
                    fontWeight: 600,
                    border: `1px solid ${alpha(theme.palette.success.main, 0.2)}`,
                  }}
                >
                  <CheckCircle fontSize="small" />
                  Healthy
                </Box>
                <Box
                  sx={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: 1,
                    px: 1.5,
                    py: 0.5,
                    borderRadius: '12px',
                    backgroundColor: alpha(theme.palette.warning.main, 0.1),
                    color: theme.palette.warning.main,
                    fontSize: '0.875rem',
                    fontWeight: 600,
                    border: `1px solid ${alpha(theme.palette.warning.main, 0.2)}`,
                  }}
                >
                  <Warning fontSize="small" />
                  Warning
                </Box>
                <Box
                  sx={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: 1,
                    px: 1.5,
                    py: 0.5,
                    borderRadius: '12px',
                    backgroundColor: alpha(theme.palette.error.main, 0.1),
                    color: theme.palette.error.main,
                    fontSize: '0.875rem',
                    fontWeight: 600,
                    border: `1px solid ${alpha(theme.palette.error.main, 0.2)}`,
                  }}
                >
                  <Error fontSize="small" />
                  Error
                </Box>
              </Box>
            </Grid>
          </Grid>
        </Paper>
      </Box>

      {/* Design System Demo */}
      <Box sx={{ mb: 6 }}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Design System Showcase
        </Typography>
        <Paper sx={{ p: 3, borderRadius: '16px' }}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                Color Palette
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {[
                  { name: 'primary', color: theme.palette.primary.main },
                  { name: 'secondary', color: theme.palette.secondary.main },
                  { name: 'success', color: theme.palette.success.main },
                  { name: 'warning', color: theme.palette.warning.main },
                  { name: 'error', color: theme.palette.error.main },
                  { name: 'info', color: theme.palette.info.main },
                ].map(({ name, color }) => (
                  <Box
                    key={name}
                    sx={{
                      width: 60,
                      height: 60,
                      borderRadius: '12px',
                      backgroundColor: color,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      color: 'white',
                      fontSize: '0.75rem',
                      fontWeight: 600,
                      textTransform: 'capitalize',
                      transition: 'transform 0.2s ease',
                      '&:hover': {
                        transform: 'scale(1.1)',
                      },
                    }}
                  >
                    {name}
                  </Box>
                ))}
              </Box>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                Typography Scale
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Typography variant="h4" fontWeight={700}>
                  Heading 4
                </Typography>
                <Typography variant="h5" fontWeight={600}>
                  Heading 5
                </Typography>
                <Typography variant="h6" fontWeight={600}>
                  Heading 6
                </Typography>
                <Typography variant="body1">Body 1 - Regular text</Typography>
                <Typography variant="body2" color="text.secondary">
                  Body 2 - Secondary text
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Caption text
                </Typography>
              </Box>
            </Grid>
          </Grid>
        </Paper>
      </Box>

      <Divider sx={{ my: 4 }} />

      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography variant="h6" gutterBottom fontWeight={600}>
          🎨 Enhanced UI Components
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>
          MonitoringGrid Design System v2.0
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Modern components with animations, enhanced styling, and improved user experience
        </Typography>
      </Box>
    </Container>
  );
};

export default ComponentShowcase;
