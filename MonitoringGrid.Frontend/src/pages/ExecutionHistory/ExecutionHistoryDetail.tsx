import React from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Chip,
  Button,
  Alert,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  ExpandMore as ExpandMoreIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Person as PersonIcon,
  Storage as DatabaseIcon,
  Timer as TimerIcon,
  TrendingUp as KpiIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import toast from 'react-hot-toast';

import { executionHistoryApi, kpiApi } from '@/services/api';
import { ExecutionHistoryDetailDto } from '@/types/api';
import {
  PageHeader,
  LoadingSpinner,
  StatusChip,
} from '@/components/Common';

const ExecutionHistoryDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();

  const historicalId = parseInt(id || '0');

  // Check if we came from a KPI details page
  const fromKpiDetails = location.state?.fromKpiDetails;
  const kpiId = location.state?.kpiId;

  // Fetch execution detail
  const { data: execution, isLoading, error } = useQuery({
    queryKey: ['execution-detail', historicalId],
    queryFn: () => executionHistoryApi.getExecutionDetail(historicalId),
    enabled: !!historicalId,
  });

  // Fetch KPI details if we came from KPI page and have execution data
  const { data: kpiData } = useQuery({
    queryKey: ['kpi', execution?.kpiId || kpiId],
    queryFn: () => kpiApi.getKpi(execution?.kpiId || kpiId),
    enabled: !!(execution?.kpiId || kpiId) && fromKpiDetails,
  });

  // Handle invalid ID
  React.useEffect(() => {
    if (!id || isNaN(historicalId) || historicalId <= 0) {
      console.log('Invalid execution ID, redirecting to execution history');
      navigate('/execution-history', { replace: true });
    }
  }, [id, historicalId, navigate]);

  if (isLoading) {
    return <LoadingSpinner message="Loading execution details..." />;
  }

  if (error) {
    return (
      <Box>
        <Alert severity="error">
          Failed to load execution details. Please try again.
        </Alert>
      </Box>
    );
  }

  if (!execution) {
    return (
      <Box>
        <Alert severity="error">
          Execution record not found or you don't have permission to view it.
        </Alert>
      </Box>
    );
  }

  const getPerformanceColor = (category: string) => {
    switch (category.toLowerCase()) {
      case 'fast': return 'success';
      case 'normal': return 'info';
      case 'slow': return 'warning';
      case 'very slow': return 'error';
      default: return 'default';
    }
  };

  // Generate breadcrumbs based on navigation context
  const getBreadcrumbs = () => {
    if (fromKpiDetails && kpiData) {
      return [
        { label: 'KPIs', href: '/kpis' },
        { label: kpiData.indicator, href: `/kpis/${kpiData.kpiId}` },
        { label: 'Execution Details' },
      ];
    }
    return [
      { label: 'Execution History', href: '/execution-history' },
      { label: 'Details' },
    ];
  };

  // Generate back action based on navigation context
  const getBackAction = () => {
    if (fromKpiDetails && execution?.kpiId) {
      return {
        label: 'Back to KPI',
        icon: <BackIcon />,
        onClick: () => navigate(`/kpis/${execution.kpiId}`),
      };
    }
    return {
      label: 'Back to History',
      icon: <BackIcon />,
      onClick: () => navigate('/execution-history'),
    };
  };

  return (
    <Box>
      <PageHeader
        title="Execution Details"
        subtitle={`${execution.indicator} • ${format(new Date(execution.timestamp), 'PPpp')}`}
        breadcrumbs={getBreadcrumbs()}
        primaryAction={{
          label: 'View KPI',
          icon: <KpiIcon />,
          onClick: () => navigate(`/kpis/${execution.kpiId}`),
        }}
        actions={[getBackAction()]}
      />

      <Grid container spacing={3}>
        {/* Basic Information */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <PersonIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Basic Information
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Box><strong>KPI:</strong> {execution.indicator}</Box>
                <Box><strong>Owner:</strong> {execution.kpiOwner}</Box>
                <Box><strong>Stored Procedure:</strong> {execution.spName}</Box>
                <Box><strong>Executed By:</strong> {execution.executedBy || 'System'}</Box>
                <Box><strong>Method:</strong> {execution.executionMethod || 'Unknown'}</Box>
                <Box><strong>Timestamp:</strong> {format(new Date(execution.timestamp), 'PPpp')}</Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Performance Metrics */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <TimerIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Performance Metrics
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Box><strong>Current Value:</strong> {execution.currentValue.toFixed(2)}</Box>
                <Box><strong>Historical Value:</strong> {execution.historicalValue?.toFixed(2) || 'N/A'}</Box>
                <Box><strong>Deviation:</strong> {execution.deviationPercent?.toFixed(2) || 'N/A'}%</Box>
                <Box><strong>Execution Time:</strong> {execution.executionTimeMs || 'N/A'}ms</Box>
                <Box><strong>Performance:</strong> 
                  <Chip 
                    label={execution.performanceCategory} 
                    size="small" 
                    color={getPerformanceColor(execution.performanceCategory)}
                    sx={{ ml: 1 }}
                  />
                </Box>
                <Box><strong>Status:</strong> 
                  <Chip 
                    label={execution.isSuccessful ? 'Success' : 'Failed'} 
                    size="small" 
                    color={execution.isSuccessful ? 'success' : 'error'}
                    icon={execution.isSuccessful ? <SuccessIcon /> : <ErrorIcon />}
                    sx={{ ml: 1 }}
                  />
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Technical Details */}
        <Grid item xs={12}>
          <Accordion>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography variant="h6">
                <DatabaseIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Technical Details
              </Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" gutterBottom>Database Information</Typography>
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1, fontSize: '0.875rem' }}>
                    <Box><strong>Database:</strong> {execution.databaseName || 'N/A'}</Box>
                    <Box><strong>Server:</strong> {execution.serverName || 'N/A'}</Box>
                    <Box><strong>Session ID:</strong> {execution.sessionId || 'N/A'}</Box>
                    <Box><strong>IP Address:</strong> {execution.ipAddress || 'N/A'}</Box>
                    <Box><strong>User Agent:</strong> {execution.userAgent || 'N/A'}</Box>
                  </Box>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" gutterBottom>Alert Information</Typography>
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1, fontSize: '0.875rem' }}>
                    <Box><strong>Should Alert:</strong> {execution.shouldAlert ? 'Yes' : 'No'}</Box>
                    <Box><strong>Alert Sent:</strong> {execution.alertSent ? 'Yes' : 'No'}</Box>
                    <Box><strong>Deviation Category:</strong> {execution.deviationCategory}</Box>
                  </Box>
                </Grid>
              </Grid>
            </AccordionDetails>
          </Accordion>
        </Grid>

        {/* SQL Command */}
        {execution.sqlCommand && (
          <Grid item xs={12}>
            <Accordion>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography variant="h6">SQL Command</Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Box 
                  component="pre" 
                  sx={{ 
                    backgroundColor: 'grey.100', 
                    p: 2, 
                    borderRadius: 1, 
                    overflow: 'auto',
                    fontSize: '0.875rem',
                    fontFamily: 'monospace'
                  }}
                >
                  {execution.sqlCommand}
                </Box>
                {execution.sqlParameters && (
                  <Box sx={{ mt: 2 }}>
                    <Typography variant="subtitle2" gutterBottom>Parameters:</Typography>
                    <Box 
                      component="pre" 
                      sx={{ 
                        backgroundColor: 'grey.50', 
                        p: 1, 
                        borderRadius: 1, 
                        fontSize: '0.8rem',
                        fontFamily: 'monospace'
                      }}
                    >
                      {execution.sqlParameters}
                    </Box>
                  </Box>
                )}
              </AccordionDetails>
            </Accordion>
          </Grid>
        )}

        {/* Raw Response */}
        {execution.rawResponse && (
          <Grid item xs={12}>
            <Accordion>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography variant="h6">Raw Response</Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Box 
                  component="pre" 
                  sx={{ 
                    backgroundColor: 'grey.100', 
                    p: 2, 
                    borderRadius: 1, 
                    overflow: 'auto',
                    fontSize: '0.875rem',
                    fontFamily: 'monospace',
                    maxHeight: 400
                  }}
                >
                  {execution.rawResponse}
                </Box>
              </AccordionDetails>
            </Accordion>
          </Grid>
        )}

        {/* Error Message */}
        {execution.errorMessage && (
          <Grid item xs={12}>
            <Alert severity="error">
              <Typography variant="subtitle2" gutterBottom>Execution Error:</Typography>
              <Typography variant="body2">{execution.errorMessage}</Typography>
            </Alert>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};

export default ExecutionHistoryDetail;
