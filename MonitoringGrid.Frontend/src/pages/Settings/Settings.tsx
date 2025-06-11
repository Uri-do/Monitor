import React, { useState } from 'react';
import {
  Box,
  Grid,
  CardContent,
  Typography,
  Switch,
  FormControlLabel,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Chip,
  Alert,
  Tabs,
  Tab,
  Paper,
} from '@mui/material';
import {
  Save as SaveIcon,
  Email as EmailIcon,
  Sms as SmsIcon,
  Security as SecurityIcon,
  Notifications as NotificationIcon,
  Storage as StorageIcon,
  Settings as SystemIcon,
  CheckCircle as SuccessIcon,
} from '@mui/icons-material';
import { useQuery, useMutation } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import {
  UltimatePageHeader,
  UltimateStatusChip,
  UltimateLoadingSpinner,
  UltimateCard,
  UltimateInputField,
  UltimateSelect,
  UltimateButton
} from '@/components/UltimateEnterprise';

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
      id={`settings-tabpanel-${index}`}
      aria-labelledby={`settings-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const Settings: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [emailSettings, setEmailSettings] = useState({
    smtpServer: 'smtp.company.com',
    smtpPort: '587',
    username: 'monitoring@company.com',
    password: '••••••••',
    enableSsl: true,
    fromAddress: 'monitoring@company.com',
    fromName: 'Monitoring System',
  });

  const [smsSettings, setSmsSettings] = useState({
    provider: 'Twilio',
    accountSid: 'AC••••••••••••••••••••••••••••••••',
    authToken: '••••••••••••••••••••••••••••••••',
    fromNumber: '+1234567890',
    enabled: true,
  });

  const [systemSettings, setSystemSettings] = useState({
    defaultKpiFrequency: 60,
    maxRetryAttempts: 3,
    alertCooldownMinutes: 30,
    dataRetentionDays: 90,
    enableLogging: true,
    logLevel: 'Info',
  });

  // Mock health check data
  const systemHealth = {
    database: { status: 'Healthy', lastCheck: '2024-01-15T10:45:00Z' },
    emailService: { status: 'Healthy', lastCheck: '2024-01-15T10:44:00Z' },
    smsService: { status: 'Healthy', lastCheck: '2024-01-15T10:43:00Z' },
    workerService: { status: 'Healthy', lastCheck: '2024-01-15T10:45:00Z' },
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleSaveEmailSettings = () => {
    // TODO: Implement save functionality
    toast.success('Email settings saved successfully');
  };

  const handleSaveSmsSettings = () => {
    // TODO: Implement save functionality
    toast.success('SMS settings saved successfully');
  };

  const handleSaveSystemSettings = () => {
    // TODO: Implement save functionality
    toast.success('System settings saved successfully');
  };

  const handleTestEmail = () => {
    // TODO: Implement test email functionality
    toast.success('Test email sent successfully');
  };

  const handleTestSms = () => {
    // TODO: Implement test SMS functionality
    toast.success('Test SMS sent successfully');
  };

  return (
    <Box>
      <UltimatePageHeader
        title="User Settings"
        subtitle="Configure your personal preferences and notification settings"
      />

      <Paper sx={{ width: '100%' }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          aria-label="settings tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab icon={<NotificationIcon />} label="Notifications" />
          <Tab icon={<SystemIcon />} label="Preferences" />
          <Tab icon={<StorageIcon />} label="Health" />
        </Tabs>

        {/* Notifications Tab */}
        <TabPanel value={activeTab} index={0}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={8}>
              <UltimateCard>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Email Notification Preferences
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={6}>
                      <UltimateInputField
                        fullWidth
                        label="SMTP Server"
                        value={emailSettings.smtpServer}
                        onChange={e =>
                          setEmailSettings({ ...emailSettings, smtpServer: e.target.value })
                        }
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <UltimateInputField
                        fullWidth
                        label="Port"
                        type="number"
                        value={emailSettings.smtpPort}
                        onChange={e =>
                          setEmailSettings({ ...emailSettings, smtpPort: e.target.value })
                        }
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <UltimateInputField
                        fullWidth
                        label="Username"
                        value={emailSettings.username}
                        onChange={e =>
                          setEmailSettings({ ...emailSettings, username: e.target.value })
                        }
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <UltimateInputField
                        fullWidth
                        label="Password"
                        type="password"
                        value={emailSettings.password}
                        onChange={e =>
                          setEmailSettings({ ...emailSettings, password: e.target.value })
                        }
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <UltimateInputField
                        fullWidth
                        label="From Address"
                        value={emailSettings.fromAddress}
                        onChange={e =>
                          setEmailSettings({ ...emailSettings, fromAddress: e.target.value })
                        }
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <UltimateInputField
                        fullWidth
                        label="From Name"
                        value={emailSettings.fromName}
                        onChange={e =>
                          setEmailSettings({ ...emailSettings, fromName: e.target.value })
                        }
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={emailSettings.enableSsl}
                            onChange={e =>
                              setEmailSettings({ ...emailSettings, enableSsl: e.target.checked })
                            }
                          />
                        }
                        label="Enable SSL/TLS"
                      />
                    </Grid>
                  </Grid>
                  <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
                    <UltimateButton
                      gradient="primary"
                      startIcon={<SaveIcon />}
                      onClick={handleSaveEmailSettings}
                    >
                      Save Settings
                    </UltimateButton>
                    <UltimateButton
                      variant="outlined"
                      gradient="info"
                      onClick={handleTestEmail}
                    >
                      Send Test Email
                    </UltimateButton>
                  </Box>
                </CardContent>
              </UltimateCard>
            </Grid>
            <Grid item xs={12} md={4}>
              <UltimateCard>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Email Status
                  </Typography>
                  <List>
                    <ListItem>
                      <ListItemText primary="Service Status" />
                      <ListItemSecondaryAction>
                        <UltimateStatusChip status="success" />
                      </ListItemSecondaryAction>
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Last Test" />
                      <ListItemSecondaryAction>
                        <Typography variant="body2">2 hours ago</Typography>
                      </ListItemSecondaryAction>
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Emails Sent Today" />
                      <ListItemSecondaryAction>
                        <Chip label="24" color="primary" size="small" />
                      </ListItemSecondaryAction>
                    </ListItem>
                  </List>
                </CardContent>
              </UltimateCard>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Preferences Tab */}
        <TabPanel value={activeTab} index={1}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={8}>
              <UltimateCard>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Personal Preferences
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Provider"
                        value={smsSettings.provider}
                        onChange={e => setSmsSettings({ ...smsSettings, provider: e.target.value })}
                        select
                        SelectProps={{ native: true }}
                      >
                        <option value="Twilio">Twilio</option>
                        <option value="AWS SNS">AWS SNS</option>
                        <option value="Azure">Azure Communication Services</option>
                      </TextField>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <TextField
                        fullWidth
                        label="Account SID"
                        value={smsSettings.accountSid}
                        onChange={e =>
                          setSmsSettings({ ...smsSettings, accountSid: e.target.value })
                        }
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <TextField
                        fullWidth
                        label="Auth Token"
                        type="password"
                        value={smsSettings.authToken}
                        onChange={e =>
                          setSmsSettings({ ...smsSettings, authToken: e.target.value })
                        }
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <TextField
                        fullWidth
                        label="From Number"
                        value={smsSettings.fromNumber}
                        onChange={e =>
                          setSmsSettings({ ...smsSettings, fromNumber: e.target.value })
                        }
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={smsSettings.enabled}
                            onChange={e =>
                              setSmsSettings({ ...smsSettings, enabled: e.target.checked })
                            }
                          />
                        }
                        label="Enable SMS Notifications"
                      />
                    </Grid>
                  </Grid>
                  <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
                    <UltimateButton
                      gradient="primary"
                      startIcon={<SaveIcon />}
                      onClick={handleSaveSmsSettings}
                    >
                      Save Settings
                    </UltimateButton>
                    <UltimateButton
                      variant="outlined"
                      gradient="info"
                      onClick={handleTestSms}
                      disabled={!smsSettings.enabled}
                    >
                      Send Test SMS
                    </UltimateButton>
                  </Box>
                </CardContent>
              </UltimateCard>
            </Grid>
            <Grid item xs={12} md={4}>
              <UltimateCard>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    SMS Status
                  </Typography>
                  <List>
                    <ListItem>
                      <ListItemText primary="Service Status" />
                      <ListItemSecondaryAction>
                        <UltimateStatusChip status={smsSettings.enabled ? 'success' : 'inactive'} />
                      </ListItemSecondaryAction>
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Last Test" />
                      <ListItemSecondaryAction>
                        <Typography variant="body2">1 hour ago</Typography>
                      </ListItemSecondaryAction>
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="SMS Sent Today" />
                      <ListItemSecondaryAction>
                        <Chip label="8" color="primary" size="small" />
                      </ListItemSecondaryAction>
                    </ListItem>
                  </List>
                </CardContent>
              </UltimateCard>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Health Tab */}
        <TabPanel value={activeTab} index={2}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <UltimateCard>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    KPI Defaults
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Default Frequency (minutes)"
                        type="number"
                        value={systemSettings.defaultKpiFrequency}
                        onChange={e =>
                          setSystemSettings({
                            ...systemSettings,
                            defaultKpiFrequency: parseInt(e.target.value),
                          })
                        }
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Alert Cooldown (minutes)"
                        type="number"
                        value={systemSettings.alertCooldownMinutes}
                        onChange={e =>
                          setSystemSettings({
                            ...systemSettings,
                            alertCooldownMinutes: parseInt(e.target.value),
                          })
                        }
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Max Retry Attempts"
                        type="number"
                        value={systemSettings.maxRetryAttempts}
                        onChange={e =>
                          setSystemSettings({
                            ...systemSettings,
                            maxRetryAttempts: parseInt(e.target.value),
                          })
                        }
                      />
                    </Grid>
                  </Grid>
                </CardContent>
              </UltimateCard>
            </Grid>
            <Grid item xs={12} md={6}>
              <UltimateCard>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Data Management
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Data Retention (days)"
                        type="number"
                        value={systemSettings.dataRetentionDays}
                        onChange={e =>
                          setSystemSettings({
                            ...systemSettings,
                            dataRetentionDays: parseInt(e.target.value),
                          })
                        }
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Log Level"
                        value={systemSettings.logLevel}
                        onChange={e =>
                          setSystemSettings({ ...systemSettings, logLevel: e.target.value })
                        }
                        select
                        SelectProps={{ native: true }}
                      >
                        <option value="Debug">Debug</option>
                        <option value="Info">Info</option>
                        <option value="Warning">Warning</option>
                        <option value="Error">Error</option>
                      </TextField>
                    </Grid>
                    <Grid item xs={12}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={systemSettings.enableLogging}
                            onChange={e =>
                              setSystemSettings({
                                ...systemSettings,
                                enableLogging: e.target.checked,
                              })
                            }
                          />
                        }
                        label="Enable Detailed Logging"
                      />
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12}>
              <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                <Button
                  variant="contained"
                  startIcon={<SaveIcon />}
                  onClick={handleSaveSystemSettings}
                >
                  Save System Settings
                </Button>
              </Box>
            </Grid>
          </Grid>
        </TabPanel>
      </Paper>
    </Box>
  );
};

export default Settings;
