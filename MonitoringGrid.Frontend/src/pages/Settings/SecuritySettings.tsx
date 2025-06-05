import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  TextField,
  Switch,
  FormControlLabel,
  Button,
  Alert,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper
} from '@mui/material';
import {
  Security,
  Key,
  Shield,
  Lock,
  Visibility,
  Delete,
  Add,
  Warning,
  CheckCircle,
  VpnKey
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { PageHeader } from '../../components/Common';
import { securityService } from '../../services/securityService';

interface SecurityConfig {
  passwordPolicy: {
    minimumLength: number;
    requireUppercase: boolean;
    requireLowercase: boolean;
    requireDigit: boolean;
    requireSpecialCharacter: boolean;
    passwordHistoryCount: number;
    maxFailedAttempts: number;
    lockoutDurationMinutes: number;
    passwordExpirationDays: number;
  };
  sessionSettings: {
    sessionTimeoutMinutes: number;
    idleTimeoutMinutes: number;
    requireHttps: boolean;
    secureCookies: boolean;
  };
  twoFactorSettings: {
    isEnabled: boolean;
    isRequired: boolean;
    enabledProviders: string[];
    codeExpirationMinutes: number;
    maxAttempts: number;
  };
  rateLimitSettings: {
    isEnabled: boolean;
    requestsPerMinute: number;
    requestsPerHour: number;
    requestsPerDay: number;
  };
}

interface ApiKey {
  keyId: string;
  name: string;
  scopes: string[];
  expiresAt?: string;
  lastUsed?: string;
  isActive: boolean;
  createdAt: string;
}

interface SecurityEvent {
  eventId: string;
  eventType: string;
  userId?: string;
  ipAddress?: string;
  timestamp: string;
  isSuccess: boolean;
  description: string;
}

const securitySchema = yup.object({
  passwordPolicy: yup.object({
    minimumLength: yup.number().min(6).max(128).required(),
    passwordExpirationDays: yup.number().min(1).max(365).required(),
    maxFailedAttempts: yup.number().min(1).max(10).required(),
    lockoutDurationMinutes: yup.number().min(1).max(1440).required()
  }),
  sessionSettings: yup.object({
    sessionTimeoutMinutes: yup.number().min(5).max(1440).required(),
    idleTimeoutMinutes: yup.number().min(5).max(240).required()
  }),
  rateLimitSettings: yup.object({
    requestsPerMinute: yup.number().min(1).max(10000).required(),
    requestsPerHour: yup.number().min(1).max(100000).required(),
    requestsPerDay: yup.number().min(1).max(1000000).required()
  })
});

export const SecuritySettings: React.FC = () => {
  const [config, setConfig] = useState<SecurityConfig | null>(null);
  const [apiKeys, setApiKeys] = useState<ApiKey[]>([]);
  const [securityEvents, setSecurityEvents] = useState<SecurityEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [apiKeyDialogOpen, setApiKeyDialogOpen] = useState(false);
  const [newApiKeyName, setNewApiKeyName] = useState('');

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors }
  } = useForm<SecurityConfig>({
    resolver: yupResolver(securitySchema)
  });

  useEffect(() => {
    loadSecuritySettings();
  }, []);

  const loadSecuritySettings = async () => {
    try {
      setLoading(true);
      const [configData, apiKeysData, eventsData] = await Promise.all([
        securityService.getSecurityConfig(),
        securityService.getApiKeys(),
        securityService.getSecurityEvents()
      ]);
      
      setConfig(configData);
      setApiKeys(apiKeysData);
      setSecurityEvents(eventsData);
      reset(configData);
    } catch (err) {
      setError('Failed to load security settings');
      console.error('Error loading security settings:', err);
    } finally {
      setLoading(false);
    }
  };

  const onSubmit = async (data: SecurityConfig) => {
    try {
      setSaving(true);
      await securityService.updateSecurityConfig(data);
      setSuccess('Security settings updated successfully');
      setConfig(data);
    } catch (err) {
      setError('Failed to update security settings');
      console.error('Error updating security settings:', err);
    } finally {
      setSaving(false);
    }
  };

  const handleCreateApiKey = async () => {
    if (!newApiKeyName.trim()) return;

    try {
      const newKey = await securityService.createApiKey({
        name: newApiKeyName,
        scopes: ['read:kpis', 'write:kpis']
      });
      
      setApiKeys(prev => [newKey, ...prev]);
      setApiKeyDialogOpen(false);
      setNewApiKeyName('');
      setSuccess('API key created successfully');
    } catch (err) {
      setError('Failed to create API key');
      console.error('Error creating API key:', err);
    }
  };

  const handleRevokeApiKey = async (keyId: string) => {
    if (!confirm('Are you sure you want to revoke this API key?')) return;

    try {
      await securityService.revokeApiKey(keyId);
      setApiKeys(prev => prev.filter(key => key.keyId !== keyId));
      setSuccess('API key revoked successfully');
    } catch (err) {
      setError('Failed to revoke API key');
      console.error('Error revoking API key:', err);
    }
  };

  if (loading) {
    return (
      <Box>
        <PageHeader
          title="Security Settings"
          subtitle="Configure security policies and access controls"
          icon={<Security />}
        />
        <Typography>Loading...</Typography>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title="Security Settings"
        subtitle="Configure security policies and access controls"
        icon={<Security />}
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

      <form onSubmit={handleSubmit(onSubmit)}>
        <Grid container spacing={3}>
          {/* Password Policy */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  <Lock sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Password Policy
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="passwordPolicy.minimumLength"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          label="Minimum Length"
                          type="number"
                          fullWidth
                          error={!!errors.passwordPolicy?.minimumLength}
                          helperText={errors.passwordPolicy?.minimumLength?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="passwordPolicy.requireUppercase"
                      control={control}
                      render={({ field }) => (
                        <FormControlLabel
                          control={<Switch {...field} checked={field.value} />}
                          label="Require Uppercase Letters"
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="passwordPolicy.requireLowercase"
                      control={control}
                      render={({ field }) => (
                        <FormControlLabel
                          control={<Switch {...field} checked={field.value} />}
                          label="Require Lowercase Letters"
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="passwordPolicy.requireDigit"
                      control={control}
                      render={({ field }) => (
                        <FormControlLabel
                          control={<Switch {...field} checked={field.value} />}
                          label="Require Numbers"
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="passwordPolicy.requireSpecialCharacter"
                      control={control}
                      render={({ field }) => (
                        <FormControlLabel
                          control={<Switch {...field} checked={field.value} />}
                          label="Require Special Characters"
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <Controller
                      name="passwordPolicy.maxFailedAttempts"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          label="Max Failed Attempts"
                          type="number"
                          fullWidth
                          error={!!errors.passwordPolicy?.maxFailedAttempts}
                          helperText={errors.passwordPolicy?.maxFailedAttempts?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <Controller
                      name="passwordPolicy.lockoutDurationMinutes"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          label="Lockout Duration (minutes)"
                          type="number"
                          fullWidth
                          error={!!errors.passwordPolicy?.lockoutDurationMinutes}
                          helperText={errors.passwordPolicy?.lockoutDurationMinutes?.message}
                        />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          {/* Session Settings */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  <Shield sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Session Settings
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <Controller
                      name="sessionSettings.sessionTimeoutMinutes"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          label="Session Timeout (minutes)"
                          type="number"
                          fullWidth
                          error={!!errors.sessionSettings?.sessionTimeoutMinutes}
                          helperText={errors.sessionSettings?.sessionTimeoutMinutes?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <Controller
                      name="sessionSettings.idleTimeoutMinutes"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          label="Idle Timeout (minutes)"
                          type="number"
                          fullWidth
                          error={!!errors.sessionSettings?.idleTimeoutMinutes}
                          helperText={errors.sessionSettings?.idleTimeoutMinutes?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="sessionSettings.requireHttps"
                      control={control}
                      render={({ field }) => (
                        <FormControlLabel
                          control={<Switch {...field} checked={field.value} />}
                          label="Require HTTPS"
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="sessionSettings.secureCookies"
                      control={control}
                      render={({ field }) => (
                        <FormControlLabel
                          control={<Switch {...field} checked={field.value} />}
                          label="Secure Cookies"
                        />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          {/* API Keys Management */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Typography variant="h6">
                    <Key sx={{ mr: 1, verticalAlign: 'middle' }} />
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
                <TableContainer component={Paper}>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Name</TableCell>
                        <TableCell>Scopes</TableCell>
                        <TableCell>Created</TableCell>
                        <TableCell>Last Used</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell>Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {apiKeys.map((apiKey) => (
                        <TableRow key={apiKey.keyId}>
                          <TableCell>{apiKey.name}</TableCell>
                          <TableCell>
                            {apiKey.scopes.map((scope) => (
                              <Chip key={scope} label={scope} size="small" sx={{ mr: 0.5 }} />
                            ))}
                          </TableCell>
                          <TableCell>{new Date(apiKey.createdAt).toLocaleDateString()}</TableCell>
                          <TableCell>
                            {apiKey.lastUsed ? new Date(apiKey.lastUsed).toLocaleDateString() : 'Never'}
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={apiKey.isActive ? 'Active' : 'Revoked'}
                              color={apiKey.isActive ? 'success' : 'error'}
                              size="small"
                            />
                          </TableCell>
                          <TableCell>
                            <IconButton
                              onClick={() => handleRevokeApiKey(apiKey.keyId)}
                              disabled={!apiKey.isActive}
                              color="error"
                            >
                              <Delete />
                            </IconButton>
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
              <Button
                type="submit"
                variant="contained"
                size="large"
                disabled={saving}
                startIcon={<Security />}
              >
                {saving ? 'Saving...' : 'Save Security Settings'}
              </Button>
            </Box>
          </Grid>
        </Grid>
      </form>

      {/* Create API Key Dialog */}
      <Dialog open={apiKeyDialogOpen} onClose={() => setApiKeyDialogOpen(false)}>
        <DialogTitle>Create New API Key</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="API Key Name"
            fullWidth
            variant="outlined"
            value={newApiKeyName}
            onChange={(e) => setNewApiKeyName(e.target.value)}
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

export default SecuritySettings;
