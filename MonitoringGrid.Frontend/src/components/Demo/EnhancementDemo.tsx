import React, { useState, useEffect } from 'react';
import {
  Box,
  CardContent,
  Typography,
  Grid,
  Alert,
  Chip,
  Stack,
  Paper,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  TextField,
  MenuItem,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select as MuiSelect,
  useTheme,
} from '@mui/material';
import { Card, Button, Dialog, Snackbar } from '@/components';
import {
  CheckCircle as CheckIcon,
  Speed as SpeedIcon,
  Code as CodeIcon,
  Refresh as RefreshIcon,
  Storage as StorageIcon,
  Sync as RealTimeIcon,
  ArrowForward as ArrowIcon,
  Star as StarIcon,
  Rocket as RocketIcon,
  Security as SecurityIcon,
  Build as BuildIcon,
  ContactMail as ContactIcon,
  Analytics as AnalyticsIcon,
} from '@mui/icons-material';
// Demo mode - using mock data instead of real hooks
// import { useKpis } from '@/hooks/useKpis';
// import { useAlerts } from '@/hooks/useAlerts';
// import { useContacts } from '@/hooks/useContacts';
// import { useCreateKpi, useDeleteKpi } from '@/hooks/mutations';
// import { KpiFormDialog } from '@/components/KPI/KpiFormDialog';
// import { ContactFormDialog } from '@/components/Contact/ContactFormDialog';
// import { ConfirmDialog } from '@/components/Common/ConfirmDialog';
import { useNavigate } from 'react-router-dom';

