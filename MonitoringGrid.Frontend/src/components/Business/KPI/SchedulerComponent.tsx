import React, { useState, useEffect } from 'react';
import {
  Box,
  CardContent,
  Typography,
  Grid,
  Chip,
  Alert,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  FormControlLabel,
  Switch,
  Tooltip,
  IconButton,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Schedule as ScheduleIcon,
  Info as InfoIcon,
  PlayArrow as PreviewIcon,
} from '@mui/icons-material';
import { ScheduleConfiguration, ScheduleType, CronPreset } from '@/types/api';
import {
  validateCronExpression,
  calculateNextExecutions,
  getScheduleDescription,
  COMMON_CRON_PRESETS,
} from '@/utils/schedulerUtils';
import { UltimateCard, UltimateInputField, UltimateSelect } from '@/components/UltimateEnterprise';

interface SchedulerComponentProps {
  value: ScheduleConfiguration;
  onChange: (schedule: ScheduleConfiguration) => void;
  disabled?: boolean;
  showAdvanced?: boolean;
}

const CRON_PRESETS = COMMON_CRON_PRESETS;

const INTERVAL_PRESETS = [
  { label: '1 minute', value: 1 },
  { label: '5 minutes', value: 5 },
  { label: '15 minutes', value: 15 },
  { label: '30 minutes', value: 30 },
  { label: '1 hour', value: 60 },
  { label: '2 hours', value: 120 },
  { label: '6 hours', value: 360 },
  { label: '12 hours', value: 720 },
  { label: '24 hours', value: 1440 },
];

