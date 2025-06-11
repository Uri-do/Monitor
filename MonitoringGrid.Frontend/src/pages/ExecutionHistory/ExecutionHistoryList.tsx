import React, { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Chip,
  Typography,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Grid,
  Card,
  CardContent,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  Visibility as ViewIcon,
  ExpandMore as ExpandMoreIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Schedule as ScheduleIcon,
  Person as PersonIcon,
  Storage as DatabaseIcon,
  Timer as TimerIcon,
  TrendingUp as KpiIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import toast from 'react-hot-toast';

import { executionHistoryApi } from '@/services/api';
import { ExecutionHistoryDto, ExecutionHistoryDetailDto } from '@/types/api';
import {
  UltimatePageHeader,
  UltimateDataTable,
  UltimateDataTableColumn,
  UltimateFilterPanel,
  UltimateLoadingSpinner,
  UltimateStatusChip,
  UltimateDialog,
  UltimateButton,
  UltimateCard,
} from '@/components/UltimateEnterprise';

interface ExecutionHistoryFilters {
  kpiId?: number;
  executedBy?: string;
  executionMethod?: string;
  isSuccessful?: boolean;
  startDate?: string;
  endDate?: string;
  search: string;
}

const ExecutionHistoryList: React.FC = () => {
  const navigate = useNavigate();
  const [filters, setFilters] = useState<ExecutionHistoryFilters>({
    search: '',
  });
  const [pageSize, setPageSize] = useState(50);
  const [pageNumber, setPageNumber] = useState(1);
  const [selectedRows, setSelectedRows] = useState<ExecutionHistoryDto[]>([]);
  const [detailDialog, setDetailDialog] = useState<{
    open: boolean;
    execution?: ExecutionHistoryDetailDto;
  }>({ open: false });

  // Fetch execution history
  const {
    data: historyData,
    isLoading,
    refetch,
  } = useQuery({
    queryKey: ['executionHistory', filters, pageSize, pageNumber],
    queryFn: () =>
      executionHistoryApi.getExecutionHistory({
        ...filters,
        pageSize,
        pageNumber,
      }),
    staleTime: 5000, // 5 seconds - reduced for testing
    refetchInterval: 10000, // Auto-refresh every 10 seconds
  });

  const executions = historyData?.executions || [];

  // Handle view detail
  const handleViewDetail = async (execution: ExecutionHistoryDto) => {
    try {
      const detail = await executionHistoryApi.getExecutionDetail(execution.historicalId);
      setDetailDialog({ open: true, execution: detail });
    } catch (error) {
      toast.error('Failed to load execution details');
    }
  };

  // Handle view KPI
  const handleViewKpi = (execution: ExecutionHistoryDto) => {
    if (!execution.kpiId || execution.kpiId <= 0) {
      toast.error('Invalid KPI ID');
      return;
    }
    navigate(`/kpis/${execution.kpiId}`);
  };

  // Get status color
  const getStatusColor = (execution: ExecutionHistoryDto) => {
    if (!execution.isSuccessful) return 'error';
    if (execution.executionTimeMs && execution.executionTimeMs > 5000) return 'warning';
    return 'success';
  };

  // Get performance category color
  const getPerformanceColor = (category: string | undefined) => {
    if (!category) return 'secondary';
    switch (category.toLowerCase()) {
      case 'fast':
        return 'success';
      case 'normal':
        return 'info';
      case 'slow':
        return 'warning';
      case 'very slow':
        return 'error';
      default:
        return 'secondary';
    }
  };

  // Define table columns
  const columns: UltimateDataTableColumn<ExecutionHistoryDto>[] = [
    {
      id: 'timestamp',
      label: 'Execution Time',
      sortable: true,
      minWidth: 160,
      render: value => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
            {format(new Date(value), 'MMM dd, yyyy')}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {format(new Date(value), 'HH:mm:ss')}
          </Typography>
        </Box>
      ),
    },
    {
      id: 'indicator',
      label: 'KPI',
      sortable: true,
      minWidth: 200,
      render: (value, row) => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 'medium', mb: 0.5 }}>
            {value}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {row.spName}
          </Typography>
        </Box>
      ),
    },
    {
      id: 'isSuccessful',
      label: 'Status',
      sortable: true,
      minWidth: 100,
      render: (value, row) => <UltimateStatusChip status={value ? 'success' : 'error'} />,
    },
    {
      id: 'currentValue',
      label: 'Value',
      sortable: true,
      minWidth: 100,
      render: (value, row) => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
            {value.toFixed(2)}
          </Typography>
          {row.deviationPercent !== null && row.deviationPercent !== undefined && (
            <Typography
              variant="caption"
              color={Math.abs(row.deviationPercent) > 10 ? 'error.main' : 'text.secondary'}
            >
              {row.deviationPercent > 0 ? '+' : ''}
              {row.deviationPercent.toFixed(1)}%
            </Typography>
          )}
        </Box>
      ),
    },
    {
      id: 'executionTimeMs',
      label: 'Duration',
      sortable: true,
      minWidth: 120,
      render: (value, row) => (
        <Box>
          <Typography variant="body2">{value ? `${value}ms` : 'N/A'}</Typography>
          <Chip
            label={row.performanceCategory}
            size="small"
            color={getPerformanceColor(row.performanceCategory)}
            sx={{ fontSize: '0.7rem', height: 20 }}
          />
        </Box>
      ),
    },
    {
      id: 'executedBy',
      label: 'Executed By',
      sortable: true,
      minWidth: 120,
      render: (value, row) => (
        <Box>
          <Typography variant="body2">{value || 'System'}</Typography>
          <Typography variant="caption" color="text.secondary">
            {row.executionMethod || 'Unknown'}
          </Typography>
        </Box>
      ),
    },
    {
      id: 'shouldAlert',
      label: 'Alert',
      sortable: true,
      minWidth: 80,
      align: 'center',
      render: (value, row) =>
        value ? (
          <Chip
            label={row.alertSent ? 'Sent' : 'Pending'}
            size="small"
            color={row.alertSent ? 'success' : 'warning'}
          />
        ) : (
          <Typography variant="caption" color="text.secondary">
            No
          </Typography>
        ),
    },
  ];

  if (isLoading) {
    return <UltimateLoadingSpinner message="Loading execution history..." />;
  }

  return (
    <Box>
      <UltimatePageHeader
        title="Execution History"
        subtitle={`View detailed execution logs and performance metrics (${historyData?.totalCount || 0} total executions)`}
        onRefresh={refetch}
        refreshing={isLoading}
        secondaryActions={[
          {
            label: 'Test DB',
            onClick: async () => {
              try {
                const result = await executionHistoryApi.testDatabaseConnection();
                toast.success('Database connection test completed successfully');
              } catch (error) {
                toast.error('Database connection test failed');
              }
            },
            gradient: 'warning',
          },
        ]}
      />

      <UltimateFilterPanel
        fields={[
          {
            name: 'isSuccessful',
            label: 'Status',
            type: 'select',
            options: [
              { value: '', label: 'All' },
              { value: 'true', label: 'Successful' },
              { value: 'false', label: 'Failed' },
            ],
          },
          {
            name: 'executionMethod',
            label: 'Method',
            type: 'select',
            options: [
              { value: '', label: 'All' },
              { value: 'Manual', label: 'Manual' },
              { value: 'Scheduled', label: 'Scheduled' },
              { value: 'API', label: 'API' },
            ],
          },
          {
            name: 'executedBy',
            label: 'Executed By',
            type: 'text',
            placeholder: 'Filter by user...',
          },
          {
            name: 'startDate',
            label: 'Start Date',
            type: 'date',
          },
          {
            name: 'endDate',
            label: 'End Date',
            type: 'date',
          },
        ]}
        onFilterChange={newFilters => {
          setFilters({ ...filters, ...newFilters });
          setPageNumber(1); // Reset to first page
        }}
        onClear={() => {
          setFilters({ search: '' });
          setPageNumber(1);
        }}
        onSearch={searchTerm => {
          setFilters({ ...filters, search: searchTerm });
          setPageNumber(1);
        }}
        searchPlaceholder="Search by KPI name, owner, or stored procedure..."
        defaultExpanded={false}
      />

      <UltimateDataTable
        columns={columns}
        data={executions}
        loading={isLoading}
        selectable={true}
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        actions={[
          {
            label: 'View KPI',
            icon: <KpiIcon />,
            onClick: handleViewKpi,
            color: 'primary',
          },
          {
            label: 'View Details',
            icon: <ViewIcon />,
            onClick: handleViewDetail,
            color: 'secondary',
          },
        ]}
        pagination={{
          page: pageNumber,
          rowsPerPage: pageSize,
          totalCount: historyData?.totalCount || 0,
          onPageChange: setPageNumber,
          onRowsPerPageChange: (newSize: number) => {
            setPageSize(newSize);
            setPageNumber(1);
          },
        }}
        emptyMessage="No execution history found."
        rowKey="historicalId"
      />

      {/* Execution Detail Dialog */}
      <UltimateDialog
        open={detailDialog.open}
        onClose={() => setDetailDialog({ open: false })}
        title="Execution Details"
        icon={<ViewIcon />}
        gradient="info"
        maxWidth="lg"
        actions={
          <UltimateButton gradient="secondary" onClick={() => setDetailDialog({ open: false })}>
            Close
          </UltimateButton>
        }
      >
        {detailDialog.execution && <ExecutionDetailView execution={detailDialog.execution} />}
      </UltimateDialog>
    </Box>
  );
};