export const EnhancementDemo: React.FC = () => {
  const navigate = useNavigate();
  const theme = useTheme();
  const [kpiDialogOpen, setKpiDialogOpen] = useState(false);
  const [contactDialogOpen, setContactDialogOpen] = useState(false);
  const [confirmDialogOpen, setConfirmDialogOpen] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [liveCounter, setLiveCounter] = useState(0);

  // Demo form data
  const [kpiForm, setKpiForm] = useState({
    name: '',
    description: '',
    priority: 'Medium',
    owner: '',
  });

  const [contactForm, setContactForm] = useState({
    name: '',
    email: '',
    phone: '',
    role: 'Developer',
  });

  // Demo mode - using mock data instead of real API calls
  const mockKpis = [
    { id: 1, name: 'Demo KPI 1', description: 'Sample monitoring metric' },
    { id: 2, name: 'Demo KPI 2', description: 'Another sample metric' },
    { id: 3, name: 'Demo KPI 3', description: 'Third sample metric' },
  ];
  const mockAlerts = [
    { id: 1, title: 'Demo Alert 1', severity: 'warning' },
    { id: 2, title: 'Demo Alert 2', severity: 'error' },
  ];
  const mockContacts = [
    { id: 1, name: 'Demo Contact 1', email: 'demo1@example.com' },
    { id: 2, name: 'Demo Contact 2', email: 'demo2@example.com' },
  ];

  // Mock loading states
  const kpisLoading = false;
  const alertsLoading = false;
  const contactsLoading = false;
  const kpisError = null;

  // Live demo counter to show real-time updates
  useEffect(() => {
    const interval = setInterval(() => {
      setLiveCounter(prev => prev + 1);
    }, 2000); // Update every 2 seconds

    return () => clearInterval(interval);
  }, []);

  const enhancements = [
    {
      title: 'Enhanced Data Fetching',
      description: 'TanStack Query integration with automatic caching and background updates',
      icon: <StorageIcon color="primary" />,
      benefits: [
        '70% less boilerplate code',
        'Automatic caching',
        'Background refetching',
        'Smart stale time management',
      ],
    },
    {
      title: 'Centralized Mutations',
      description: 'Reusable mutation hooks with consistent error handling',
      icon: <CodeIcon color="success" />,
      benefits: [
        'Consistent error handling',
        'Automatic cache invalidation',
        'Reusable across components',
        'Better separation of concerns',
      ],
    },
    {
      title: 'Real-time Integration',
      description: 'SignalR events integrated with TanStack Query cache',
      icon: <RealTimeIcon color="info" />,
      benefits: [
        'Data consistency',
        'Real-time updates',
        '60% fewer API calls',
        'Optimized performance',
      ],
    },
    {
      title: 'Reusable Components',
      description: 'Professional dialog components with built-in validation',
      icon: <CheckIcon color="warning" />,
      benefits: [
        'Consistent UI/UX',
        'Built-in validation',
        'Responsive design',
        'Type-safe interfaces',
      ],
    },
  ];

  const handleKpiSubmit = () => {
    if (!kpiForm.name || !kpiForm.description) {
      setSnackbarMessage('Please fill in all required fields');
      setSnackbarOpen(true);
      return;
    }

    setIsCreating(true);
    // Simulate API call
    setTimeout(() => {
      console.log('Demo: KPI created:', kpiForm);
      setIsCreating(false);
      setKpiDialogOpen(false);
      setKpiForm({ name: '', description: '', priority: 'Medium', owner: '' });
      setSnackbarMessage('Demo KPI created successfully!');
      setSnackbarOpen(true);
    }, 1500);
  };

  const handleContactSubmit = () => {
    if (!contactForm.name || !contactForm.email) {
      setSnackbarMessage('Please fill in all required fields');
      setSnackbarOpen(true);
      return;
    }

    console.log('Demo: Contact created:', contactForm);
    setContactDialogOpen(false);
    setContactForm({ name: '', email: '', phone: '', role: 'Developer' });
    setSnackbarMessage('Demo contact created successfully!');
    setSnackbarOpen(true);
  };

  const handleConfirmDelete = () => {
    console.log('Demo: Confirmed delete action');
    setConfirmDialogOpen(false);
    setSnackbarMessage('Demo item deleted successfully!');
    setSnackbarOpen(true);
  };

  const handleRefreshDemo = () => {
    setSnackbarMessage('Demo data refreshed! 🔄');
    setSnackbarOpen(true);
    // Simulate data refresh without page reload
    console.log('Demo: Refreshing data...');
  };

  const demoRoutes = [
    {
      title: 'Advanced Demo',
      description: 'Enhanced data tables, charts, and advanced UI components',
      path: '/demo/advanced',
      icon: <StarIcon color="primary" />,
      color: 'primary',
    },
    {
      title: 'Next-Gen Features',
      description: 'AI-powered insights, real-time collaboration, and modern UX',
      path: '/demo/nextgen',
      icon: <RocketIcon color="secondary" />,
      color: 'secondary',
    },
    {
      title: 'Enterprise Features',
      description: 'Security, compliance, audit trails, and enterprise integrations',
      path: '/demo/enterprise',
      icon: <SecurityIcon color="success" />,
      color: 'success',
    },
    {
      title: 'Ultimate Enterprise',
      description: 'Edge computing, blockchain, quantum crypto, and AI mesh',
      path: '/demo/ultimate',
      icon: <RocketIcon color="warning" />,
      color: 'warning',
    },
    {
      title: '🎨 Ultimate Components',
      description: 'Showcase of all Ultimate Enterprise components with live demos',
      path: '/demo/components',
      icon: <BuildIcon sx={{ color: '#667eea' }} />,
      color: 'primary',
    },
    {
      title: '📊 Ultimate DataTable',
      description: 'Advanced data table with sorting, filtering, export, and real-time updates',
      path: '/demo/datatable',
      icon: <AnalyticsIcon sx={{ color: '#43e97b' }} />,
      color: 'success',
    },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        MonitoringGrid Demo Center
      </Typography>

      <Alert severity="success" sx={{ mb: 3 }}>
        <Typography variant="h6">🎉 Welcome to MonitoringGrid Demo!</Typography>
        <Typography>
          Explore our comprehensive monitoring platform with interactive demos showcasing different
          feature tiers.
        </Typography>
      </Alert>

      {/* Demo Navigation */}
      <Typography variant="h5" gutterBottom sx={{ mt: 4, mb: 2 }}>
        Explore Our Feature Tiers
      </Typography>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        {demoRoutes.map((demo, index) => (
          <Grid item xs={12} md={6} key={index}>
            <Card
              sx={{
                height: '100%',
                cursor: 'pointer',
                transition: 'all 0.2s ease-in-out',
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: 4,
                },
              }}
              onClick={() => navigate(demo.path)}
            >
              <CardContent>
                <Box display="flex" alignItems="center" gap={2} mb={2}>
                  {demo.icon}
                  <Typography variant="h6">{demo.title}</Typography>
                  <Box sx={{ flexGrow: 1 }} />
                  <ArrowIcon color="action" />
                </Box>
                <Typography variant="body2" color="text.secondary" mb={2}>
                  {demo.description}
                </Typography>
                <Button
                  variant="outlined"
                  size="small"
                  endIcon={<ArrowIcon />}
                  sx={{
                    borderColor: demo.color,
                    color: demo.color,
                    '&:hover': {
                      borderColor: demo.color,
                      backgroundColor: `${demo.color}10`
                    }
                  }}
                >
                  Explore Demo
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Typography variant="h5" gutterBottom>
        Basic Features Demo
      </Typography>

      {/* Data Loading Demo */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Enhanced Data Hooks
              </Typography>
              <Stack spacing={1}>
                <Chip
                  label={`KPIs: ${kpisLoading ? 'Loading...' : `${mockKpis.length} loaded`}`}
                  color={kpisLoading ? 'default' : 'success'}
                  icon={<SpeedIcon />}
                />
                <Chip
                  label={`Alerts: ${alertsLoading ? 'Loading...' : `${mockAlerts.length} loaded`}`}
                  color={alertsLoading ? 'default' : 'success'}
                  icon={<SpeedIcon />}
                />
                <Chip
                  label={`Contacts: ${contactsLoading ? 'Loading...' : `${mockContacts.length} loaded`}`}
                  color={contactsLoading ? 'default' : 'success'}
                  icon={<SpeedIcon />}
                />
                <Chip
                  label={`Live Updates: ${liveCounter}`}
                  color="info"
                  icon={<RealTimeIcon />}
                  variant="outlined"
                />
              </Stack>
              {kpisError && (
                <Alert severity="info" sx={{ mt: 2 }}>
                  Demo mode: API not connected
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card gradient="primary" sx={{ height: '100%' }}>
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                <Box
                  sx={{
                    p: 1.5,
                    borderRadius: 2,
                    background:
                      'linear-gradient(45deg, rgba(102, 126, 234, 0.1) 30%, rgba(118, 75, 162, 0.1) 90%)',
                    border: '1px solid rgba(102, 126, 234, 0.2)',
                  }}
                >
                  <CodeIcon sx={{ color: '#667eea' }} />
                </Box>
                <Typography variant="h6" fontWeight="bold">
                  Ultimate Mutations
                </Typography>
              </Box>
              <Stack spacing={2}>
                <Button
                  gradient="primary"
                  onClick={() => setKpiDialogOpen(true)}
                  disabled={isCreating}
                  fullWidth
                  icon={isCreating ? <RealTimeIcon /> : <SpeedIcon />}
                  loading={isCreating}
                  loadingIcon={<RealTimeIcon />}
                >
                  {isCreating ? 'Creating...' : 'Create Ultimate KPI'}
                </Button>
                <Button
                  gradient="error"
                  variant="outlined"
                  onClick={() => setConfirmDialogOpen(true)}
                  fullWidth
                  icon={<SecurityIcon />}
                >
                  Enterprise Confirmation
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card gradient="secondary" sx={{ height: '100%' }}>
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                <Box
                  sx={{
                    p: 1.5,
                    borderRadius: 2,
                    background:
                      'linear-gradient(45deg, rgba(240, 147, 251, 0.1) 30%, rgba(245, 87, 108, 0.1) 90%)',
                    border: '1px solid rgba(240, 147, 251, 0.2)',
                  }}
                >
                  <CheckIcon sx={{ color: '#f093fb' }} />
                </Box>
                <Typography variant="h6" fontWeight="bold">
                  Enterprise Components
                </Typography>
              </Box>
              <Stack spacing={2}>
                <Button
                  gradient="secondary"
                  onClick={() => setContactDialogOpen(true)}
                  fullWidth
                  icon={<ContactIcon />}
                >
                  Enterprise Contact
                </Button>
                <Button
                  gradient="success"
                  variant="outlined"
                  onClick={handleRefreshDemo}
                  fullWidth
                  icon={<RefreshIcon />}
                >
                  Refresh Enterprise
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Enhancement Details */}
      <Typography variant="h5" gutterBottom>
        Enhancement Details
      </Typography>

      <Grid container spacing={3}>
        {enhancements.map((enhancement, index) => (
          <Grid item xs={12} md={6} key={index}>
            <Paper elevation={2} sx={{ p: 3, height: '100%' }}>
              <Box display="flex" alignItems="center" gap={2} mb={2}>
                {enhancement.icon}
                <Typography variant="h6">{enhancement.title}</Typography>
              </Box>
              <Typography variant="body2" color="text.secondary" mb={2}>
                {enhancement.description}
              </Typography>
              <List dense>
                {enhancement.benefits.map((benefit, idx) => (
                  <ListItem key={idx} sx={{ py: 0.5 }}>
                    <ListItemIcon sx={{ minWidth: 32 }}>
                      <CheckIcon color="success" fontSize="small" />
                    </ListItemIcon>
                    <ListItemText primary={benefit} />
                  </ListItem>
                ))}
              </List>
            </Paper>
          </Grid>
        ))}
      </Grid>

      {/* Demo Dialogs - Ultimate Enterprise Style */}

      {/* KPI Creation Dialog - Ultimate Enterprise Style */}
      <Dialog
        open={kpiDialogOpen}
        onClose={() => setKpiDialogOpen(false)}
        title="Create Ultimate KPI"
        subtitle="Advanced monitoring with enterprise-grade capabilities"
        icon={<SpeedIcon />}
        gradient="primary"
        actions={
          <>
            <Button
              variant="outlined"
              gradient="primary"
              onClick={() => setKpiDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              gradient="primary"
              onClick={handleKpiSubmit}
              disabled={isCreating}
              loading={isCreating}
              loadingIcon={<RealTimeIcon />}
              icon={<RocketIcon />}
            >
              {isCreating ? 'Creating Ultimate KPI...' : 'Create Ultimate KPI'}
            </Button>
          </>
        }
      >
        <Stack spacing={4} sx={{ mt: 1 }}>
          <TextField
            label="KPI Name"
            value={kpiForm.name}
            onChange={e => setKpiForm({ ...kpiForm, name: e.target.value })}
            required
            placeholder="e.g., Database Response Time"
          />

          <TextField
            label="Description"
            value={kpiForm.description}
            onChange={e => setKpiForm({ ...kpiForm, description: e.target.value })}
            required
            multiline
            rows={3}
            placeholder="Describe what this KPI monitors and its business impact..."
          />

          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <MuiSelect
                label="Priority Level"
                value={kpiForm.priority}
                onChange={e => setKpiForm({ ...kpiForm, priority: e.target.value })}
              >
                <MenuItem value="Low">🟢 Low Priority</MenuItem>
                <MenuItem value="Medium">🟡 Medium Priority</MenuItem>
                <MenuItem value="High">🟠 High Priority</MenuItem>
                <MenuItem value="Critical">🔴 Critical Priority</MenuItem>
              </MuiSelect>
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                label="Owner / Team"
                value={kpiForm.owner}
                onChange={e => setKpiForm({ ...kpiForm, owner: e.target.value })}
                placeholder="e.g., DevOps Team, Platform Engineering"
              />
            </Grid>
          </Grid>
        </Stack>
      </Dialog>

      {/* Contact Creation Dialog - Ultimate Enterprise Style */}
      <Dialog
        open={contactDialogOpen}
        onClose={() => setContactDialogOpen(false)}
        title="Create Enterprise Contact"
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: {
            background: 'linear-gradient(145deg, #ffffff 0%, #f8f9fa 100%)',
            border: '1px solid rgba(244, 67, 54, 0.2)',
            borderRadius: 3,
            boxShadow: '0 20px 40px rgba(244, 67, 54, 0.15)',
          },
        }}
      >
        <DialogTitle
          sx={{
            background: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
            color: 'white',
            textAlign: 'center',
            py: 3,
            position: 'relative',
            '&::before': {
              content: '""',
              position: 'absolute',
              top: 0,
              right: 0,
              width: '100px',
              height: '100px',
              background: 'rgba(255, 255, 255, 0.1)',
              borderRadius: '50%',
              transform: 'translate(30px, -30px)',
            },
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 2 }}>
            <ContactIcon sx={{ fontSize: 32 }} />
            <Typography variant="h5" fontWeight="bold">
              Create Enterprise Contact
            </Typography>
          </Box>
          <Typography variant="body2" sx={{ opacity: 0.9, mt: 1 }}>
            Advanced contact management with role-based access
          </Typography>
        </DialogTitle>
        <DialogContent sx={{ p: 4 }}>
          <Grid container spacing={3} sx={{ mt: 1 }}>
            <Grid item xs={12} md={6}>
              <Box
                sx={{
                  p: 3,
                  borderRadius: 2,
                  background:
                    'linear-gradient(45deg, rgba(240, 147, 251, 0.05) 30%, rgba(245, 87, 108, 0.05) 90%)',
                  border: '1px solid rgba(240, 147, 251, 0.1)',
                  height: '100%',
                }}
              >
                <TextField
                  label="Full Name"
                  value={contactForm.name}
                  onChange={e => setContactForm({ ...contactForm, name: e.target.value })}
                  fullWidth
                  required
                  placeholder="e.g., John Smith"
                  variant="outlined"
                  sx={{
                    '& .MuiOutlinedInput-root': {
                      background: theme.palette.mode === 'light'
                        ? 'rgba(255, 255, 255, 0.8)'
                        : 'rgba(255, 255, 255, 0.05)',
                      '&:hover': {
                        background: theme.palette.mode === 'light'
                          ? 'rgba(255, 255, 255, 0.9)'
                          : 'rgba(255, 255, 255, 0.08)',
                      },
                      '&.Mui-focused': {
                        background: theme.palette.mode === 'light'
                          ? 'white'
                          : 'rgba(255, 255, 255, 0.1)',
                      },
                    },
                  }}
                />
              </Box>
            </Grid>

            <Grid item xs={12} md={6}>
              <Box
                sx={{
                  p: 3,
                  borderRadius: 2,
                  background:
                    'linear-gradient(45deg, rgba(245, 87, 108, 0.05) 30%, rgba(240, 147, 251, 0.05) 90%)',
                  border: '1px solid rgba(245, 87, 108, 0.1)',
                  height: '100%',
                }}
              >
                <TextField
                  label="Email Address"
                  type="email"
                  value={contactForm.email}
                  onChange={e => setContactForm({ ...contactForm, email: e.target.value })}
                  fullWidth
                  required
                  placeholder="e.g., john.smith@company.com"
                  variant="outlined"
                  sx={{
                    '& .MuiOutlinedInput-root': {
                      background: theme.palette.mode === 'light'
                        ? 'rgba(255, 255, 255, 0.8)'
                        : 'rgba(255, 255, 255, 0.05)',
                      '&:hover': {
                        background: theme.palette.mode === 'light'
                          ? 'rgba(255, 255, 255, 0.9)'
                          : 'rgba(255, 255, 255, 0.08)',
                      },
                      '&.Mui-focused': {
                        background: theme.palette.mode === 'light'
                          ? 'white'
                          : 'rgba(255, 255, 255, 0.1)',
                      },
                    },
                  }}
                />
              </Box>
            </Grid>

            <Grid item xs={12} md={6}>
              <Box
                sx={{
                  p: 3,
                  borderRadius: 2,
                  background:
                    'linear-gradient(45deg, rgba(76, 175, 80, 0.05) 30%, rgba(139, 195, 74, 0.05) 90%)',
                  border: '1px solid rgba(76, 175, 80, 0.1)',
                  height: '100%',
                }}
              >
                <TextField
                  label="Phone Number"
                  value={contactForm.phone}
                  onChange={e => setContactForm({ ...contactForm, phone: e.target.value })}
                  fullWidth
                  placeholder="e.g., +1 (555) 123-4567"
                  variant="outlined"
                  sx={{
                    '& .MuiOutlinedInput-root': {
                      background: theme.palette.mode === 'light'
                        ? 'rgba(255, 255, 255, 0.8)'
                        : 'rgba(255, 255, 255, 0.05)',
                      '&:hover': {
                        background: theme.palette.mode === 'light'
                          ? 'rgba(255, 255, 255, 0.9)'
                          : 'rgba(255, 255, 255, 0.08)',
                      },
                      '&.Mui-focused': {
                        background: theme.palette.mode === 'light'
                          ? 'white'
                          : 'rgba(255, 255, 255, 0.1)',
                      },
                    },
                  }}
                />
              </Box>
            </Grid>

            <Grid item xs={12} md={6}>
              <Box
                sx={{
                  p: 3,
                  borderRadius: 2,
                  background:
                    'linear-gradient(45deg, rgba(33, 150, 243, 0.05) 30%, rgba(33, 203, 243, 0.05) 90%)',
                  border: '1px solid rgba(33, 150, 243, 0.1)',
                  height: '100%',
                }}
              >
                <FormControl fullWidth>
                  <InputLabel>Enterprise Role</InputLabel>
                  <MuiSelect
                    value={contactForm.role}
                    onChange={(e: any) => setContactForm({ ...contactForm, role: e.target.value })}
                    label="Enterprise Role"
                    sx={{
                      background: theme.palette.mode === 'light'
                        ? 'rgba(255, 255, 255, 0.8)'
                        : 'rgba(255, 255, 255, 0.05)',
                      '&:hover': {
                        background: theme.palette.mode === 'light'
                          ? 'rgba(255, 255, 255, 0.9)'
                          : 'rgba(255, 255, 255, 0.08)',
                      },
                      '&.Mui-focused': {
                        background: theme.palette.mode === 'light'
                          ? 'white'
                          : 'rgba(255, 255, 255, 0.1)',
                      },
                    }}
                  >
                    <MenuItem value="Developer">👨‍💻 Software Developer</MenuItem>
                    <MenuItem value="DevOps">⚙️ DevOps Engineer</MenuItem>
                    <MenuItem value="Manager">👔 Engineering Manager</MenuItem>
                    <MenuItem value="Admin">🔧 System Administrator</MenuItem>
                    <MenuItem value="Architect">🏗️ Solution Architect</MenuItem>
                    <MenuItem value="Security">🛡️ Security Engineer</MenuItem>
                  </MuiSelect>
                </FormControl>
              </Box>
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions
          sx={{
            p: 3,
            background:
              'linear-gradient(135deg, rgba(240, 147, 251, 0.05) 0%, rgba(245, 87, 108, 0.05) 100%)',
            gap: 2,
          }}
        >
          <Button
            onClick={() => setContactDialogOpen(false)}
            variant="outlined"
            sx={{
              borderColor: 'rgba(240, 147, 251, 0.3)',
              color: '#f093fb',
              '&:hover': {
                borderColor: '#f093fb',
                background: 'rgba(240, 147, 251, 0.1)',
              },
            }}
          >
            Cancel
          </Button>
          <Button
            onClick={handleContactSubmit}
            variant="contained"
            sx={{
              background: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
              boxShadow: '0 4px 15px rgba(240, 147, 251, 0.4)',
              '&:hover': {
                background: 'linear-gradient(135deg, #e081e9 0%, #e3455a 100%)',
                boxShadow: '0 6px 20px rgba(240, 147, 251, 0.6)',
              },
            }}
          >
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <StarIcon />
              Create Enterprise Contact
            </Box>
          </Button>
        </DialogActions>
      </Dialog>

      {/* Confirmation Dialog - Ultimate Enterprise Style */}
      <Dialog
        open={confirmDialogOpen}
        onClose={() => setConfirmDialogOpen(false)}
        title="Enterprise Confirmation"
        maxWidth="sm"
        PaperProps={{
          sx: {
            background: 'linear-gradient(145deg, #ffffff 0%, #fff5f5 100%)',
            border: '1px solid rgba(244, 67, 54, 0.2)',
            borderRadius: 3,
            boxShadow: '0 20px 40px rgba(244, 67, 54, 0.15)',
          },
        }}
      >
        <DialogTitle
          sx={{
            background: 'linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%)',
            color: 'white',
            textAlign: 'center',
            py: 3,
            position: 'relative',
            '&::before': {
              content: '""',
              position: 'absolute',
              top: 0,
              right: 0,
              width: '80px',
              height: '80px',
              background: 'rgba(255, 255, 255, 0.1)',
              borderRadius: '50%',
              transform: 'translate(25px, -25px)',
            },
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 2 }}>
            <SecurityIcon sx={{ fontSize: 32 }} />
            <Typography variant="h5" fontWeight="bold">
              Enterprise Confirmation
            </Typography>
          </Box>
          <Typography variant="body2" sx={{ opacity: 0.9, mt: 1 }}>
            Secure action confirmation with audit trail
          </Typography>
        </DialogTitle>
        <DialogContent sx={{ p: 4 }}>
          <Box
            sx={{
              p: 3,
              borderRadius: 2,
              background:
                'linear-gradient(45deg, rgba(255, 107, 107, 0.05) 30%, rgba(238, 90, 36, 0.05) 90%)',
              border: '1px solid rgba(255, 107, 107, 0.1)',
              textAlign: 'center',
            }}
          >
            <Box sx={{ display: 'flex', justifyContent: 'center', mb: 2 }}>
              <Box
                sx={{
                  p: 2,
                  borderRadius: '50%',
                  background:
                    'linear-gradient(135deg, rgba(255, 107, 107, 0.1) 0%, rgba(238, 90, 36, 0.1) 100%)',
                  border: '2px solid rgba(255, 107, 107, 0.2)',
                }}
              >
                <SecurityIcon sx={{ fontSize: 40, color: '#ff6b6b' }} />
              </Box>
            </Box>
            <Typography variant="h6" gutterBottom fontWeight="bold">
              Confirm Enterprise Action
            </Typography>
            <Typography variant="body1" color="text.primary" sx={{ mb: 2 }}>
              This is a demo of the reusable confirmation dialog component with enterprise-grade
              security features.
            </Typography>
            <Typography variant="body2" color="text.secondary">
              This dialog can be customized for different types of confirmations and includes audit
              logging, role-based permissions, and security validation.
            </Typography>

            <Box
              sx={{
                mt: 3,
                p: 2,
                borderRadius: 1,
                background: 'rgba(255, 107, 107, 0.05)',
                border: '1px dashed rgba(255, 107, 107, 0.2)',
              }}
            >
              <Typography variant="caption" color="text.secondary">
                🔒 This action will be logged in the enterprise audit trail
              </Typography>
            </Box>
          </Box>
        </DialogContent>
        <DialogActions
          sx={{
            p: 3,
            background:
              'linear-gradient(135deg, rgba(255, 107, 107, 0.05) 0%, rgba(238, 90, 36, 0.05) 100%)',
            gap: 2,
          }}
        >
          <Button
            onClick={() => setConfirmDialogOpen(false)}
            variant="outlined"
            sx={{
              borderColor: 'rgba(102, 126, 234, 0.3)',
              color: '#667eea',
              '&:hover': {
                borderColor: '#667eea',
                background: 'rgba(102, 126, 234, 0.1)',
              },
            }}
          >
            Cancel
          </Button>
          <Button
            onClick={handleConfirmDelete}
            variant="contained"
            sx={{
              background: 'linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%)',
              boxShadow: '0 4px 15px rgba(255, 107, 107, 0.4)',
              '&:hover': {
                background: 'linear-gradient(135deg, #ff5252 0%, #d84315 100%)',
                boxShadow: '0 6px 20px rgba(255, 107, 107, 0.6)',
              },
            }}
          >
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <SecurityIcon />
              Confirm Enterprise Action
            </Box>
          </Button>
        </DialogActions>
      </Dialog>

      {/* Success/Error Snackbar - Ultimate Enterprise Style */}
      <Snackbar
        open={snackbarOpen}
        autoHideDuration={4000}
        onClose={() => setSnackbarOpen(false)}
        message={snackbarMessage}
        severity="success"
      />

      {/* Add CSS animations */}
      <style>
        {`
          @keyframes spin {
            from { transform: rotate(0deg); }
            to { transform: rotate(360deg); }
          }
        `}
      </style>
    </Box>
  );
};

export default EnhancementDemo;
