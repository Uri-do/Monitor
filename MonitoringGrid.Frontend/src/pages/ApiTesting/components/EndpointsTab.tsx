import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Grid,
  Alert,
  LinearProgress,
  TableContainer,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  Paper,
  Checkbox,
  Chip,
  IconButton,
  Tooltip
} from '@mui/material';
import {
  PlayArrow,
  Settings,
  CheckCircle
} from '@mui/icons-material';
import { TestableEndpoint } from '../../../types/testing';

interface EndpointsTabProps {
  endpoints: TestableEndpoint[] | undefined;
  selectedEndpoints: string[];
  isRunningTests: boolean;
  onEndpointSelection: (endpointPath: string) => void;
  onSelectAllEndpoints: () => void;
  onRunSelectedTests: () => void;
  onRunFullTestSuite: () => void;
  onRunSingleTest: (endpoint: TestableEndpoint) => void;
  onConfigureTest: (endpoint: TestableEndpoint) => void;
}

export const EndpointsTab: React.FC<EndpointsTabProps> = ({
  endpoints,
  selectedEndpoints,
  isRunningTests,
  onEndpointSelection,
  onSelectAllEndpoints,
  onRunSelectedTests,
  onRunFullTestSuite,
  onRunSingleTest,
  onConfigureTest
}) => {
  const getComplexityColor = (complexity: string) => {
    switch (complexity.toLowerCase()) {
      case 'simple': return 'success';
      case 'medium': return 'warning';
      case 'complex': return 'error';
      case 'advanced': return 'secondary';
      default: return 'default';
    }
  };

  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="h6">
                Available Endpoints ({endpoints?.length || 0})
              </Typography>
              <Box>
                <Button
                  variant="outlined"
                  onClick={onSelectAllEndpoints}
                  sx={{ mr: 1 }}
                >
                  {selectedEndpoints.length === endpoints?.length ? 'Deselect All' : 'Select All'}
                </Button>
                <Button
                  variant="contained"
                  onClick={onRunSelectedTests}
                  disabled={selectedEndpoints.length === 0 || isRunningTests}
                  startIcon={<PlayArrow />}
                  sx={{ mr: 1 }}
                >
                  Run Selected ({selectedEndpoints.length})
                </Button>
                <Button
                  variant="contained"
                  color="secondary"
                  onClick={onRunFullTestSuite}
                  disabled={isRunningTests}
                  startIcon={<PlayArrow />}
                >
                  Run Full Suite
                </Button>
              </Box>
            </Box>

            {isRunningTests && (
              <Box sx={{ mb: 2 }}>
                <LinearProgress variant="indeterminate" />
                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                  Running tests... Please wait.
                </Typography>
              </Box>
            )}

            <TableContainer component={Paper}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell padding="checkbox">
                      <Checkbox
                        checked={selectedEndpoints.length === endpoints?.length}
                        indeterminate={selectedEndpoints.length > 0 && selectedEndpoints.length < (endpoints?.length || 0)}
                        onChange={onSelectAllEndpoints}
                      />
                    </TableCell>
                    <TableCell>Method</TableCell>
                    <TableCell>Path</TableCell>
                    <TableCell>Controller</TableCell>
                    <TableCell>Complexity</TableCell>
                    <TableCell>Auth Required</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {endpoints?.map((endpoint, index) => (
                    <TableRow key={index} hover>
                      <TableCell padding="checkbox">
                        <Checkbox
                          checked={selectedEndpoints.includes(`${endpoint.method} ${endpoint.path}`)}
                          onChange={() => onEndpointSelection(`${endpoint.method} ${endpoint.path}`)}
                        />
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={endpoint.method} 
                          size="small" 
                          color={endpoint.method === 'GET' ? 'primary' : endpoint.method === 'POST' ? 'success' : 'default'}
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" fontFamily="monospace">
                          {endpoint.path}
                        </Typography>
                      </TableCell>
                      <TableCell>{endpoint.controller}</TableCell>
                      <TableCell>
                        <Chip 
                          label={endpoint.complexity} 
                          size="small" 
                          color={getComplexityColor(endpoint.complexity)}
                        />
                      </TableCell>
                      <TableCell>
                        {endpoint.requiresAuthentication ? (
                          <CheckCircle color="warning" fontSize="small" />
                        ) : (
                          <Typography variant="body2" color="text.secondary">No</Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        <Tooltip title="Run Single Test">
                          <IconButton
                            size="small"
                            onClick={() => onRunSingleTest(endpoint)}
                            disabled={isRunningTests}
                          >
                            <PlayArrow />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Configure Test">
                          <IconButton
                            size="small"
                            onClick={() => onConfigureTest(endpoint)}
                          >
                            <Settings />
                          </IconButton>
                        </Tooltip>
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
  );
};
