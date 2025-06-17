import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  FormControlLabel,
  Checkbox,
  TextField,
  Button,
  Divider,
  Alert,
  Slider
} from '@mui/material';
import {
  Save,
  RestoreFromTrash
} from '@mui/icons-material';

interface TestConfiguration {
  includePerformanceTests: boolean;
  includeSecurityTests: boolean;
  includeValidationTests: boolean;
  runInParallel: boolean;
  maxParallelTests: number;
  timeoutSeconds: number;
  retryAttempts: number;
  enableDetailedLogging: boolean;
  saveResults: boolean;
}

interface ConfigurationTabProps {
  configuration: TestConfiguration;
  onConfigurationChange: (config: TestConfiguration) => void;
  onSaveConfiguration: () => void;
  onResetConfiguration: () => void;
}

export const ConfigurationTab: React.FC<ConfigurationTabProps> = ({
  configuration,
  onConfigurationChange,
  onSaveConfiguration,
  onResetConfiguration
}) => {
  const handleCheckboxChange = (field: keyof TestConfiguration) => (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    onConfigurationChange({
      ...configuration,
      [field]: event.target.checked
    });
  };

  const handleNumberChange = (field: keyof TestConfiguration) => (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value = parseInt(event.target.value, 10);
    if (!isNaN(value)) {
      onConfigurationChange({
        ...configuration,
        [field]: value
      });
    }
  };

  const handleSliderChange = (field: keyof TestConfiguration) => (
    event: Event,
    newValue: number | number[]
  ) => {
    onConfigurationChange({
      ...configuration,
      [field]: newValue as number
    });
  };

  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Test Configuration
            </Typography>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Configure how tests are executed and what types of validation to perform.
            </Typography>

            <Box sx={{ mt: 3 }}>
              {/* Test Types */}
              <Typography variant="subtitle1" gutterBottom>
                Test Types
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6} md={4}>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={configuration.includePerformanceTests}
                        onChange={handleCheckboxChange('includePerformanceTests')}
                      />
                    }
                    label="Performance Tests"
                  />
                </Grid>
                <Grid item xs={12} sm={6} md={4}>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={configuration.includeSecurityTests}
                        onChange={handleCheckboxChange('includeSecurityTests')}
                      />
                    }
                    label="Security Tests"
                  />
                </Grid>
                <Grid item xs={12} sm={6} md={4}>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={configuration.includeValidationTests}
                        onChange={handleCheckboxChange('includeValidationTests')}
                      />
                    }
                    label="Validation Tests"
                  />
                </Grid>
              </Grid>

              <Divider sx={{ my: 3 }} />

              {/* Execution Settings */}
              <Typography variant="subtitle1" gutterBottom>
                Execution Settings
              </Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6}>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={configuration.runInParallel}
                        onChange={handleCheckboxChange('runInParallel')}
                      />
                    }
                    label="Run Tests in Parallel"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box>
                    <Typography variant="body2" gutterBottom>
                      Max Parallel Tests: {configuration.maxParallelTests}
                    </Typography>
                    <Slider
                      value={configuration.maxParallelTests}
                      onChange={handleSliderChange('maxParallelTests')}
                      min={1}
                      max={10}
                      step={1}
                      marks
                      disabled={!configuration.runInParallel}
                      valueLabelDisplay="auto"
                    />
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    label="Timeout (seconds)"
                    type="number"
                    value={configuration.timeoutSeconds}
                    onChange={handleNumberChange('timeoutSeconds')}
                    fullWidth
                    inputProps={{ min: 1, max: 300 }}
                    helperText="Maximum time to wait for each test"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    label="Retry Attempts"
                    type="number"
                    value={configuration.retryAttempts}
                    onChange={handleNumberChange('retryAttempts')}
                    fullWidth
                    inputProps={{ min: 0, max: 5 }}
                    helperText="Number of retries for failed tests"
                  />
                </Grid>
              </Grid>

              <Divider sx={{ my: 3 }} />

              {/* Logging and Storage */}
              <Typography variant="subtitle1" gutterBottom>
                Logging and Storage
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={configuration.enableDetailedLogging}
                        onChange={handleCheckboxChange('enableDetailedLogging')}
                      />
                    }
                    label="Enable Detailed Logging"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={configuration.saveResults}
                        onChange={handleCheckboxChange('saveResults')}
                      />
                    }
                    label="Save Test Results"
                  />
                </Grid>
              </Grid>

              <Divider sx={{ my: 3 }} />

              {/* Configuration Actions */}
              <Box display="flex" gap={2}>
                <Button
                  variant="contained"
                  startIcon={<Save />}
                  onClick={onSaveConfiguration}
                >
                  Save Configuration
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<RestoreFromTrash />}
                  onClick={onResetConfiguration}
                >
                  Reset to Defaults
                </Button>
              </Box>

              <Alert severity="info" sx={{ mt: 2 }}>
                <Typography variant="body2">
                  <strong>Performance Tests:</strong> Measure response times, throughput, and resource usage.
                  <br />
                  <strong>Security Tests:</strong> Check for common vulnerabilities and authentication issues.
                  <br />
                  <strong>Validation Tests:</strong> Verify request/response formats and business logic.
                </Typography>
              </Alert>
            </Box>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
};
