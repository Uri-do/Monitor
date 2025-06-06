import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Paper,
  Divider,
  Alert,
  Button,
  Stack,
} from '@mui/material';
import {
  Schedule as ScheduleIcon,
  Assessment as KpiIcon,
  Preview as PreviewIcon,
} from '@mui/icons-material';
import { 
  SchedulerComponent, 
  KpiTypeSelector,
  PageHeader 
} from '@/components/Common';
import { 
  ScheduleConfiguration, 
  ScheduleType, 
  KpiType 
} from '@/types/api';
import { 
  getScheduleDescription, 
  isValidScheduleConfiguration 
} from '@/utils/schedulerUtils';
import { 
  validateKpiConfiguration, 
  getRecommendedConfiguration,
  getKpiTypeExamples 
} from '@/utils/kpiTypeUtils';

const SchedulerDemo: React.FC = () => {
  const [scheduleConfig, setScheduleConfig] = useState<ScheduleConfiguration>({
    scheduleType: ScheduleType.Interval,
    intervalMinutes: 60,
    isEnabled: true,
    timezone: 'UTC',
  });

  const [kpiType, setKpiType] = useState<KpiType>(KpiType.SuccessRate);
  const [thresholdValue, setThresholdValue] = useState<number | undefined>(100);
  const [comparisonOperator, setComparisonOperator] = useState<'gt' | 'lt' | 'eq' | 'gte' | 'lte'>('gt');

  // Validate configurations
  const scheduleValidation = isValidScheduleConfiguration(scheduleConfig);
  const kpiValidation = validateKpiConfiguration(kpiType, {
    deviation: 10,
    thresholdValue,
    comparisonOperator,
    minimumThreshold: 10,
    lastMinutes: 1440,
  });

  const recommendedConfig = getRecommendedConfiguration(kpiType);
  const examples = getKpiTypeExamples(kpiType);

  const handleApplyRecommended = () => {
    if (recommendedConfig.thresholdValue) {
      setThresholdValue(recommendedConfig.thresholdValue);
    }
    if (recommendedConfig.comparisonOperator) {
      setComparisonOperator(recommendedConfig.comparisonOperator);
    }
  };

  return (
    <Box>
      <PageHeader
        title="Scheduler & KPI Type Demo"
        subtitle="Interactive demonstration of the new scheduler and KPI type components"
        breadcrumbs={[
          { label: 'Demo', href: '/demo' },
          { label: 'Scheduler & KPI Types' },
        ]}
      />

      <Grid container spacing={3}>
        {/* Scheduler Component Demo */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" gap={1} mb={2}>
                <ScheduleIcon color="primary" />
                <Typography variant="h6">Scheduler Component</Typography>
              </Box>
              
              <SchedulerComponent
                value={scheduleConfig}
                onChange={setScheduleConfig}
                showAdvanced={true}
              />

              <Divider sx={{ my: 2 }} />

              <Typography variant="subtitle2" gutterBottom>
                Configuration Summary:
              </Typography>
              <Paper sx={{ p: 2, bgcolor: 'grey.50' }}>
                <Typography variant="body2">
                  <strong>Description:</strong> {getScheduleDescription(scheduleConfig)}
                </Typography>
                <Typography variant="body2" sx={{ mt: 1 }}>
                  <strong>Enabled:</strong> {scheduleConfig.isEnabled ? 'Yes' : 'No'}
                </Typography>
                <Typography variant="body2" sx={{ mt: 1 }}>
                  <strong>Timezone:</strong> {scheduleConfig.timezone || 'UTC'}
                </Typography>
                {scheduleConfig.startDate && (
                  <Typography variant="body2" sx={{ mt: 1 }}>
                    <strong>Start Date:</strong> {new Date(scheduleConfig.startDate).toLocaleString()}
                  </Typography>
                )}
                {scheduleConfig.endDate && (
                  <Typography variant="body2" sx={{ mt: 1 }}>
                    <strong>End Date:</strong> {new Date(scheduleConfig.endDate).toLocaleString()}
                  </Typography>
                )}
              </Paper>

              {!scheduleValidation.isValid && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  <Typography variant="body2" fontWeight="medium">
                    Configuration Issues:
                  </Typography>
                  <ul style={{ margin: '8px 0', paddingLeft: '20px' }}>
                    {scheduleValidation.errors.map((error, index) => (
                      <li key={index}>
                        <Typography variant="body2">{error}</Typography>
                      </li>
                    ))}
                  </ul>
                </Alert>
              )}

              {scheduleValidation.isValid && scheduleConfig.isEnabled && (
                <Alert severity="success" sx={{ mt: 2 }}>
                  Schedule configuration is valid and ready to use!
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* KPI Type Selector Demo */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" gap={1} mb={2}>
                <KpiIcon color="primary" />
                <Typography variant="h6">KPI Type Selector</Typography>
              </Box>
              
              <KpiTypeSelector
                selectedType={kpiType}
                onTypeChange={setKpiType}
                thresholdValue={thresholdValue}
                onThresholdChange={setThresholdValue}
                comparisonOperator={comparisonOperator}
                onOperatorChange={setComparisonOperator}
              />

              <Divider sx={{ my: 2 }} />

              <Typography variant="subtitle2" gutterBottom>
                Recommended Configuration:
              </Typography>
              <Paper sx={{ p: 2, bgcolor: 'grey.50' }}>
                <Typography variant="body2" sx={{ mb: 1 }}>
                  {recommendedConfig.description}
                </Typography>
                <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
                  <Button
                    size="small"
                    variant="outlined"
                    onClick={handleApplyRecommended}
                    startIcon={<PreviewIcon />}
                  >
                    Apply Recommended
                  </Button>
                </Stack>
              </Paper>

              <Typography variant="subtitle2" gutterBottom sx={{ mt: 2 }}>
                Example Use Cases:
              </Typography>
              <Paper sx={{ p: 2, bgcolor: 'grey.50' }}>
                <ul style={{ margin: 0, paddingLeft: '20px' }}>
                  {examples.map((example, index) => (
                    <li key={index}>
                      <Typography variant="body2">{example}</Typography>
                    </li>
                  ))}
                </ul>
              </Paper>

              {!kpiValidation.isValid && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  <Typography variant="body2" fontWeight="medium">
                    Configuration Issues:
                  </Typography>
                  <ul style={{ margin: '8px 0', paddingLeft: '20px' }}>
                    {kpiValidation.errors.map((error, index) => (
                      <li key={index}>
                        <Typography variant="body2">{error}</Typography>
                      </li>
                    ))}
                  </ul>
                </Alert>
              )}

              {kpiValidation.isValid && (
                <Alert severity="success" sx={{ mt: 2 }}>
                  KPI type configuration is valid!
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Combined Configuration Preview */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Combined Configuration Preview
              </Typography>
              
              <Paper sx={{ p: 3, bgcolor: 'grey.50' }}>
                <Grid container spacing={2}>
                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" gutterBottom>
                      Schedule Configuration:
                    </Typography>
                    <Typography variant="body2">
                      {getScheduleDescription(scheduleConfig)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                      Status: {scheduleValidation.isValid ? '✅ Valid' : '❌ Invalid'}
                    </Typography>
                  </Grid>
                  
                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" gutterBottom>
                      KPI Type Configuration:
                    </Typography>
                    <Typography variant="body2">
                      Type: {kpiType.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase())}
                    </Typography>
                    {thresholdValue !== undefined && (
                      <Typography variant="body2">
                        Threshold: {comparisonOperator} {thresholdValue}
                      </Typography>
                    )}
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                      Status: {kpiValidation.isValid ? '✅ Valid' : '❌ Invalid'}
                    </Typography>
                  </Grid>
                </Grid>

                {scheduleValidation.isValid && kpiValidation.isValid && (
                  <Alert severity="info" sx={{ mt: 2 }}>
                    <Typography variant="body2">
                      🎉 Both configurations are valid! This KPI would run {getScheduleDescription(scheduleConfig).toLowerCase()} 
                      and monitor {kpiType.replace('_', ' ')} metrics.
                    </Typography>
                  </Alert>
                )}
              </Paper>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default SchedulerDemo;
