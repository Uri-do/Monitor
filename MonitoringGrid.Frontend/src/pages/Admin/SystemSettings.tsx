import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Switch,
  FormControlLabel,
  TextField,
  Button,
  Grid,
  Divider,
  Alert,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
} from '@mui/material';
import {
  Settings,
  Security,
  Notifications,
  Storage,
  Wifi as Network,
  Monitor,
  Add,
  Delete,
  Edit,
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { PageHeader } from '@/components';
import toast from 'react-hot-toast';

interface SystemConfig {
  general: {
    siteName: string;
    siteDescription: string;
    maintenanceMode: boolean;
    debugMode: boolean;
  };
  security: {
    sessionTimeout: number;
    maxLoginAttempts: number;
    passwordExpiration: number;
    twoFactorRequired: boolean;
  };
  notifications: {
    emailEnabled: boolean;
    smsEnabled: boolean;
    slackEnabled: boolean;
    webhookEnabled: boolean;
  };
  monitoring: {
    dataRetentionDays: number;
    alertCooldown: number;
    maxConcurrentKpis: number;
    autoArchive: boolean;
  };
}

interface ApiKey {
  id: string;
  name: string;
  key: string;
  permissions: string[];
  createdAt: string;
  lastUsed?: string;
  isActive: boolean;
}

const configSchema = yup.object({
  general: yup.object({
    siteName: yup.string().required('Site name is required'),
    siteDescription: yup.string().required('Site description is required'),
    maintenanceMode: yup.boolean().required(),
    debugMode: yup.boolean().required(),
  }),
  security: yup.object({
    sessionTimeout: yup.number().min(5).max(1440).required(),
    maxLoginAttempts: yup.number().min(1).max(10).required(),
    passwordExpiration: yup.number().min(30).max(365).required(),
    twoFactorRequired: yup.boolean().required(),
  }),
  notifications: yup.object({
    emailEnabled: yup.boolean().required(),
    smsEnabled: yup.boolean().required(),
    slackEnabled: yup.boolean().required(),
    webhookEnabled: yup.boolean().required(),
  }),
  monitoring: yup.object({
    dataRetentionDays: yup.number().min(30).max(2555).required(),
    alertCooldown: yup.number().min(1).max(60).required(),
    maxConcurrentKpis: yup.number().min(10).max(1000).required(),
    autoArchive: yup.boolean().required(),
  }),
});

const SystemSettings: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [apiKeys, setApiKeys] = useState<ApiKey[]>([]);
  const [apiKeyDialogOpen, setApiKeyDialogOpen] = useState(false);
  const [newApiKeyName, setNewApiKeyName] = useState('');

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<SystemConfig>({
    resolver: yupResolver(configSchema),
    defaultValues: {
      general: {
        siteName: 'MonitoringGrid',
        siteDescription: 'KPI Monitoring and Alerting System',
        maintenanceMode: false,
        debugMode: false,
      },
      security: {
        sessionTimeout: 60,
        maxLoginAttempts: 5,
        passwordExpiration: 90,
        twoFactorRequired: false,
      },
      notifications: {
        emailEnabled: true,
        smsEnabled: false,
        slackEnabled: false,
        webhookEnabled: false,
      },
      monitoring: {
        dataRetentionDays: 365,
        alertCooldown: 5,
        maxConcurrentKpis: 100,
        autoArchive: true,
      },
    },
  });

  const onSubmit = async (data: SystemConfig) => {
    setLoading(true);
    setError(null);

    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));

      toast.success('System settings updated successfully');
      setSuccess('System settings updated successfully');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update settings';
      setError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateApiKey = async () => {
    if (!newApiKeyName.trim()) {
      toast.error('Please enter a name for the API key');
      return;
    }

    try {
      const newKey: ApiKey = {
        id: Date.now().toString(),
        name: newApiKeyName,
        key: `mgk_${Math.random().toString(36).substring(2, 15)}${Math.random().toString(36).substring(2, 15)}`,
        permissions: ['read', 'write'],
        createdAt: new Date().toISOString(),
        isActive: true,
      };

      setApiKeys(prev => [...prev, newKey]);
      setApiKeyDialogOpen(false);
      setNewApiKeyName('');
      toast.success('API key created successfully');
    } catch (err) {
      toast.error('Failed to create API key');
    }
  };

  const handleDeleteApiKey = (keyId: string) => {
    if (confirm('Are you sure you want to delete this API key?')) {
      setApiKeys(prev => prev.filter(key => key.id !== keyId));
      toast.success('API key deleted successfully');
    }
  };

  return (
    <Box>
      <PageHeader
        title="System Settings"
        subtitle="Configure system-wide settings and preferences"
        icon={<Settings />}
      />

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {success && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      )}

      <form onSubmit={handleSubmit(onSubmit as any)}>
        <Grid container spacing={3}>
          {/* General Settings */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography
                  variant="h6"
                  gutterBottom
                  sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
                >
                  <Monitor />
                  General Settings
                </Typography>

                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <Controller
                    name="general.siteName"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Site Name"
                        error={!!errors.general?.siteName}
                        helperText={errors.general?.siteName?.message}
                        fullWidth
                      />
                    )}
                  />

                  <Controller
                    name="general.siteDescription"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Site Description"
                        multiline
                        rows={3}
                        error={!!errors.general?.siteDescription}
                        helperText={errors.general?.siteDescription?.message}
                        fullWidth
                      />
                    )}
                  />

                  <Controller
                    name="general.maintenanceMode"
                    control={control}
                    render={({ field }) => (
                      <FormControlLabel
                        control={<Switch {...field} checked={field.value} />}
                        label="Maintenance Mode"
                      />
                    )}
                  />

                  <Controller
                    name="general.debugMode"
                    control={control}
                    render={({ field }) => (
                      <FormControlLabel
                        control={<Switch {...field} checked={field.value} />}
                        label="Debug Mode"
                      />
                    )}
                  />
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* Security Settings */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography
                  variant="h6"
                  gutterBottom
                  sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
                >
                  <Security />
                  Security Settings
                </Typography>

                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <Controller
                    name="security.sessionTimeout"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Session Timeout (minutes)"
                        type="number"
                        error={!!errors.security?.sessionTimeout}
                        helperText={errors.security?.sessionTimeout?.message}
                        fullWidth
                      />
                    )}
                  />

                  <Controller
                    name="security.maxLoginAttempts"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Max Login Attempts"
                        type="number"
                        error={!!errors.security?.maxLoginAttempts}
                        helperText={errors.security?.maxLoginAttempts?.message}
                        fullWidth
                      />
                    )}
                  />

                  <Controller
                    name="security.passwordExpiration"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Password Expiration (days)"
                        type="number"
                        error={!!errors.security?.passwordExpiration}
                        helperText={errors.security?.passwordExpiration?.message}
                        fullWidth
                      />
                    )}
                  />

                  <Controller
                    name="security.twoFactorRequired"
                    control={control}
                    render={({ field }) => (
                      <FormControlLabel
                        control={<Switch {...field} checked={field.value} />}
                        label="Require Two-Factor Authentication"
                      />
                    )}
                  />
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* API Keys Management */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Box
                  sx={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    mb: 2,
                  }}
                >
                  <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Network />
                    API Keys
                  </Typography>
                  <Button
                    variant="contained"
                    startIcon={<Add />}
                    onClick={() => setApiKeyDialogOpen(true)}
                  >
                    Create API Key
                  </Button>
                </Box>

                <TableContainer component={Paper} variant="outlined">
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Name</TableCell>
                        <TableCell>Key</TableCell>
                        <TableCell>Permissions</TableCell>
                        <TableCell>Created</TableCell>
                        <TableCell>Last Used</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell>Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {apiKeys.map(apiKey => (
                        <TableRow key={apiKey.id}>
                          <TableCell>{apiKey.name}</TableCell>
                          <TableCell>
                            <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                              {apiKey.key.substring(0, 20)}...
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <Box sx={{ display: 'flex', gap: 0.5 }}>
                              {apiKey.permissions.map(permission => (
                                <Chip key={permission} label={permission} size="small" />
                              ))}
                            </Box>
                          </TableCell>
                          <TableCell>{new Date(apiKey.createdAt).toLocaleDateString()}</TableCell>
                          <TableCell>
                            {apiKey.lastUsed
                              ? new Date(apiKey.lastUsed).toLocaleDateString()
                              : 'Never'}
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={apiKey.isActive ? 'Active' : 'Inactive'}
                              color={apiKey.isActive ? 'success' : 'error'}
                              size="small"
                            />
                          </TableCell>
                          <TableCell>
                            <Button
                              size="small"
                              color="error"
                              onClick={() => handleDeleteApiKey(apiKey.id)}
                            >
                              <Delete />
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>

          {/* Save Button */}
          <Grid item xs={12}>
            <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button type="submit" variant="contained" size="large" disabled={loading}>
                {loading ? 'Saving...' : 'Save Settings'}
              </Button>
            </Box>
          </Grid>
        </Grid>
      </form>

      {/* Create API Key Dialog */}
      <Dialog open={apiKeyDialogOpen} onClose={() => setApiKeyDialogOpen(false)}>
        <DialogTitle>Create API Key</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="API Key Name"
            fullWidth
            variant="outlined"
            value={newApiKeyName}
            onChange={e => setNewApiKeyName(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setApiKeyDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleCreateApiKey} variant="contained">
            Create
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default SystemSettings;
