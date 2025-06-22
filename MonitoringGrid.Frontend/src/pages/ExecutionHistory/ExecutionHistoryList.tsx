import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Box,
} from '@mui/material';
import {
  History as HistoryIcon,
  Visibility as ViewIcon,
  Assessment as KpiIcon,
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
  DataTable,
  DataTableColumn,
} from '@/components';
import {
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

  // Define table columns
  const columns: DataTableColumn<ExecutionHistoryDto>[] = [
    {
      id: 'indicator',
      label: 'Indicator',
      sortable: true,
      render: (value, row) => value || 'N/A',
    },
    {
      id: 'kpiOwner',
      label: 'Owner',
      sortable: true,
      render: (value, row) => value || 'N/A',
    },
    {
      id: 'timestamp',
      label: 'Executed At',
      sortable: true,
      render: (value, row) =>
        new Date(value).toLocaleString(),
    },
    {
      id: 'executedBy',
      label: 'Executed By',
      sortable: true,
      render: (value, row) => value || 'System',
    },
    {
      id: 'executionMethod',
      label: 'Method',
      sortable: true,
      render: (value, row) => value || 'Unknown',
    },
    {
      id: 'isSuccessful',
      label: 'Status',
      sortable: true,
      render: (value, row) => (
        <Box
          sx={{
            px: 1,
            py: 0.5,
            borderRadius: 1,
            fontSize: '0.75rem',
            fontWeight: 'bold',
            textAlign: 'center',
            color: 'white',
            backgroundColor: value ? '#4caf50' : '#f44336',
          }}
        >
          {value ? 'Success' : 'Failed'}
        </Box>
      ),
    },
    {
      id: 'currentValue',
      label: 'Current Value',
      sortable: true,
      render: (value, row) =>
        value !== null && value !== undefined ? value.toFixed(2) : 'N/A',
    },
    {
      id: 'executionTimeMs',
      label: 'Duration',
      sortable: true,
      render: (value, row) =>
        value ? `${value}ms` : 'N/A',
    },
  ];

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

export default ExecutionHistoryList;
