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
  LinearProgress,
  Tabs,
  Tab,
  IconButton,
  Tooltip,
  Stack,
  Divider,
} from '@mui/material';
import {
  PlayArrow as RunIcon,
  Stop as StopIcon,
  Refresh as RefreshIcon,
  BugReport as TestIcon,
  Speed as PerformanceIcon,
  CheckCircle as PassIcon,
  Error as FailIcon,
  Schedule as PendingIcon,
  Assessment as ReportIcon,
} from '@mui/icons-material';
import { toast } from 'react-toastify';
import { testSuiteService, TestResult as ApiTestResult, TestExecutionStatus } from '@/services/testSuiteService';
import { useAuth } from '@/hooks/useAuth';
import TestRunner from './components/TestRunner';
import TestResults from './components/TestResults';
import TestConfiguration from './components/TestConfiguration';
import TestMetrics from './components/TestMetrics';

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
      id={`test-tabpanel-${index}`}
      aria-labelledby={`test-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export interface TestSuiteStatus {
  isRunning: boolean;
  totalTests: number;
  passedTests: number;
  failedTests: number;
  skippedTests: number;
  currentTest?: string;
  progress: number;
  duration: number;
  lastRun?: Date;
}

export interface TestResult {
  id: string;
  name: string;
  status: 'passed' | 'failed' | 'skipped' | 'running';
  duration: number;
  message?: string;
  stackTrace?: string;
  category: string;
}

const TestSuitePage: React.FC = () => {
  const { user } = useAuth();
  const [tabValue, setTabValue] = useState(0);
  const [testStatus, setTestStatus] = useState<TestSuiteStatus>({
    isRunning: false,
    totalTests: 12,
    passedTests: 0,
    failedTests: 0,
    skippedTests: 0,
    progress: 0,
    duration: 0,
  });
  const [testResults, setTestResults] = useState<TestResult[]>([]);
  const [selectedCategories, setSelectedCategories] = useState<string[]>(['all']);

  // Helper function to map API test results to UI format
  const mapApiTestResult = (apiResult: ApiTestResult): TestResult => ({
    id: apiResult.id,
    name: apiResult.name,
    status: apiResult.status,
    duration: apiResult.duration,
    message: apiResult.message,
    stackTrace: apiResult.stackTrace,
    category: apiResult.category,
  });

  // Load initial test suite status
  useEffect(() => {
    const loadInitialData = async () => {
      try {
        const status = await testSuiteService.getStatus();
        setTestStatus(prev => ({
          ...prev,
          totalTests: status.totalTests,
          lastRun: status.lastRun ? new Date(status.lastRun) : undefined,
          isRunning: status.isRunning,
        }));

        if (status.testResults.length > 0) {
          setTestResults(status.testResults.map(mapApiTestResult));
          const passedCount = status.testResults.filter(r => r.status === 'passed').length;
          const failedCount = status.testResults.filter(r => r.status === 'failed').length;
          setTestStatus(prev => ({
            ...prev,
            passedTests: passedCount,
            failedTests: failedCount,
          }));
        }
      } catch (error) {
        console.error('Failed to load test suite status:', error);
        toast.error('Failed to load test suite status');
      }
    };

    loadInitialData();
  }, []);

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleRunTests = async (categories: string[] = ['all']) => {
    setTestStatus(prev => ({ ...prev, isRunning: true, progress: 0 }));
    setSelectedCategories(categories);

    try {
      toast.info('Starting test execution...');

      // Call the actual test API
      const execution = await testSuiteService.runTests({ categories });

      // Poll for progress updates
      await testSuiteService.pollExecutionStatus(
        execution.executionId,
        (status: TestExecutionStatus) => {
          setTestStatus(prev => ({
            ...prev,
            progress: status.progress,
            currentTest: status.currentTest,
            duration: new Date(status.duration).getTime(),
          }));

          if (status.results) {
            setTestResults(status.results.map(mapApiTestResult));
          }
        }
      );

      toast.success('Test execution completed!');

    } catch (error: any) {
      console.error('Test execution failed:', error);
      toast.error('Failed to run tests: ' + error.message);
    } finally {
      setTestStatus(prev => ({ ...prev, isRunning: false }));
    }
  };

  const handleStopTests = async () => {
    try {
      // In a real implementation, we'd need to track the current execution ID
      // For now, just update the UI state
      setTestStatus(prev => ({ ...prev, isRunning: false }));
      toast.warning('Test execution stopped');
    } catch (error: any) {
      console.error('Failed to stop tests:', error);
      toast.error('Failed to stop tests: ' + error.message);
    }
  };



  const getStatusColor = (status: TestSuiteStatus) => {
    if (status.isRunning) return 'info';
    if (status.failedTests > 0) return 'error';
    if (status.passedTests > 0) return 'success';
    return 'default';
  };

  const getStatusIcon = (status: TestSuiteStatus) => {
    if (status.isRunning) return <PendingIcon />;
    if (status.failedTests > 0) return <FailIcon />;
    if (status.passedTests > 0) return <PassIcon />;
    return <TestIcon />;
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Test Suite Dashboard
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Manage and execute Worker Controller test suite
      </Typography>



      {/* Status Overview */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={3} alignItems="center">
            <Grid item xs={12} md={6}>
              <Stack direction="row" spacing={2} alignItems="center">
                <Chip
                  icon={getStatusIcon(testStatus)}
                  label={testStatus.isRunning ? 'Running' : 'Ready'}
                  color={getStatusColor(testStatus)}
                  size="medium"
                />
                <Typography variant="h6">
                  {testStatus.passedTests}/{testStatus.totalTests} Tests Passed
                </Typography>
              </Stack>
              
              {testStatus.isRunning && (
                <Box sx={{ mt: 2 }}>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    {testStatus.currentTest && `Running: ${testStatus.currentTest}`}
                  </Typography>
                  <LinearProgress 
                    variant="determinate" 
                    value={testStatus.progress} 
                    sx={{ height: 8, borderRadius: 4 }}
                  />
                  <Typography variant="caption" color="text.secondary">
                    {Math.round(testStatus.progress)}% Complete
                  </Typography>
                </Box>
              )}
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Stack direction="row" spacing={1} justifyContent="flex-end">
                <Button
                  variant="contained"
                  color="success"
                  startIcon={testStatus.isRunning ? <StopIcon /> : <RunIcon />}
                  onClick={testStatus.isRunning ? handleStopTests : () => handleRunTests()}
                  disabled={false}
                >
                  {testStatus.isRunning ? 'Stop Tests' : 'Run All Tests'}
                </Button>
                
                <Tooltip title="Refresh Status">
                  <IconButton onClick={() => window.location.reload()}>
                    <RefreshIcon />
                  </IconButton>
                </Tooltip>
              </Stack>
              
              {testStatus.lastRun && (
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', textAlign: 'right', mt: 1 }}>
                  Last run: {testStatus.lastRun.toLocaleString()}
                </Typography>
              )}
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Tabs */}
      <Card>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={tabValue} onChange={handleTabChange} aria-label="test suite tabs">
            <Tab icon={<RunIcon />} label="Test Runner" />
            <Tab icon={<ReportIcon />} label="Results" />
            <Tab icon={<PerformanceIcon />} label="Metrics" />
            <Tab icon={<TestIcon />} label="Configuration" />
          </Tabs>
        </Box>

        <TabPanel value={tabValue} index={0}>
          <TestRunner 
            onRunTests={handleRunTests}
            onStopTests={handleStopTests}
            testStatus={testStatus}
            testResults={testResults}
          />
        </TabPanel>

        <TabPanel value={tabValue} index={1}>
          <TestResults 
            testResults={testResults}
            testStatus={testStatus}
          />
        </TabPanel>

        <TabPanel value={tabValue} index={2}>
          <TestMetrics 
            testResults={testResults}
            testStatus={testStatus}
          />
        </TabPanel>

        <TabPanel value={tabValue} index={3}>
          <TestConfiguration 
            selectedCategories={selectedCategories}
            onCategoriesChange={setSelectedCategories}
            onRunTests={handleRunTests}
          />
        </TabPanel>
      </Card>
    </Box>
  );
};

export default TestSuitePage;
