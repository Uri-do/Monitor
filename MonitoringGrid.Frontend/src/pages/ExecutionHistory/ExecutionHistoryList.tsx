import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Box,
} from '@mui/material';
import {
  History as HistoryIcon,
} from '@mui/icons-material';
import toast from 'react-hot-toast';

import { executionHistoryApi } from '@/services/api';
import { ExecutionHistoryDto, ExecutionHistoryDetailDto } from '@/types/api';
import {
  PageHeader,
  FilterPanel,
  LoadingSpinner,
  Dialog,
  Button,
} from '@/components';
import {
  ExecutionHistoryTable,
  ExecutionDetailView,
} from './components';
import { ExecutionHistoryFilters } from './types';

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



  if (isLoading) {
    return <LoadingSpinner message="Loading execution history..." />;
  }

  return (
    <Box>
      <PageHeader
        title="Execution History"
        subtitle={`View detailed execution logs and performance metrics (${historyData?.totalCount || 0} total executions)`}
        icon={<HistoryIcon />}
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

      <FilterPanel
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

      <DataTable
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
          },
          {
            label: 'View Details',
            icon: <ViewIcon />,
            onClick: handleViewDetail,
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
      <Dialog
        open={detailDialog.open}
        onClose={() => setDetailDialog({ open: false })}
        title="Execution Details"
        maxWidth="lg"
        actions={
          <Button variant="outlined" onClick={() => setDetailDialog({ open: false })}>
            Close
          </Button>
        }
      >
        {detailDialog.execution && <ExecutionDetailView execution={detailDialog.execution} />}
      </Dialog>
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
          <Card>
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
                  <strong>Execution Time:</strong>{' '}
                  {execution.executionTimeMs ? `${execution.executionTimeMs}ms` : 'N/A'}
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