export const SchedulerComponent: React.FC<SchedulerComponentProps> = ({
  value,
  onChange,
  disabled = false,
  showAdvanced = true,
}) => {
  const [cronError, setCronError] = useState<string>('');
  const [nextRuns, setNextRuns] = useState<string[]>([]);

  // Validate cron expression using utility
  const validateCron = (expression: string): boolean => {
    const validation = validateCronExpression(expression);
    if (!validation.isValid && validation.error) {
      setCronError(validation.error);
      return false;
    }
    setCronError('');
    return true;
  };

  // Calculate next run times using utility
  const calculateNextRuns = () => {
    const executions = calculateNextExecutions(value, 3);
    const runs = executions.map(date => date.toLocaleString());
    setNextRuns(runs);
  };

  useEffect(() => {
    calculateNextRuns();
  }, [value]);

  const handleScheduleTypeChange = (type: ScheduleType) => {
    const newSchedule: ScheduleConfiguration = {
      ...value,
      scheduleType: type,
      cronExpression: type === ScheduleType.Cron ? '*/5 * * * *' : undefined,
      intervalMinutes: type === ScheduleType.Interval ? 5 : undefined,
      startDate: type === ScheduleType.OneTime ? new Date().toISOString().slice(0, 16) : undefined,
    };
    onChange(newSchedule);
  };

  const handleCronPresetSelect = (preset: CronPreset) => {
    onChange({
      ...value,
      cronExpression: preset.expression,
    });
  };

  const handleIntervalPresetSelect = (minutes: number) => {
    onChange({
      ...value,
      intervalMinutes: minutes,
    });
  };

  return (
    <UltimateCard>
      <CardContent>
        <Box display="flex" alignItems="center" gap={1} mb={2}>
          <ScheduleIcon color="primary" />
          <Typography variant="h6">Schedule Configuration</Typography>
          <Tooltip title="Configure when this KPI should be executed">
            <IconButton size="small">
              <InfoIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>

        <Grid container spacing={3}>
          {/* Enable/Disable Schedule */}
          <Grid item xs={12}>
            <FormControlLabel
              control={
                <Switch
                  checked={value.isEnabled}
                  onChange={e => onChange({ ...value, isEnabled: e.target.checked })}
                  disabled={disabled}
                />
              }
              label="Enable Scheduling"
            />
          </Grid>

          {value.isEnabled && (
            <>
              {/* Schedule Type Selection */}
              <Grid item xs={12} md={6}>
                <UltimateSelect
                  fullWidth
                  label="Schedule Type"
                  value={value.scheduleType}
                  onChange={e => handleScheduleTypeChange(e.target.value as ScheduleType)}
                  disabled={disabled}
                  options={[
                    { value: ScheduleType.Interval, label: 'Interval (Every X minutes)' },
                    { value: ScheduleType.Cron, label: 'Cron Expression' },
                    { value: ScheduleType.OneTime, label: 'One Time' },
                  ]}
                />
              </Grid>

              {/* Interval Configuration */}
              {value.scheduleType === ScheduleType.Interval && (
                <>
                  <Grid item xs={12} md={6}>
                    <UltimateInputField
                      fullWidth
                      label="Interval (minutes)"
                      type="number"
                      value={value.intervalMinutes || ''}
                      onChange={e =>
                        onChange({
                          ...value,
                          intervalMinutes: parseInt(e.target.value) || undefined,
                        })
                      }
                      disabled={disabled}
                      inputProps={{ min: 1, max: 10080 }} // Max 1 week
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Typography variant="subtitle2" gutterBottom>
                      Quick Presets:
                    </Typography>
                    <Box display="flex" flexWrap="wrap" gap={1}>
                      {INTERVAL_PRESETS.map(preset => (
                        <Chip
                          key={preset.value}
                          label={preset.label}
                          onClick={() => handleIntervalPresetSelect(preset.value)}
                          variant={value.intervalMinutes === preset.value ? 'filled' : 'outlined'}
                          color="primary"
                          size="small"
                          disabled={disabled}
                        />
                      ))}
                    </Box>
                  </Grid>
                </>
              )}

              {/* Cron Configuration */}
              {value.scheduleType === ScheduleType.Cron && (
                <>
                  <Grid item xs={12}>
                    <UltimateInputField
                      fullWidth
                      label="Cron Expression"
                      value={value.cronExpression || ''}
                      onChange={e => {
                        const expression = e.target.value;
                        validateCron(expression);
                        onChange({ ...value, cronExpression: expression });
                      }}
                      disabled={disabled}
                      error={!!cronError}
                      helperText={
                        cronError || 'Format: minute hour day month weekday (e.g., */5 * * * *)'
                      }
                      placeholder="*/5 * * * *"
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Typography variant="subtitle2" gutterBottom>
                      Common Patterns:
                    </Typography>
                    <Box display="flex" flexWrap="wrap" gap={1}>
                      {CRON_PRESETS.map(preset => (
                        <Tooltip key={preset.expression} title={preset.description}>
                          <Chip
                            label={preset.name}
                            onClick={() => handleCronPresetSelect(preset)}
                            variant={
                              value.cronExpression === preset.expression ? 'filled' : 'outlined'
                            }
                            color="primary"
                            size="small"
                            disabled={disabled}
                          />
                        </Tooltip>
                      ))}
                    </Box>
                  </Grid>
                </>
              )}

              {/* One Time Configuration */}
              {value.scheduleType === ScheduleType.OneTime && (
                <Grid item xs={12} md={6}>
                  <UltimateInputField
                    fullWidth
                    label="Execution Date & Time"
                    type="datetime-local"
                    value={value.startDate?.slice(0, 16) || ''}
                    onChange={e =>
                      onChange({
                        ...value,
                        startDate: e.target.value
                          ? new Date(e.target.value).toISOString()
                          : undefined,
                      })
                    }
                    disabled={disabled}
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
              )}

              {/* Advanced Options */}
              {showAdvanced && (
                <Grid item xs={12}>
                  <Accordion>
                    <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                      <Typography variant="subtitle2">Advanced Options</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                      <Grid container spacing={2}>
                        <Grid item xs={12} md={6}>
                          <UltimateInputField
                            fullWidth
                            label="Start Date"
                            type="datetime-local"
                            value={value.startDate?.slice(0, 16) || ''}
                            onChange={e =>
                              onChange({
                                ...value,
                                startDate: e.target.value
                                  ? new Date(e.target.value).toISOString()
                                  : undefined,
                              })
                            }
                            disabled={disabled}
                            InputLabelProps={{ shrink: true }}
                            helperText="When to start the schedule (optional)"
                          />
                        </Grid>
                        <Grid item xs={12} md={6}>
                          <UltimateInputField
                            fullWidth
                            label="End Date"
                            type="datetime-local"
                            value={value.endDate?.slice(0, 16) || ''}
                            onChange={e =>
                              onChange({
                                ...value,
                                endDate: e.target.value
                                  ? new Date(e.target.value).toISOString()
                                  : undefined,
                              })
                            }
                            disabled={disabled}
                            InputLabelProps={{ shrink: true }}
                            helperText="When to stop the schedule (optional)"
                          />
                        </Grid>
                        <Grid item xs={12} md={6}>
                          <UltimateInputField
                            fullWidth
                            label="Timezone"
                            value={value.timezone || 'UTC'}
                            onChange={e => onChange({ ...value, timezone: e.target.value })}
                            disabled={disabled}
                            helperText="Timezone for schedule execution"
                          />
                        </Grid>
                      </Grid>
                    </AccordionDetails>
                  </Accordion>
                </Grid>
              )}

              {/* Preview */}
              <Grid item xs={12}>
                <Box display="flex" alignItems="center" gap={1} mb={1}>
                  <PreviewIcon color="action" />
                  <Typography variant="subtitle2">Next Executions Preview:</Typography>
                </Box>
                {nextRuns.length > 0 ? (
                  <Box>
                    {nextRuns.map((run, index) => (
                      <Chip
                        key={index}
                        label={run}
                        variant="outlined"
                        size="small"
                        sx={{ mr: 1, mb: 1 }}
                      />
                    ))}
                  </Box>
                ) : (
                  <Alert severity="info" sx={{ mt: 1 }}>
                    Configure schedule to see preview
                  </Alert>
                )}
              </Grid>
            </>
          )}
        </Grid>
      </CardContent>
    </UltimateCard>
  );
};

export default SchedulerComponent;