// Execution Detail View Component
const ExecutionDetailView: React.FC<{ execution: ExecutionHistoryDetailDto }> = ({ execution }) => {
  return (
    <Box sx={{ mt: 2 }}>
      <Grid container spacing={3}>
        {/* Basic Information */}
        <Grid item xs={12} md={6}>
          <UltimateCard>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <PersonIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Basic Information
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Box>
                  <strong>KPI:</strong> {execution.indicator}
                </Box>
                <Box>
                  <strong>Owner:</strong> {execution.kpiOwner}
                </Box>
                <Box>
                  <strong>Stored Procedure:</strong> {execution.spName}
                </Box>
                <Box>
                  <strong>Executed By:</strong> {execution.executedBy || 'System'}
                </Box>
                <Box>
                  <strong>Method:</strong> {execution.executionMethod || 'Unknown'}
                </Box>
                <Box>
                  <strong>Timestamp:</strong> {format(new Date(execution.timestamp), 'PPpp')}
                </Box>
              </Box>
            </CardContent>
          </UltimateCard>
        </Grid>

        {/* Performance Metrics */}
        <Grid item xs={12} md={6}>
          <UltimateCard>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <TimerIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Performance Metrics
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Box>
                  <strong>Current Value:</strong> {execution.currentValue?.toFixed(2) || 'N/A'}
                </Box>
                <Box>
                  <strong>Historical Value:</strong>{' '}
                  {execution.historicalValue?.toFixed(2) || 'N/A'}
                </Box>
                <Box>
                  <strong>Deviation:</strong> {execution.deviationPercent?.toFixed(2) || 'N/A'}%
                </Box>
                <Box>
                  <strong>Execution Time:</strong> {execution.executionTimeMs || 'N/A'}ms
                </Box>
                <Box>
                  <strong>Performance:</strong>
                  <Chip
                    label={execution.performanceCategory}
                    size="small"
                    color={execution.performanceCategory === 'Fast' ? 'success' : 'warning'}
                    sx={{ ml: 1 }}
                  />
                </Box>
                <Box>
                  <strong>Status:</strong>
                  <Chip
                    label={execution.isSuccessful ? 'Success' : 'Failed'}
                    size="small"
                    color={execution.isSuccessful ? 'success' : 'error'}
                    sx={{ ml: 1 }}
                  />
                </Box>
              </Box>
            </CardContent>
          </UltimateCard>
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
                  <Typography variant="subtitle2" gutterBottom>
                    Database Information
                  </Typography>
                  <Box
                    sx={{ display: 'flex', flexDirection: 'column', gap: 1, fontSize: '0.875rem' }}
                  >
                    <Box>
                      <strong>Database:</strong> {execution.databaseName || 'N/A'}
                    </Box>
                    <Box>
                      <strong>Server:</strong> {execution.serverName || 'N/A'}
                    </Box>
                    <Box>
                      <strong>Session ID:</strong> {execution.sessionId || 'N/A'}
                    </Box>
                    <Box>
                      <strong>IP Address:</strong> {execution.ipAddress || 'N/A'}
                    </Box>
                    <Box>
                      <strong>User Agent:</strong> {execution.userAgent || 'N/A'}
                    </Box>
                  </Box>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" gutterBottom>
                    Alert Information
                  </Typography>
                  <Box
                    sx={{ display: 'flex', flexDirection: 'column', gap: 1, fontSize: '0.875rem' }}
                  >
                    <Box>
                      <strong>Should Alert:</strong> {execution.shouldAlert ? 'Yes' : 'No'}
                    </Box>
                    <Box>
                      <strong>Alert Sent:</strong> {execution.alertSent ? 'Yes' : 'No'}
                    </Box>
                    <Box>
                      <strong>Deviation Category:</strong> {execution.deviationCategory}
                    </Box>
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
                    fontFamily: 'monospace',
                  }}
                >
                  {execution.sqlCommand}
                </Box>
                {execution.sqlParameters && (
                  <Box sx={{ mt: 2 }}>
                    <Typography variant="subtitle2" gutterBottom>
                      Parameters:
                    </Typography>
                    <Box
                      component="pre"
                      sx={{
                        backgroundColor: 'grey.50',
                        p: 1,
                        borderRadius: 1,
                        fontSize: '0.8rem',
                        fontFamily: 'monospace',
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
                <Typography variant="h6">Raw Response Data</Typography>
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
                    maxHeight: 400,
                  }}
                >
                  {execution.rawResponse}
                </Box>
              </AccordionDetails>
            </Accordion>
          </Grid>
        )}

        {/* Execution Context */}
        {execution.executionContext && (
          <Grid item xs={12}>
            <Accordion>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography variant="h6">Execution Context</Typography>
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
                    maxHeight: 300,
                  }}
                >
                  {execution.executionContext}
                </Box>
              </AccordionDetails>
            </Accordion>
          </Grid>
        )}

        {/* Error Message */}
        {execution.errorMessage && (
          <Grid item xs={12}>
            <Card sx={{ backgroundColor: 'error.light', color: 'error.contrastText' }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  <ErrorIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Error Details
                </Typography>
                <Typography variant="body2">{execution.errorMessage}</Typography>
              </CardContent>
            </Card>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};

export default ExecutionHistoryList;
