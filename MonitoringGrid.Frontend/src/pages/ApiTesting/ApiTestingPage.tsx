import React, { useState } from 'react';
import {
  Box,
  Typography,
  Tabs,
  Tab,
  Alert,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControlLabel,
  Checkbox,
  Button
} from '@mui/material';
import {
  Speed,
  Security,
  BugReport,
  Analytics,
  History,
  Settings,
  ArrowBack
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiTestingService } from '../../services/apiTestingService';
import { TestableEndpoint, TestExecutionResult } from '../../types/testing';

// Import tab components
import {
  EndpointsTab,
  TestResultsTab,
  StatisticsTab,
  HistoryTab,
  ConfigurationTab
} from './components';

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
      id={`api-testing-tabpanel-${index}`}
      aria-labelledby={`api-testing-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

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

export const ApiTestingPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [selectedEndpoints, setSelectedEndpoints] = useState<string[]>([]);
  const [testResults, setTestResults] = useState<TestExecutionResult[]>([]);
  const [isRunningTests, setIsRunningTests] = useState(false);
  const [showTestDialog, setShowTestDialog] = useState(false);
  const [selectedEndpoint, setSelectedEndpoint] = useState<TestableEndpoint | null>(null);
  const [testConfiguration, setTestConfiguration] = useState<TestConfiguration>({
    includePerformanceTests: true,
    includeSecurityTests: true,
    includeValidationTests: true,
    runInParallel: false,
    maxParallelTests: 3,
    timeoutSeconds: 30,
    retryAttempts: 1,
    enableDetailedLogging: false,
    saveResults: true
  });

  const queryClient = useQueryClient();

  // Fetch testable endpoints
  const { data: endpoints, isLoading: endpointsLoading, error: endpointsError } = useQuery({
    queryKey: ['testable-endpoints'],
    queryFn: apiTestingService.getTestableEndpoints
  });

  // Fetch test statistics
  const { data: statistics, isLoading: statisticsLoading } = useQuery({
    queryKey: ['test-statistics'],
    queryFn: () => apiTestingService.getTestStatistics({ days: 7 })
  });

  // Fetch test history
  const { data: history, isLoading: historyLoading } = useQuery({
    queryKey: ['test-history'],
    queryFn: () => apiTestingService.getTestHistory({ days: 7, pageSize: 50 })
  });

  // Execute single test mutation
  const executeSingleTestMutation = useMutation({
    mutationFn: apiTestingService.executeTest,
    onSuccess: (result) => {
      setTestResults(prev => [...prev, result]);
      queryClient.invalidateQueries({ queryKey: ['test-history'] });
      queryClient.invalidateQueries({ queryKey: ['test-statistics'] });
    }
  });

  // Execute batch tests mutation
  const executeBatchTestsMutation = useMutation({
    mutationFn: apiTestingService.executeBatchTests,
    onSuccess: (result) => {
      setTestResults(prev => [...prev, ...result.results]);
      setIsRunningTests(false);
      setTestProgress(0);
      queryClient.invalidateQueries({ queryKey: ['test-history'] });
      queryClient.invalidateQueries({ queryKey: ['test-statistics'] });
    },
    onError: () => {
      setIsRunningTests(false);
      setTestProgress(0);
    }
  });

  // Execute test suite mutation
  const executeTestSuiteMutation = useMutation({
    mutationFn: apiTestingService.executeTestSuite,
    onSuccess: (result) => {
      const allResults = result.controllerResults.flatMap(cr => cr.endpointResults);
      setTestResults(prev => [...prev, ...allResults]);
      setIsRunningTests(false);
      setTestProgress(0);
      queryClient.invalidateQueries({ queryKey: ['test-history'] });
      queryClient.invalidateQueries({ queryKey: ['test-statistics'] });
    },
    onError: () => {
      setIsRunningTests(false);
      setTestProgress(0);
    }
  });

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleEndpointSelection = (endpointPath: string) => {
    setSelectedEndpoints(prev => 
      prev.includes(endpointPath) 
        ? prev.filter(ep => ep !== endpointPath)
        : [...prev, endpointPath]
    );
  };

  const handleSelectAllEndpoints = () => {
    if (selectedEndpoints.length === endpoints?.length) {
      setSelectedEndpoints([]);
    } else {
      setSelectedEndpoints(endpoints?.map(ep => `${ep.method} ${ep.path}`) || []);
    }
  };

  const handleRunSingleTest = async (endpoint: TestableEndpoint) => {
    try {
      await executeSingleTestMutation.mutateAsync({
        method: endpoint.method,
        path: endpoint.path,
        testName: `${endpoint.controller}.${endpoint.action}`,
        includePerformanceMetrics: testConfiguration.includePerformanceTests,
        includeResponseValidation: testConfiguration.includeValidationTests
      });
    } catch (error) {
      console.error('Error running single test:', error);
    }
  };

  const handleRunSelectedTests = async () => {
    if (!endpoints || selectedEndpoints.length === 0) return;

    setIsRunningTests(true);
    setTestProgress(0);

    const selectedEndpointObjects = endpoints.filter(ep => 
      selectedEndpoints.includes(`${ep.method} ${ep.path}`)
    );

    const tests = selectedEndpointObjects.map(endpoint => ({
      method: endpoint.method,
      path: endpoint.path,
      testName: `${endpoint.controller}.${endpoint.action}`,
      includePerformanceMetrics: testConfiguration.includePerformanceTests,
      includeResponseValidation: testConfiguration.includeValidationTests
    }));

    try {
      await executeBatchTestsMutation.mutateAsync({
        tests,
        runInParallel: testConfiguration.runInParallel,
        maxParallelTests: testConfiguration.maxParallelTests,
        batchName: `Selected Tests - ${new Date().toLocaleString()}`
      });
    } catch (error) {
      console.error('Error running batch tests:', error);
    }
  };

  const handleRunFullTestSuite = async () => {
    setIsRunningTests(true);

    try {
      await executeTestSuiteMutation.mutateAsync({
        includePerformanceTests: testConfiguration.includePerformanceTests,
        includeSecurityTests: testConfiguration.includeSecurityTests,
        includeValidationTests: testConfiguration.includeValidationTests,
        runInParallel: testConfiguration.runInParallel,
        maxParallelTests: testConfiguration.maxParallelTests,
        suiteName: `Full Test Suite - ${new Date().toLocaleString()}`
      });
    } catch (error) {
      console.error('Error running test suite:', error);
    }
  };

  const handleConfigureTest = (endpoint: TestableEndpoint) => {
    setSelectedEndpoint(endpoint);
    setShowTestDialog(true);
  };

  const handleSaveConfiguration = () => {
    // Save configuration to sessionStorage (non-sensitive test configuration)
    sessionStorage.setItem('apiTestingConfiguration', JSON.stringify(testConfiguration));
  };

  const handleResetConfiguration = () => {
    setTestConfiguration({
      includePerformanceTests: true,
      includeSecurityTests: true,
      includeValidationTests: true,
      runInParallel: false,
      maxParallelTests: 3,
      timeoutSeconds: 30,
      retryAttempts: 1,
      enableDetailedLogging: false,
      saveResults: true
    });
  };

  if (endpointsLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
        <Typography variant="h6" sx={{ ml: 2 }}>Loading API endpoints...</Typography>
      </Box>
    );
  }

  if (endpointsError) {
    return (
      <Alert severity="error">
        Failed to load API endpoints. Please check your connection and try again.
      </Alert>
    );
  }

  return (
    <Box sx={{ width: '100%' }}>
      {/* Page Header with Back Button */}
      <Box display="flex" alignItems="center" mb={3}>
        <Button
          startIcon={<ArrowBack />}
          onClick={() => window.history.back()}
          sx={{ mr: 2 }}
        >
          Back
        </Button>
        <Box>
          <Typography variant="h4" gutterBottom sx={{ mb: 0 }}>
            <BugReport sx={{ mr: 1, verticalAlign: 'middle' }} />
            API Testing Suite
          </Typography>
          <Typography variant="subtitle1" color="text.secondary">
            Comprehensive testing and validation for all API endpoints
          </Typography>
        </Box>
      </Box>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={handleTabChange} aria-label="API testing tabs">
          <Tab label="Endpoints" icon={<BugReport />} />
          <Tab label="Test Results" icon={<Analytics />} />
          <Tab label="Statistics" icon={<Speed />} />
          <Tab label="History" icon={<History />} />
          <Tab label="Configuration" icon={<Settings />} />
        </Tabs>
      </Box>

      <TabPanel value={activeTab} index={0}>
        <EndpointsTab
          endpoints={endpoints}
          selectedEndpoints={selectedEndpoints}
          isRunningTests={isRunningTests}
          onEndpointSelection={handleEndpointSelection}
          onSelectAllEndpoints={handleSelectAllEndpoints}
          onRunSelectedTests={handleRunSelectedTests}
          onRunFullTestSuite={handleRunFullTestSuite}
          onRunSingleTest={handleRunSingleTest}
          onConfigureTest={handleConfigureTest}
        />
      </TabPanel>

      <TabPanel value={activeTab} index={1}>
        <TestResultsTab
          testResults={testResults}
          onClearResults={() => setTestResults([])}
        />
      </TabPanel>

      <TabPanel value={activeTab} index={2}>
        <StatisticsTab
          statistics={statistics}
          isLoading={statisticsLoading}
        />
      </TabPanel>

      <TabPanel value={activeTab} index={3}>
        <HistoryTab
          history={history}
          isLoading={historyLoading}
        />
      </TabPanel>

      <TabPanel value={activeTab} index={4}>
        <ConfigurationTab
          configuration={testConfiguration}
          onConfigurationChange={setTestConfiguration}
          onSaveConfiguration={handleSaveConfiguration}
          onResetConfiguration={handleResetConfiguration}
        />
      </TabPanel>

      {/* Test Configuration Dialog */}
      <Dialog open={showTestDialog} onClose={() => setShowTestDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>
          Configure Test: {selectedEndpoint?.controller}.{selectedEndpoint?.action}
        </DialogTitle>
        <DialogContent>
          {selectedEndpoint && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="body2" gutterBottom>
                <strong>Method:</strong> {selectedEndpoint.method}
              </Typography>
              <Typography variant="body2" gutterBottom>
                <strong>Path:</strong> {selectedEndpoint.path}
              </Typography>
              <Typography variant="body2" gutterBottom>
                <strong>Description:</strong> {selectedEndpoint.description}
              </Typography>
              <Typography variant="body2" gutterBottom>
                <strong>Complexity:</strong> {selectedEndpoint.complexity}
              </Typography>
              <Typography variant="body2" gutterBottom>
                <strong>Requires Authentication:</strong> {selectedEndpoint.requiresAuthentication ? 'Yes' : 'No'}
              </Typography>
              {selectedEndpoint.requiredRoles.length > 0 && (
                <Typography variant="body2" gutterBottom>
                  <strong>Required Roles:</strong> {selectedEndpoint.requiredRoles.join(', ')}
                </Typography>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowTestDialog(false)}>Cancel</Button>
          <Button 
            variant="contained" 
            onClick={() => {
              if (selectedEndpoint) {
                handleRunSingleTest(selectedEndpoint);
              }
              setShowTestDialog(false);
            }}
          >
            Run Test
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
