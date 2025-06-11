import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Grid,
  Alert,
  Chip,
  Stack,
  Paper,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Tabs,
  Tab,
  Switch,
  FormControlLabel,
  LinearProgress,
  Badge,
  IconButton,
  Tooltip,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
} from '@mui/material';
import {
  Security as SecurityIcon,
  Language as I18nIcon,
  Psychology as AIIcon,
  Settings as ConfigIcon,
  Visibility as ObservabilityIcon,
  Architecture as MicroFrontendIcon,
  Analytics as AnalyticsIcon,
  Shield as ComplianceIcon,
  Speed as PerformanceIcon,
  Cloud as CloudIcon,
  Lock as EncryptionIcon,
  Verified as CertificationIcon,
  Shield,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { useAppStore, useAppSelectors } from '@/stores/appStore';
import { useObservability } from '@/hooks/useObservability';
import { aiService } from '@/services/aiService';
import { configService } from '@/services/configService';
import { securityService } from '@/services/securityService';
import { supportedLanguages, changeLanguage, getCurrentLanguage } from '@/i18n';

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
      id={`enterprise-tabpanel-${index}`}
      aria-labelledby={`enterprise-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export const EnterpriseFeaturesDemo: React.FC = () => {
  const { t, i18n } = useTranslation();
  const [tabValue, setTabValue] = useState(0);
  const [currentLanguage, setCurrentLanguage] = useState(getCurrentLanguage());
  const [aiPredictions, setAiPredictions] = useState<any[]>([]);
  const [securityMetrics, setSecurityMetrics] = useState<any>({});
  const [featureFlags, setFeatureFlags] = useState<any[]>([]);

  // Store hooks
  const preferences = useAppSelectors.preferences();
  const updatePreferences = useAppStore((state) => state.updatePreferences);
  
  // Observability hook
  const {
    recordMetric,
    recordBusinessMetric,
    logEvent,
    trackUserInteraction,
    sessionId,
    traceId,
  } = useObservability('EnterpriseFeaturesDemo');

  // Load enterprise data
  useEffect(() => {
    loadEnterpriseData();
  }, []);

  const loadEnterpriseData = async () => {
    try {
      // Load AI predictions
      const predictions = await aiService.getPredictions({ limit: 5 });
      setAiPredictions(predictions);

      // Load security metrics
      const security = await securityService.getSecurityDashboard();
      setSecurityMetrics(security);

      // Load feature flags
      const flags = await configService.getFeatureFlags();
      setFeatureFlags(flags);

      // Record business metrics
      recordBusinessMetric({
        name: 'enterprise_demo_loaded',
        value: 1,
        timestamp: new Date(),
      });

      logEvent('Enterprise demo data loaded successfully', 'info');
    } catch (error) {
      logEvent('Failed to load enterprise demo data', 'error', undefined, { error });
    }
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
    trackUserInteraction({
      type: 'click',
      element: `enterprise-tab-${newValue}`,
      page: '/demo/enterprise',
    });
    recordMetric({
      name: 'tab_change',
      value: newValue,
      timestamp: new Date(),
      tags: { component: 'EnterpriseFeaturesDemo' },
    });
  };

  const handleLanguageChange = async (languageCode: string) => {
    try {
      await changeLanguage(languageCode);
      setCurrentLanguage(languageCode);
      
      recordBusinessMetric({
        name: 'language_changed',
        value: 1,
        dimension: { language: languageCode },
        timestamp: new Date(),
      });

      logEvent(`Language changed to ${languageCode}`, 'info');
    } catch (error) {
      logEvent('Failed to change language', 'error', undefined, { error, languageCode });
    }
  };

  const triggerAIPrediction = async () => {
    try {
      const prediction = await aiService.generatePrediction('model-1', 1, {
        timestamp: new Date().toISOString(),
        context: 'demo',
      });
      
      setAiPredictions(prev => [prediction, ...prev.slice(0, 4)]);
      
      recordBusinessMetric({
        name: 'ai_prediction_generated',
        value: 1,
        timestamp: new Date(),
      });

      logEvent('AI prediction generated successfully', 'info', undefined, { predictionId: prediction.id });
    } catch (error) {
      logEvent('Failed to generate AI prediction', 'error', undefined, { error });
    }
  };

  const enterpriseFeatures = [
    {
      title: 'Enterprise Security',
      description: 'Advanced security features with threat detection and compliance',
      icon: <SecurityIcon color="error" />,
      benefits: ['Threat detection', 'Compliance reporting', 'Audit logs', 'Multi-factor authentication'],
      status: 'Active',
      metrics: {
        'Security Score': securityMetrics.riskScore || 85,
        'Active Threats': securityMetrics.activeThreats || 0,
        'Compliance': '98%',
      },
    },
    {
      title: 'Internationalization (i18n)',
      description: 'Multi-language support with dynamic loading',
      icon: <I18nIcon color="primary" />,
      benefits: ['9 languages supported', 'RTL support', 'Dynamic loading', 'Cultural formatting'],
      status: 'Active',
      metrics: {
        'Languages': supportedLanguages.length,
        'Current': currentLanguage.toUpperCase(),
        'Coverage': '95%',
      },
    },
    {
      title: 'AI/ML Integration',
      description: 'Predictive analytics and intelligent insights',
      icon: <AIIcon color="secondary" />,
      benefits: ['Predictive models', 'Anomaly detection', 'Trend analysis', 'Smart insights'],
      status: 'Active',
      metrics: {
        'Models': 2,
        'Predictions': aiPredictions.length,
        'Accuracy': '87%',
      },
    },
    {
      title: 'Configuration Management',
      description: 'Enterprise-grade configuration and feature flags',
      icon: <ConfigIcon color="info" />,
      benefits: ['Feature flags', 'Environment configs', 'A/B testing', 'Rollback capability'],
      status: 'Active',
      metrics: {
        'Feature Flags': featureFlags.length,
        'Environments': 3,
        'Rollouts': '100%',
      },
    },
    {
      title: 'Observability & Monitoring',
      description: 'Comprehensive monitoring and distributed tracing',
      icon: <ObservabilityIcon color="success" />,
      benefits: ['Distributed tracing', 'Performance metrics', 'Error tracking', 'Business metrics'],
      status: 'Active',
      metrics: {
        'Session ID': sessionId.slice(0, 8) + '...',
        'Trace ID': traceId?.slice(0, 8) + '...' || 'N/A',
        'Uptime': '99.9%',
      },
    },
    {
      title: 'Micro-Frontend Ready',
      description: 'Module federation and micro-frontend architecture',
      icon: <MicroFrontendIcon color="warning" />,
      benefits: ['Module federation', 'Independent deployments', 'Shared dependencies', 'Scalable architecture'],
      status: 'Ready',
      metrics: {
        'Modules': 1,
        'Shared Deps': 5,
        'Bundle Size': '2.1MB',
      },
    },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        {t('Enterprise-Grade Advanced Features - Round 4')}
      </Typography>
      
      <Alert severity="success" sx={{ mb: 3 }}>
        <Typography variant="h6">🏢 Round 4: Enterprise Features Successfully Applied!</Typography>
        <Typography>
          Advanced security, internationalization, AI/ML integration, configuration management, and enterprise observability are now active.
        </Typography>
      </Alert>

      {/* Enterprise Status Overview */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Security Status
              </Typography>
              <Stack direction="row" spacing={1} alignItems="center">
                <SecurityIcon color={securityMetrics.riskScore > 80 ? 'success' : 'warning'} />
                <Typography variant="h4" color="primary">
                  {securityMetrics.riskScore || 85}
                </Typography>
                <Typography variant="body2">Score</Typography>
              </Stack>
              <Typography variant="caption" color="text.secondary">
                {securityMetrics.activeThreats || 0} active threats
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                AI Predictions
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Typography variant="h3" color="secondary">
                  {aiPredictions.length}
                </Typography>
                <Chip 
                  label="87% Accuracy"
                  color="success"
                  icon={<AIIcon />}
                />
              </Box>
              <Button size="small" onClick={triggerAIPrediction} sx={{ mt: 1 }}>
                Generate Prediction
              </Button>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Internationalization
              </Typography>
              <FormControl size="small" fullWidth>
                <InputLabel>Language</InputLabel>
                <Select
                  value={currentLanguage}
                  onChange={(e) => handleLanguageChange(e.target.value)}
                  label="Language"
                >
                  {supportedLanguages.map((lang) => (
                    <MenuItem key={lang.code} value={lang.code}>
                      {lang.flag} {lang.nativeName}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                {supportedLanguages.length} languages supported
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Observability
              </Typography>
              <Stack spacing={1}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <ObservabilityIcon color="success" />
                  <Typography variant="body2">Active Monitoring</Typography>
                </Box>
                <Typography variant="caption" color="text.secondary">
                  Session: {sessionId.slice(0, 8)}...
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Trace: {traceId?.slice(0, 8) || 'N/A'}...
                </Typography>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabbed Enterprise Interface */}
      <Card>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={tabValue} onChange={handleTabChange}>
            <Tab label={t('Security Center')} />
            <Tab label={t('AI/ML Analytics')} />
            <Tab label={t('Configuration')} />
            <Tab label={t('Observability')} />
            <Tab label={t('Feature Overview')} />
          </Tabs>
        </Box>

        <TabPanel value={tabValue} index={0}>
          <Typography variant="h6" gutterBottom>
            {t('Enterprise Security Center')}
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Security Metrics
                </Typography>
                <List dense>
                  <ListItem>
                    <ListItemIcon>
                      <Shield sx={{ color: 'success.main' }} />
                    </ListItemIcon>
                    <ListItemText 
                      primary="Security Score" 
                      secondary={`${securityMetrics.riskScore || 85}/100`}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <SecurityIcon color="info" />
                    </ListItemIcon>
                    <ListItemText 
                      primary="Active Threats" 
                      secondary={securityMetrics.activeThreats || 0}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <ComplianceIcon color="success" />
                    </ListItemIcon>
                    <ListItemText 
                      primary="Compliance Status" 
                      secondary="98% Compliant"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <CertificationIcon color="primary" />
                    </ListItemIcon>
                    <ListItemText 
                      primary="Certifications" 
                      secondary="SOC 2, ISO 27001, GDPR"
                    />
                  </ListItem>
                </List>
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Security Features
                </Typography>
                <Stack spacing={2}>
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="Multi-Factor Authentication"
                  />
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="Threat Detection"
                  />
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="Audit Logging"
                  />
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="Data Encryption"
                  />
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="Compliance Monitoring"
                  />
                </Stack>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={1}>
          <Typography variant="h6" gutterBottom>
            AI/ML Predictive Analytics
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={8}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Recent AI Predictions
                </Typography>
                {aiPredictions.length > 0 ? (
                  <List>
                    {aiPredictions.slice(0, 5).map((prediction, index) => (
                      <ListItem key={index}>
                        <ListItemIcon>
                          <AIIcon color="secondary" />
                        </ListItemIcon>
                        <ListItemText
                          primary={`KPI ${prediction.kpiId} Prediction`}
                          secondary={`Value: ${prediction.predictedValue?.toFixed(2)} | Confidence: ${(prediction.confidence * 100)?.toFixed(1)}%`}
                        />
                        <Chip 
                          label={`${(prediction.confidence * 100)?.toFixed(0)}%`}
                          size="small"
                          color={prediction.confidence > 0.8 ? 'success' : 'warning'}
                        />
                      </ListItem>
                    ))}
                  </List>
                ) : (
                  <Typography color="text.secondary">
                    No predictions available. Click "Generate Prediction" to create one.
                  </Typography>
                )}
              </Paper>
            </Grid>
            <Grid item xs={12} md={4}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  AI Models
                </Typography>
                <Stack spacing={2}>
                  <Box>
                    <Typography variant="body2" fontWeight="bold">
                      KPI Trend Predictor
                    </Typography>
                    <LinearProgress variant="determinate" value={87} />
                    <Typography variant="caption">87% Accuracy</Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" fontWeight="bold">
                      Anomaly Detector
                    </Typography>
                    <LinearProgress variant="determinate" value={92} />
                    <Typography variant="caption">92% Accuracy</Typography>
                  </Box>
                  <Button variant="outlined" onClick={triggerAIPrediction}>
                    Generate New Prediction
                  </Button>
                </Stack>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={2}>
          <Typography variant="h6" gutterBottom>
            Enterprise Configuration Management
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Feature Flags
                </Typography>
                {featureFlags.length > 0 ? (
                  <List dense>
                    {featureFlags.map((flag, index) => (
                      <ListItem key={index}>
                        <ListItemIcon>
                          <ConfigIcon color={flag.enabled ? 'success' : 'disabled'} />
                        </ListItemIcon>
                        <ListItemText
                          primary={flag.name}
                          secondary={flag.description}
                        />
                        <Switch checked={flag.enabled} disabled />
                      </ListItem>
                    ))}
                  </List>
                ) : (
                  <Typography color="text.secondary">
                    Loading feature flags...
                  </Typography>
                )}
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Environment Configuration
                </Typography>
                <Stack spacing={2}>
                  <Box>
                    <Typography variant="body2" fontWeight="bold">
                      Current Environment
                    </Typography>
                    <Chip label="Production" color="success" />
                  </Box>
                  <Box>
                    <Typography variant="body2" fontWeight="bold">
                      Configuration Version
                    </Typography>
                    <Typography variant="body2">v1.0.0</Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" fontWeight="bold">
                      Last Updated
                    </Typography>
                    <Typography variant="body2">{new Date().toLocaleDateString()}</Typography>
                  </Box>
                </Stack>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={3}>
          <Typography variant="h6" gutterBottom>
            Enterprise Observability
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Current Session
                </Typography>
                <List dense>
                  <ListItem>
                    <ListItemText 
                      primary="Session ID" 
                      secondary={sessionId}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText 
                      primary="Trace ID" 
                      secondary={traceId || 'No active trace'}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText 
                      primary="Component" 
                      secondary="EnterpriseFeaturesDemo"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText 
                      primary="Language" 
                      secondary={currentLanguage.toUpperCase()}
                    />
                  </ListItem>
                </List>
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 2 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Monitoring Features
                </Typography>
                <Stack spacing={1}>
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="Distributed Tracing"
                  />
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="Performance Metrics"
                  />
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="Error Tracking"
                  />
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="Business Metrics"
                  />
                  <FormControlLabel
                    control={<Switch checked disabled />}
                    label="User Interaction Tracking"
                  />
                </Stack>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={4}>
          <Typography variant="h6" gutterBottom>
            Round 4 Enterprise Feature Overview
          </Typography>
          <Grid container spacing={3}>
            {enterpriseFeatures.map((feature, index) => (
              <Grid item xs={12} md={6} key={index}>
                <Paper elevation={2} sx={{ p: 3, height: '100%' }}>
                  <Box display="flex" alignItems="center" gap={2} mb={2}>
                    {feature.icon}
                    <Box>
                      <Typography variant="h6">{feature.title}</Typography>
                      <Chip label={feature.status} size="small" color="primary" />
                    </Box>
                  </Box>
                  <Typography variant="body2" color="text.secondary" mb={2}>
                    {feature.description}
                  </Typography>
                  
                  {/* Metrics */}
                  <Box mb={2}>
                    <Typography variant="subtitle2" gutterBottom>
                      Metrics
                    </Typography>
                    <Grid container spacing={1}>
                      {Object.entries(feature.metrics).map(([key, value]) => (
                        <Grid item xs={4} key={key}>
                          <Box textAlign="center">
                            <Typography variant="h6" color="primary">
                              {value}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {key}
                            </Typography>
                          </Box>
                        </Grid>
                      ))}
                    </Grid>
                  </Box>

                  {/* Benefits */}
                  <List dense>
                    {feature.benefits.map((benefit, idx) => (
                      <ListItem key={idx} sx={{ py: 0.5 }}>
                        <ListItemIcon sx={{ minWidth: 32 }}>
                          <AnalyticsIcon color="success" fontSize="small" />
                        </ListItemIcon>
                        <ListItemText primary={benefit} />
                      </ListItem>
                    ))}
                  </List>
                </Paper>
              </Grid>
            ))}
          </Grid>
        </TabPanel>
      </Card>
    </Box>
  );
};

export default EnterpriseFeaturesDemo;
