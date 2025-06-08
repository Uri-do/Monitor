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
  Paper,
  Tabs,
  Tab,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Accordion,
  AccordionSummary,
  AccordionDetails,
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
  VpnKey,
  People,
  Settings,
  History,
  ExpandMore,
  Save,
  AdminPanelSettings,
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import toast from 'react-hot-toast';
import { securityService } from '../../services/securityService';
import { SecurityConfig, SecurityEvent, User, Role, Permission } from '../../types/auth';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`security-tabpanel-${index}`}
      aria-labelledby={`security-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

interface ApiKey {
  keyId: string;
  name: string;
  scopes: string[];
  createdAt: string;
  lastUsed?: string;
  isActive: boolean;
}

const securitySchema = yup.object({
  passwordPolicy: yup.object({
    minimumLength: yup.number().min(6).max(128).required(),
    requireUppercase: yup.boolean().required(),
    requireLowercase: yup.boolean().required(),
    requireNumbers: yup.boolean().required(),
    requireSpecialChars: yup.boolean().required(),
    passwordExpirationDays: yup.number().min(1).max(365).required(),
    maxFailedAttempts: yup.number().min(1).max(10).required(),
    lockoutDurationMinutes: yup.number().min(1).max(1440).required(),
  }),
  sessionSettings: yup.object({
    sessionTimeoutMinutes: yup.number().min(5).max(1440).required(),
    idleTimeoutMinutes: yup.number().min(5).max(240).required(),
    allowConcurrentSessions: yup.boolean().required(),
  }),
  twoFactorSettings: yup.object({
    enabled: yup.boolean().required(),
    required: yup.boolean().required(),
    methods: yup.array().of(yup.string().required()).required(),
  }),
  rateLimitSettings: yup.object({
    enabled: yup.boolean().required(),
    maxRequestsPerMinute: yup.number().min(1).max(10000).required(),
    maxRequestsPerHour: yup.number().min(1).max(100000).required(),
  }),
});

export const SecuritySettingsNew: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [config, setConfig] = useState<SecurityConfig | null>(null);
  const [apiKeys, setApiKeys] = useState<ApiKey[]>([]);
  const [securityEvents, setSecurityEvents] = useState<SecurityEvent[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [apiKeyDialogOpen, setApiKeyDialogOpen] = useState(false);
  const [newApiKeyName, setNewApiKeyName] = useState('');
  const [newApiKeyScopes, setNewApiKeyScopes] = useState<string[]>([]);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<SecurityConfig>({
    resolver: yupResolver(securitySchema),
  });

  useEffect(() => {
    loadSecuritySettings();
  }, []);

  const loadSecuritySettings = async () => {
    try {
      setLoading(true);
      setError(null);

      // Load security configuration with default values if API fails
      let configData: SecurityConfig;
      try {
        configData = await securityService.getSecurityConfig();
      } catch (err) {
        console.warn('Failed to load security config, using defaults:', err);
        configData = {
          passwordPolicy: {
            minimumLength: 8,
            requireUppercase: true,
            requireLowercase: true,
            requireNumbers: true,
            requireSpecialChars: true,
            passwordExpirationDays: 90,
            maxFailedAttempts: 5,
            lockoutDurationMinutes: 30,
          },
          sessionSettings: {
            sessionTimeoutMinutes: 480,
            idleTimeoutMinutes: 60,
            allowConcurrentSessions: false,
          },
          twoFactorSettings: {
            enabled: false,
            required: false,
            methods: ['TOTP', 'SMS', 'Email'],
          },
          rateLimitSettings: {
            enabled: true,
            maxRequestsPerMinute: 100,
            maxRequestsPerHour: 1000,
          },
        };
      }

      // Load other data with error handling
      const [apiKeysData, eventsData, usersData, rolesData, permissionsData] =
        await Promise.allSettled([
          securityService.getApiKeys(),
          securityService.getSecurityEvents(),
          securityService.getUsers(),
          securityService.getRoles(),
          securityService.getPermissions(),
        ]);

      setConfig(configData);
      setApiKeys(apiKeysData.status === 'fulfilled' ? apiKeysData.value : []);
      setSecurityEvents(eventsData.status === 'fulfilled' ? eventsData.value : []);
      setUsers(usersData.status === 'fulfilled' ? usersData.value : []);
      setRoles(rolesData.status === 'fulfilled' ? rolesData.value : []);
      setPermissions(permissionsData.status === 'fulfilled' ? permissionsData.value : []);

      reset(configData);
    } catch (err) {
      setError('Failed to load security settings');
      console.error('Error loading security settings:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const onSubmit = async (data: SecurityConfig) => {
    try {
      setSaving(true);
      setError(null);
      await securityService.updateSecurityConfig(data);
      setSuccess('Security settings updated successfully');
      setConfig(data);
      toast.success('Security settings updated successfully');
    } catch (err) {
      const errorMessage = 'Failed to update security settings';
      setError(errorMessage);
      toast.error(errorMessage);
      console.error('Error updating security settings:', err);
    } finally {
      setSaving(false);
    }
  };

  const handleCreateApiKey = async () => {
    if (!newApiKeyName.trim()) {
      setError('API key name is required');
      return;
    }

    try {
      const scopes = newApiKeyScopes.length > 0 ? newApiKeyScopes : ['read'];
      const result = await securityService.createApiKey(newApiKeyName, scopes);
      setApiKeys(prev => [
        ...prev,
        {
          keyId: result.keyId,
          name: newApiKeyName,
          scopes,
          createdAt: new Date().toISOString(),
          isActive: true,
        },
      ]);
      setNewApiKeyName('');
      setNewApiKeyScopes([]);
      setApiKeyDialogOpen(false);
      setSuccess(`API key created successfully`);
      toast.success(`API key "${newApiKeyName}" created successfully`);

      // Show the API key to the user (this should be done securely)
      alert(
        `API Key: ${result.key}\n\nPlease save this key securely. You won't be able to see it again.`
      );
    } catch (err) {
      const errorMessage = 'Failed to create API key';
      setError(errorMessage);
      toast.error(errorMessage);
      console.error('Error creating API key:', err);
    }
  };

  const handleRevokeApiKey = async (keyId: string) => {
    if (!confirm('Are you sure you want to revoke this API key?')) return;

    try {
      await securityService.revokeApiKey(keyId);
      setApiKeys(prev => prev.filter(key => key.keyId !== keyId));
      setSuccess('API key revoked successfully');
      toast.success('API key revoked successfully');
    } catch (err) {
      const errorMessage = 'Failed to revoke API key';
      setError(errorMessage);
      toast.error(errorMessage);
      console.error('Error revoking API key:', err);
    }
  };

  if (loading) {
    return (
      <Box>
        <Typography>Loading security settings...</Typography>
      </Box>
    );
  }

  return (
    <Box>
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

      <Paper sx={{ width: '100%' }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          aria-label="security settings tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab icon={<Lock />} label="Password Policy" />
          <Tab icon={<Shield />} label="Session & Auth" />
          <Tab icon={<People />} label="User Management" />
          <Tab icon={<Key />} label="API Keys" />
          <Tab icon={<History />} label="Security Events" />
        </Tabs>

        {/* Password Policy Tab */}
        <TabPanel value={activeTab} index={0}>
          <form onSubmit={handleSubmit(onSubmit as any)}>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>
                      <Lock sx={{ mr: 1, verticalAlign: 'middle' }} />
                      Password Requirements
                    </Typography>
                    <Grid container spacing={2}>
                      <Grid item xs={12}>
                        <Controller
                          name="passwordPolicy.minimumLength"
                          control={control}
                          render={({ field }) => (
                            <TextField
                              {...field}
                              fullWidth
                              label="Minimum Length"
                              type="number"
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
                          name="passwordPolicy.requireNumbers"
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
                          name="passwordPolicy.requireSpecialChars"
                          control={control}
                          render={({ field }) => (
                            <FormControlLabel
                              control={<Switch {...field} checked={field.value} />}
                              label="Require Special Characters"
                            />
                          )}
                        />
                      </Grid>
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>
                      <Warning sx={{ mr: 1, verticalAlign: 'middle' }} />
                      Account Security
                    </Typography>
                    <Grid container spacing={2}>
                      <Grid item xs={12}>
                        <Controller
                          name="passwordPolicy.passwordExpirationDays"
                          control={control}
                          render={({ field }) => (
                            <TextField
                              {...field}
                              fullWidth
                              label="Password Expiration (days)"
                              type="number"
                              error={!!errors.passwordPolicy?.passwordExpirationDays}
                              helperText={errors.passwordPolicy?.passwordExpirationDays?.message}
                            />
                          )}
                        />
                      </Grid>
                      <Grid item xs={12}>
                        <Controller
                          name="passwordPolicy.maxFailedAttempts"
                          control={control}
                          render={({ field }) => (
                            <TextField
                              {...field}
                              fullWidth
                              label="Max Failed Login Attempts"
                              type="number"
                              error={!!errors.passwordPolicy?.maxFailedAttempts}
                              helperText={errors.passwordPolicy?.maxFailedAttempts?.message}
                            />
                          )}
                        />
                      </Grid>
                      <Grid item xs={12}>
                        <Controller
                          name="passwordPolicy.lockoutDurationMinutes"
                          control={control}
                          render={({ field }) => (
                            <TextField
                              {...field}
                              fullWidth
                              label="Account Lockout Duration (minutes)"
                              type="number"
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

              <Grid item xs={12}>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                  <Button type="submit" variant="contained" disabled={saving} startIcon={<Save />}>
                    {saving ? 'Saving...' : 'Save Password Policy'}
                  </Button>
                </Box>
              </Grid>
            </Grid>
          </form>
        </TabPanel>

        {/* Session & Auth Tab */}
        <TabPanel value={activeTab} index={1}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Shield sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Session Settings
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12}>
                      <Controller
                        name="sessionSettings.sessionTimeoutMinutes"
                        control={control}
                        render={({ field }) => (
                          <TextField
                            {...field}
                            fullWidth
                            label="Session Timeout (minutes)"
                            type="number"
                            error={!!errors.sessionSettings?.sessionTimeoutMinutes}
                            helperText={errors.sessionSettings?.sessionTimeoutMinutes?.message}
                          />
                        )}
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <Controller
                        name="sessionSettings.idleTimeoutMinutes"
                        control={control}
                        render={({ field }) => (
                          <TextField
                            {...field}
                            fullWidth
                            label="Idle Timeout (minutes)"
                            type="number"
                            error={!!errors.sessionSettings?.idleTimeoutMinutes}
                            helperText={errors.sessionSettings?.idleTimeoutMinutes?.message}
                          />
                        )}
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <Controller
                        name="sessionSettings.allowConcurrentSessions"
                        control={control}
                        render={({ field }) => (
                          <FormControlLabel
                            control={<Switch {...field} checked={field.value} />}
                            label="Allow Concurrent Sessions"
                          />
                        )}
                      />
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <VpnKey sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Two-Factor Authentication
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12}>
                      <Controller
                        name="twoFactorSettings.enabled"
                        control={control}
                        render={({ field }) => (
                          <FormControlLabel
                            control={<Switch {...field} checked={field.value} />}
                            label="Enable Two-Factor Authentication"
                          />
                        )}
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <Controller
                        name="twoFactorSettings.required"
                        control={control}
                        render={({ field }) => (
                          <FormControlLabel
                            control={<Switch {...field} checked={field.value} />}
                            label="Require for All Users"
                          />
                        )}
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <FormControl fullWidth>
                        <InputLabel>Available Methods</InputLabel>
                        <Select
                          multiple
                          value={config?.twoFactorSettings.methods || []}
                          label="Available Methods"
                          renderValue={selected => (
                            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                              {selected.map(value => (
                                <Chip key={value} label={value} size="small" />
                              ))}
                            </Box>
                          )}
                        >
                          <MenuItem value="TOTP">Authenticator App (TOTP)</MenuItem>
                          <MenuItem value="SMS">SMS</MenuItem>
                          <MenuItem value="Email">Email</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Warning sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Rate Limiting
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={4}>
                      <Controller
                        name="rateLimitSettings.enabled"
                        control={control}
                        render={({ field }) => (
                          <FormControlLabel
                            control={<Switch {...field} checked={field.value} />}
                            label="Enable Rate Limiting"
                          />
                        )}
                      />
                    </Grid>
                    <Grid item xs={12} sm={4}>
                      <Controller
                        name="rateLimitSettings.maxRequestsPerMinute"
                        control={control}
                        render={({ field }) => (
                          <TextField
                            {...field}
                            fullWidth
                            label="Requests per Minute"
                            type="number"
                            error={!!errors.rateLimitSettings?.maxRequestsPerMinute}
                            helperText={errors.rateLimitSettings?.maxRequestsPerMinute?.message}
                          />
                        )}
                      />
                    </Grid>
                    <Grid item xs={12} sm={4}>
                      <Controller
                        name="rateLimitSettings.maxRequestsPerHour"
                        control={control}
                        render={({ field }) => (
                          <TextField
                            {...field}
                            fullWidth
                            label="Requests per Hour"
                            type="number"
                            error={!!errors.rateLimitSettings?.maxRequestsPerHour}
                            helperText={errors.rateLimitSettings?.maxRequestsPerHour?.message}
                          />
                        )}
                      />
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>

        {/* User Management Tab */}
        <TabPanel value={activeTab} index={2}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <People sx={{ mr: 1, verticalAlign: 'middle' }} />
                    User Management
                  </Typography>
                  <TableContainer component={Paper}>
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell>Username</TableCell>
                          <TableCell>Email</TableCell>
                          <TableCell>Display Name</TableCell>
                          <TableCell>Roles</TableCell>
                          <TableCell>Status</TableCell>
                          <TableCell>Last Login</TableCell>
                          <TableCell>Actions</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {users.map(user => (
                          <TableRow key={user.userId}>
                            <TableCell>{user.username}</TableCell>
                            <TableCell>{user.email}</TableCell>
                            <TableCell>{user.displayName}</TableCell>
                            <TableCell>
                              {user.roles?.map(role => (
                                <Chip
                                  key={role.roleId}
                                  label={role.name}
                                  size="small"
                                  sx={{ mr: 0.5 }}
                                />
                              ))}
                            </TableCell>
                            <TableCell>
                              <Chip
                                label={user.isActive ? 'Active' : 'Inactive'}
                                color={user.isActive ? 'success' : 'error'}
                                size="small"
                              />
                            </TableCell>
                            <TableCell>
                              {user.lastLogin
                                ? new Date(user.lastLogin).toLocaleDateString()
                                : 'Never'}
                            </TableCell>
                            <TableCell>
                              <IconButton size="small" color="primary">
                                <Settings />
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

            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <AdminPanelSettings sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Roles & Permissions
                  </Typography>
                  <List>
                    {roles.map(role => (
                      <ListItem key={role.roleId}>
                        <ListItemText primary={role.name} secondary={role.description} />
                        <ListItemSecondaryAction>
                          <Chip
                            label={role.isSystemRole ? 'System' : 'Custom'}
                            color={role.isSystemRole ? 'default' : 'primary'}
                            size="small"
                          />
                        </ListItemSecondaryAction>
                      </ListItem>
                    ))}
                  </List>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Key sx={{ mr: 1, verticalAlign: 'middle' }} />
                    System Permissions
                  </Typography>
                  <List>
                    {permissions.map(permission => (
                      <ListItem key={permission.permissionId}>
                        <ListItemText
                          primary={permission.name}
                          secondary={`${permission.resource}:${permission.action}`}
                        />
                      </ListItem>
                    ))}
                  </List>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>

        {/* API Keys Tab */}
        <TabPanel value={activeTab} index={3}>
          <Grid container spacing={3}>
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
                    <Typography variant="h6">
                      <Key sx={{ mr: 1, verticalAlign: 'middle' }} />
                      API Keys Management
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
                        {apiKeys.map(apiKey => (
                          <TableRow key={apiKey.keyId}>
                            <TableCell>{apiKey.name}</TableCell>
                            <TableCell>
                              {apiKey.scopes.map(scope => (
                                <Chip key={scope} label={scope} size="small" sx={{ mr: 0.5 }} />
                              ))}
                            </TableCell>
                            <TableCell>{new Date(apiKey.createdAt).toLocaleDateString()}</TableCell>
                            <TableCell>
                              {apiKey.lastUsed
                                ? new Date(apiKey.lastUsed).toLocaleDateString()
                                : 'Never'}
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
                                size="small"
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
          </Grid>
        </TabPanel>

        {/* Security Events Tab */}
        <TabPanel value={activeTab} index={4}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <History sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Security Events Log
                  </Typography>
                  <TableContainer component={Paper}>
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell>Timestamp</TableCell>
                          <TableCell>Event Type</TableCell>
                          <TableCell>User</TableCell>
                          <TableCell>Description</TableCell>
                          <TableCell>IP Address</TableCell>
                          <TableCell>Status</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {securityEvents.map((event, index) => (
                          <TableRow key={event.id || index}>
                            <TableCell>{new Date(event.timestamp).toLocaleString()}</TableCell>
                            <TableCell>{event.eventType}</TableCell>
                            <TableCell>{event.userId || 'System'}</TableCell>
                            <TableCell>{event.description}</TableCell>
                            <TableCell>{event.ipAddress || 'N/A'}</TableCell>
                            <TableCell>
                              <Chip label="Success" color="success" size="small" />
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>
      </Paper>

      {/* Create API Key Dialog */}
      <Dialog
        open={apiKeyDialogOpen}
        onClose={() => setApiKeyDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Create New API Key</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                autoFocus
                fullWidth
                label="API Key Name"
                variant="outlined"
                value={newApiKeyName}
                onChange={e => setNewApiKeyName(e.target.value)}
                placeholder="Enter a descriptive name for this API key"
              />
            </Grid>
            <Grid item xs={12}>
              <FormControl fullWidth>
                <InputLabel>Scopes</InputLabel>
                <Select
                  multiple
                  value={newApiKeyScopes}
                  onChange={e => setNewApiKeyScopes(e.target.value as string[])}
                  label="Scopes"
                  renderValue={selected => (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                      {selected.map(value => (
                        <Chip key={value} label={value} size="small" />
                      ))}
                    </Box>
                  )}
                >
                  <MenuItem value="read">Read Access</MenuItem>
                  <MenuItem value="write">Write Access</MenuItem>
                  <MenuItem value="admin">Admin Access</MenuItem>
                  <MenuItem value="kpis:read">Read KPIs</MenuItem>
                  <MenuItem value="kpis:write">Write KPIs</MenuItem>
                  <MenuItem value="users:read">Read Users</MenuItem>
                  <MenuItem value="users:write">Write Users</MenuItem>
                </Select>
              </FormControl>
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setApiKeyDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleCreateApiKey} variant="contained" disabled={!newApiKeyName.trim()}>
            Create API Key
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default SecuritySettingsNew;
