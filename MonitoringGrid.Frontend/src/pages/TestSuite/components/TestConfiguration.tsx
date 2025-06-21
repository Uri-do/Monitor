import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  FormGroup,
  FormControlLabel,
  Checkbox,
  Button,
  Grid,
  Stack,
  Divider,
  TextField,
  Switch,
  Alert,
  Chip,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  PlayArrow as RunIcon,
  Settings as ConfigIcon,
  Speed as PerformanceIcon,
  BugReport as UnitIcon,
  Code as FrameworkIcon,
  ExpandMore as ExpandIcon,
  Save as SaveIcon,
  Restore as ResetIcon,
} from '@mui/icons-material';

interface TestConfigurationProps {
  selectedCategories: string[];
  onCategoriesChange: (categories: string[]) => void;
  onRunTests: (categories: string[]) => Promise<void>;
}

interface TestCategory {
  id: string;
  name: string;
  description: string;
  icon: React.ReactNode;
  testCount: number;
  enabled: boolean;
}

interface TestSettings {
  parallel: boolean;
  maxParallelism: number;
  timeout: number;
  retryFailedTests: boolean;
  maxRetries: number;
  generateReport: boolean;
  verboseOutput: boolean;
  stopOnFirstFailure: boolean;
}

const TestConfiguration: React.FC<TestConfigurationProps> = ({
  selectedCategories,
  onCategoriesChange,
  onRunTests,
}) => {
  const [testCategories, setTestCategories] = useState<TestCategory[]>([
    {
      id: 'Framework',
      name: 'Framework Tests',
      description: 'Tests that validate the testing framework itself (xUnit, Moq, FluentAssertions)',
      icon: <FrameworkIcon />,
      testCount: 5,
      enabled: selectedCategories.includes('Framework') || selectedCategories.includes('all'),
    },
    {
      id: 'Unit',
      name: 'Unit Tests',
      description: 'Unit tests for Worker Controller methods and business logic',
      icon: <UnitIcon />,
      testCount: 4,
      enabled: selectedCategories.includes('Unit') || selectedCategories.includes('all'),
    },
    {
      id: 'Performance',
      name: 'Performance Tests',
      description: 'Performance and load tests to ensure optimal response times',
      icon: <PerformanceIcon />,
      testCount: 3,
      enabled: selectedCategories.includes('Performance') || selectedCategories.includes('all'),
    },
  ]);

  const [testSettings, setTestSettings] = useState<TestSettings>({
    parallel: true,
    maxParallelism: 4,
    timeout: 30000,
    retryFailedTests: false,
    maxRetries: 2,
    generateReport: true,
    verboseOutput: false,
    stopOnFirstFailure: false,
  });

  const [isRunning, setIsRunning] = useState(false);

  const handleCategoryToggle = (categoryId: string) => {
    const updatedCategories = testCategories.map(cat =>
      cat.id === categoryId ? { ...cat, enabled: !cat.enabled } : cat
    );
    setTestCategories(updatedCategories);

    const enabledCategories = updatedCategories
      .filter(cat => cat.enabled)
      .map(cat => cat.id);
    
    onCategoriesChange(enabledCategories.length === 0 ? [] : enabledCategories);
  };

  const handleSelectAll = () => {
    const allEnabled = testCategories.every(cat => cat.enabled);
    const updatedCategories = testCategories.map(cat => ({ ...cat, enabled: !allEnabled }));
    setTestCategories(updatedCategories);
    
    if (allEnabled) {
      onCategoriesChange([]);
    } else {
      onCategoriesChange(['all']);
    }
  };

  const handleSettingChange = (setting: keyof TestSettings, value: any) => {
    setTestSettings(prev => ({ ...prev, [setting]: value }));
  };

  const handleRunSelectedTests = async () => {
    const enabledCategories = testCategories
      .filter(cat => cat.enabled)
      .map(cat => cat.id);
    
    if (enabledCategories.length === 0) {
      return;
    }

    setIsRunning(true);
    try {
      await onRunTests(enabledCategories);
    } finally {
      setIsRunning(false);
    }
  };

  const resetToDefaults = () => {
    setTestSettings({
      parallel: true,
      maxParallelism: 4,
      timeout: 30000,
      retryFailedTests: false,
      maxRetries: 2,
      generateReport: true,
      verboseOutput: false,
      stopOnFirstFailure: false,
    });

    const resetCategories = testCategories.map(cat => ({ ...cat, enabled: true }));
    setTestCategories(resetCategories);
    onCategoriesChange(['all']);
  };

  const enabledCount = testCategories.filter(cat => cat.enabled).length;
  const totalTestCount = testCategories.filter(cat => cat.enabled).reduce((sum, cat) => sum + cat.testCount, 0);

  return (
    <Box>
      <Grid container spacing={3}>
        {/* Test Categories */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 2 }}>
                <ConfigIcon color="primary" />
                <Typography variant="h6">Test Categories</Typography>
              </Stack>

              <Stack spacing={2}>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Typography variant="body2" color="text.secondary">
                    Select test categories to run
                  </Typography>
                  <Button size="small" onClick={handleSelectAll}>
                    {testCategories.every(cat => cat.enabled) ? 'Deselect All' : 'Select All'}
                  </Button>
                </Box>

                <FormGroup>
                  {testCategories.map((category) => (
                    <Card key={category.id} variant="outlined" sx={{ mb: 1 }}>
                      <CardContent sx={{ py: 2 }}>
                        <FormControlLabel
                          control={
                            <Checkbox
                              checked={category.enabled}
                              onChange={() => handleCategoryToggle(category.id)}
                            />
                          }
                          label={
                            <Box>
                              <Stack direction="row" alignItems="center" spacing={1}>
                                {category.icon}
                                <Typography variant="body2" fontWeight="medium">
                                  {category.name}
                                </Typography>
                                <Chip size="small" label={`${category.testCount} tests`} />
                              </Stack>
                              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
                                {category.description}
                              </Typography>
                            </Box>
                          }
                        />
                      </CardContent>
                    </Card>
                  ))}
                </FormGroup>

                <Divider />

                <Box>
                  <Typography variant="body2" color="text.secondary">
                    Selected: {enabledCount} categories, {totalTestCount} tests
                  </Typography>
                </Box>

                <Button
                  variant="contained"
                  startIcon={<RunIcon />}
                  onClick={handleRunSelectedTests}
                  disabled={enabledCount === 0 || isRunning}
                  fullWidth
                  size="large"
                >
                  {isRunning ? 'Running Tests...' : `Run Selected Tests (${totalTestCount})`}
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Test Settings */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Test Execution Settings
              </Typography>

              <Stack spacing={3}>
                {/* Execution Settings */}
                <Accordion defaultExpanded>
                  <AccordionSummary expandIcon={<ExpandIcon />}>
                    <Typography variant="subtitle2">Execution Options</Typography>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Stack spacing={2}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={testSettings.parallel}
                            onChange={(e) => handleSettingChange('parallel', e.target.checked)}
                          />
                        }
                        label="Run tests in parallel"
                      />

                      {testSettings.parallel && (
                        <TextField
                          label="Max Parallelism"
                          type="number"
                          value={testSettings.maxParallelism}
                          onChange={(e) => handleSettingChange('maxParallelism', parseInt(e.target.value))}
                          size="small"
                          inputProps={{ min: 1, max: 16 }}
                          helperText="Number of tests to run simultaneously"
                        />
                      )}

                      <TextField
                        label="Test Timeout (ms)"
                        type="number"
                        value={testSettings.timeout}
                        onChange={(e) => handleSettingChange('timeout', parseInt(e.target.value))}
                        size="small"
                        inputProps={{ min: 1000, max: 300000 }}
                        helperText="Maximum time to wait for each test"
                      />

                      <FormControlLabel
                        control={
                          <Switch
                            checked={testSettings.stopOnFirstFailure}
                            onChange={(e) => handleSettingChange('stopOnFirstFailure', e.target.checked)}
                          />
                        }
                        label="Stop on first failure"
                      />
                    </Stack>
                  </AccordionDetails>
                </Accordion>

                {/* Retry Settings */}
                <Accordion>
                  <AccordionSummary expandIcon={<ExpandIcon />}>
                    <Typography variant="subtitle2">Retry Options</Typography>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Stack spacing={2}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={testSettings.retryFailedTests}
                            onChange={(e) => handleSettingChange('retryFailedTests', e.target.checked)}
                          />
                        }
                        label="Retry failed tests"
                      />

                      {testSettings.retryFailedTests && (
                        <TextField
                          label="Max Retries"
                          type="number"
                          value={testSettings.maxRetries}
                          onChange={(e) => handleSettingChange('maxRetries', parseInt(e.target.value))}
                          size="small"
                          inputProps={{ min: 1, max: 5 }}
                          helperText="Number of times to retry failed tests"
                        />
                      )}
                    </Stack>
                  </AccordionDetails>
                </Accordion>

                {/* Output Settings */}
                <Accordion>
                  <AccordionSummary expandIcon={<ExpandIcon />}>
                    <Typography variant="subtitle2">Output Options</Typography>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Stack spacing={2}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={testSettings.generateReport}
                            onChange={(e) => handleSettingChange('generateReport', e.target.checked)}
                          />
                        }
                        label="Generate test report"
                      />

                      <FormControlLabel
                        control={
                          <Switch
                            checked={testSettings.verboseOutput}
                            onChange={(e) => handleSettingChange('verboseOutput', e.target.checked)}
                          />
                        }
                        label="Verbose output"
                      />
                    </Stack>
                  </AccordionDetails>
                </Accordion>

                <Divider />

                <Stack direction="row" spacing={2}>
                  <Button
                    variant="outlined"
                    startIcon={<SaveIcon />}
                    onClick={() => {
                      // Save settings to localStorage
                      localStorage.setItem('testSettings', JSON.stringify(testSettings));
                      localStorage.setItem('testCategories', JSON.stringify(testCategories));
                    }}
                    size="small"
                  >
                    Save Settings
                  </Button>
                  
                  <Button
                    variant="outlined"
                    startIcon={<ResetIcon />}
                    onClick={resetToDefaults}
                    size="small"
                  >
                    Reset to Defaults
                  </Button>
                </Stack>
              </Stack>
            </CardContent>
          </Card>

          {/* Configuration Summary */}
          <Card sx={{ mt: 2 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Current Configuration
              </Typography>
              
              <Alert severity="info" sx={{ mb: 2 }}>
                <Typography variant="body2">
                  Ready to run {totalTestCount} tests from {enabledCount} categories
                  {testSettings.parallel ? ` in parallel (max ${testSettings.maxParallelism})` : ' sequentially'}.
                </Typography>
              </Alert>

              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <Typography variant="caption" color="text.secondary">
                    Execution Mode
                  </Typography>
                  <Typography variant="body2">
                    {testSettings.parallel ? 'Parallel' : 'Sequential'}
                  </Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="text.secondary">
                    Timeout
                  </Typography>
                  <Typography variant="body2">
                    {testSettings.timeout / 1000}s
                  </Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="text.secondary">
                    Retry Failed
                  </Typography>
                  <Typography variant="body2">
                    {testSettings.retryFailedTests ? `Yes (${testSettings.maxRetries}x)` : 'No'}
                  </Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="text.secondary">
                    Generate Report
                  </Typography>
                  <Typography variant="body2">
                    {testSettings.generateReport ? 'Yes' : 'No'}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default TestConfiguration;
