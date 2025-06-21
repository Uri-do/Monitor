import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Grid,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Chip,
  LinearProgress,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Stack,
  Alert,
  Collapse,
  IconButton,
} from '@mui/material';
import {
  PlayArrow as RunIcon,
  Stop as StopIcon,
  CheckCircle as PassIcon,
  Error as FailIcon,
  Schedule as PendingIcon,
  ExpandMore as ExpandIcon,
  ExpandLess as CollapseIcon,
  Speed as PerformanceIcon,
  BugReport as UnitIcon,
  Settings as FrameworkIcon,
} from '@mui/icons-material';
import { TestSuiteStatus, TestResult } from '../TestSuitePage';

interface TestRunnerProps {
  onRunTests: (categories?: string[]) => Promise<void>;
  onStopTests: () => Promise<void>;
  testStatus: TestSuiteStatus;
  testResults: TestResult[];
}

const TestRunner: React.FC<TestRunnerProps> = ({
  onRunTests,
  onStopTests,
  testStatus,
  testResults,
}) => {
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set(['all']));

  const testCategories = [
    { value: 'all', label: 'All Tests', icon: <RunIcon />, count: testResults.length },
    { value: 'Framework', label: 'Framework Tests', icon: <FrameworkIcon />, count: testResults.filter(t => t.category === 'Framework').length },
    { value: 'Unit', label: 'Unit Tests', icon: <UnitIcon />, count: testResults.filter(t => t.category === 'Unit').length },
    { value: 'Performance', label: 'Performance Tests', icon: <PerformanceIcon />, count: testResults.filter(t => t.category === 'Performance').length },
  ];

  const getTestIcon = (status: string) => {
    switch (status) {
      case 'passed':
        return <PassIcon color="success" />;
      case 'failed':
        return <FailIcon color="error" />;
      case 'running':
        return <PendingIcon color="info" />;
      default:
        return <PendingIcon color="disabled" />;
    }
  };

  const getTestsByCategory = (category: string) => {
    if (category === 'all') return testResults;
    return testResults.filter(test => test.category === category);
  };

  const toggleCategoryExpansion = (category: string) => {
    const newExpanded = new Set(expandedCategories);
    if (newExpanded.has(category)) {
      newExpanded.delete(category);
    } else {
      newExpanded.add(category);
    }
    setExpandedCategories(newExpanded);
  };

  const handleRunCategory = async () => {
    const categories = selectedCategory === 'all' ? ['all'] : [selectedCategory];
    await onRunTests(categories);
  };

  return (
    <Box>
      <Grid container spacing={3}>
        {/* Test Controls */}
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Test Execution
              </Typography>
              
              <Stack spacing={2}>
                <FormControl fullWidth>
                  <InputLabel>Test Category</InputLabel>
                  <Select
                    value={selectedCategory}
                    label="Test Category"
                    onChange={(e) => setSelectedCategory(e.target.value)}
                    disabled={testStatus.isRunning}
                  >
                    {testCategories.map((category) => (
                      <MenuItem key={category.value} value={category.value}>
                        <Box display="flex" alignItems="center" gap={1}>
                          {category.icon}
                          {category.label}
                          <Chip size="small" label={category.count} />
                        </Box>
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>

                <Button
                  variant="contained"
                  color={testStatus.isRunning ? "error" : "primary"}
                  startIcon={testStatus.isRunning ? <StopIcon /> : <RunIcon />}
                  onClick={testStatus.isRunning ? onStopTests : handleRunCategory}
                  fullWidth
                  size="large"
                >
                  {testStatus.isRunning ? 'Stop Tests' : `Run ${selectedCategory === 'all' ? 'All' : selectedCategory} Tests`}
                </Button>

                {testStatus.isRunning && (
                  <Box>
                    <Typography variant="body2" color="text.secondary" gutterBottom>
                      Progress: {Math.round(testStatus.progress)}%
                    </Typography>
                    <LinearProgress 
                      variant="determinate" 
                      value={testStatus.progress}
                      sx={{ height: 8, borderRadius: 4 }}
                    />
                    {testStatus.currentTest && (
                      <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                        Running: {testStatus.currentTest}
                      </Typography>
                    )}
                  </Box>
                )}
              </Stack>
            </CardContent>
          </Card>

          {/* Quick Stats */}
          <Card sx={{ mt: 2 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Quick Stats
              </Typography>
              
              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <Box textAlign="center">
                    <Typography variant="h4" color="success.main">
                      {testStatus.passedTests}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Passed
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={6}>
                  <Box textAlign="center">
                    <Typography variant="h4" color="error.main">
                      {testStatus.failedTests}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Failed
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12}>
                  <Box textAlign="center">
                    <Typography variant="body2" color="text.secondary">
                      Duration: {(testStatus.duration / 1000).toFixed(2)}s
                    </Typography>
                  </Box>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Test List */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Test Results
              </Typography>

              {testResults.length === 0 ? (
                <Alert severity="info">
                  No tests have been run yet. Select a category and click "Run Tests" to begin.
                </Alert>
              ) : (
                <Box>
                  {testCategories.map((category) => {
                    const categoryTests = getTestsByCategory(category.value);
                    if (category.value !== 'all' && categoryTests.length === 0) return null;
                    
                    return (
                      <Box key={category.value} sx={{ mb: 2 }}>
                        <Box
                          display="flex"
                          alignItems="center"
                          sx={{ cursor: 'pointer', p: 1, borderRadius: 1, '&:hover': { bgcolor: 'action.hover' } }}
                          onClick={() => toggleCategoryExpansion(category.value)}
                        >
                          {category.icon}
                          <Typography variant="subtitle1" sx={{ ml: 1, flexGrow: 1 }}>
                            {category.label}
                          </Typography>
                          <Chip 
                            size="small" 
                            label={`${categoryTests.filter(t => t.status === 'passed').length}/${categoryTests.length}`}
                            color={categoryTests.every(t => t.status === 'passed') ? 'success' : 'default'}
                          />
                          <IconButton size="small">
                            {expandedCategories.has(category.value) ? <CollapseIcon /> : <ExpandIcon />}
                          </IconButton>
                        </Box>

                        <Collapse in={expandedCategories.has(category.value)}>
                          <List dense>
                            {categoryTests.map((test) => (
                              <ListItem key={test.id} sx={{ pl: 4 }}>
                                <ListItemIcon>
                                  {getTestIcon(test.status)}
                                </ListItemIcon>
                                <ListItemText
                                  primary={test.name}
                                  secondary={
                                    <Box>
                                      <Typography variant="caption" color="text.secondary">
                                        {test.duration > 0 && `${test.duration}ms`}
                                        {test.message && ` • ${test.message}`}
                                      </Typography>
                                    </Box>
                                  }
                                />
                                <Chip
                                  size="small"
                                  label={test.status}
                                  color={
                                    test.status === 'passed' ? 'success' :
                                    test.status === 'failed' ? 'error' :
                                    test.status === 'running' ? 'info' : 'default'
                                  }
                                />
                              </ListItem>
                            ))}
                          </List>
                        </Collapse>
                      </Box>
                    );
                  })}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default TestRunner;
