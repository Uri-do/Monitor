import React, { useState, useMemo } from 'react';
import { Box, Tooltip } from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  PlayArrow as ExecuteIcon,
  Visibility as ViewIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { kpiApi } from '@/services/api';
import { KpiDto, TestKpiRequest } from '@/types/api';
import { useKpis } from '@/hooks/useKpis';
import { useDeleteKpi, useExecuteKpi } from '@/hooks/mutations';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import {
  UltimateDataTable,
  UltimateDataTableColumn,
  UltimatePageHeader,
  UltimateFilterPanel,
  UltimateStatusChip,
  UltimateLoadingSpinner,
} from '@/components/UltimateEnterprise';
import ExecutionProgressDialog from '@/components/KPI/ExecutionProgressDialog';

const KpiList: React.FC = () => {
  const navigate = useNavigate();
  const [filters, setFilters] = useState({
    isActive: '',
    owner: '',
    priority: '',
    search: '',
  });
  const [selectedRows, setSelectedRows] = useState<KpiDto[]>([]);
  const [progressDialog, setProgressDialog] = useState<{
    open: boolean;
    kpi?: KpiDto;
  }>({ open: false });

  // Use our enhanced KPI hook
  const {
    data: kpis = [],
    isLoading,
    refetch,
  } = useKpis({
    isActive: filters.isActive ? filters.isActive === 'true' : undefined,
    owner: filters.owner || undefined,
    priority: filters.priority ? parseInt(filters.priority) : undefined,
  });

  // Use our custom mutation hooks
  const deleteKpiMutation = useDeleteKpi();
  const executeKpiMutation = useExecuteKpi();

  // Create wrapper for delete with confirmation
  const deleteMutation = {
    ...deleteKpiMutation,
    mutate: (kpiId: number) => deleteKpiMutation.mutate(kpiId),
  };

  // Create wrapper for execute with custom success handling
  const executeMutation = {
    ...executeKpiMutation,
    mutate: (request: TestKpiRequest) => {
      executeKpiMutation.mutate(request, {
        onSuccess: result => {
          // Show detailed execution results
          const executionTime = result.executionTimeMs ? `${result.executionTimeMs}ms` : 'N/A';
          const statusMessage = result.isSuccessful
            ? `✅ Success (${executionTime})`
            : `❌ Failed (${executionTime})`;

          if (result.isSuccessful) {
            toast.success(
              `KPI executed: ${statusMessage}\nCurrent: ${result.currentValue}, Historical: ${result.historicalValue}, Deviation: ${result.deviationPercent.toFixed(2)}%`
            );
          } else {
            toast.error(
              `KPI execution failed: ${result.errorMessage || 'Unknown error'}\nExecution time: ${executionTime}`
            );
          }
        },
      });
    },
  };

  const handleDelete = (kpi: KpiDto) => {
    if (window.confirm(`Are you sure you want to delete "${kpi.indicator}"?`)) {
      deleteMutation.mutate(kpi.kpiId);
    }
  };

  const handleExecute = (kpi: KpiDto) => {
    setProgressDialog({ open: true, kpi });
  };

  const handleProgressExecute = async () => {
    if (!progressDialog.kpi) return;

    // Use our execute mutation hook for consistency
    executeKpiMutation.mutate({ kpiId: progressDialog.kpi.kpiId });

    // The mutation hook handles cache invalidation automatically
    return Promise.resolve();
  };

  const getKpiStatus = (kpi: KpiDto) => {
    if (!kpi.isActive) return 'inactive';
    if (!kpi.lastRun) return 'error';

    const nextRun = new Date(kpi.lastRun);
    nextRun.setMinutes(nextRun.getMinutes() + kpi.frequency);
    const now = new Date();

    if (nextRun <= now) return 'due';
    return 'running';
  };

  // Filter data based on search
  const filteredKpis = useMemo(() => {
    if (!filters.search) return kpis;

    const searchLower = filters.search.toLowerCase();
    return kpis.filter(
      kpi =>
        kpi.indicator.toLowerCase().includes(searchLower) ||
        kpi.owner.toLowerCase().includes(searchLower) ||
        kpi.spName.toLowerCase().includes(searchLower)
    );
  }, [kpis, filters.search]);

  // Define table columns
  const columns: UltimateDataTableColumn<KpiDto>[] = [
    {
      id: 'indicator',
      label: 'KPI Indicator',
      sortable: true,
      minWidth: 200,
      render: (value, row) => (
        <Box>
          <Box sx={{ fontWeight: 'medium', mb: 0.5 }}>{value}</Box>
          <Box sx={{ fontSize: '0.75rem', color: 'text.secondary' }}>{row.spName}</Box>
        </Box>
      ),
    },
    {
      id: 'owner',
      label: 'Owner',
      sortable: true,
      minWidth: 120,
    },
    {
      id: 'priority',
      label: 'Priority',
      sortable: true,
      minWidth: 100,
      render: (value, row) => <UltimateStatusChip status={row.priorityName} />,
    },
    {
      id: 'isActive',
      label: 'Status',
      sortable: true,
      minWidth: 100,
      render: (value, row) => <UltimateStatusChip status={getKpiStatus(row)} />,
    },
    {
      id: 'frequency',
      label: 'Frequency',
      sortable: true,
      minWidth: 100,
      render: value => `${value} min`,
    },
    {
      id: 'lastMinutes',
      label: 'Data Window',
      sortable: true,
      minWidth: 120,
      render: value => `${value} min (${Math.round(value / 60)}h)`,
    },
    {
      id: 'deviation',
      label: 'Deviation',
      sortable: true,
      minWidth: 100,
      render: value => `${value}%`,
    },
    {
      id: 'lastRun',
      label: 'Last Run',
      sortable: true,
      minWidth: 140,
      render: value => (value ? format(new Date(value), 'MMM dd, HH:mm') : 'Never'),
    },
    {
      id: 'contacts',
      label: 'Contacts',
      minWidth: 80,
      align: 'center',
      render: value => value.length,
    },
  ];

  if (isLoading) {
    return <UltimateLoadingSpinner message="Loading KPIs..." />;
  }

  return (
    <Box>
      <UltimatePageHeader
        title="KPI Management"
        subtitle={`Manage and monitor your Key Performance Indicators (${filteredKpis.length} total)`}
        primaryAction={{
          label: 'Create KPI',
          icon: <AddIcon />,
          onClick: () => navigate('/kpis/create'),
        }}
        onRefresh={refetch}
        refreshing={isLoading}
      />

      <UltimateFilterPanel
        fields={[
          {
            name: 'isActive',
            label: 'Status',
            type: 'select',
            options: [
              { value: '', label: 'All' },
              { value: 'true', label: 'Active' },
              { value: 'false', label: 'Inactive' },
            ],
          },
          {
            name: 'owner',
            label: 'Owner',
            type: 'text',
            placeholder: 'Filter by owner...',
          },
          {
            name: 'priority',
            label: 'Priority',
            type: 'select',
            options: [
              { value: '', label: 'All' },
              { value: '1', label: 'Critical (SMS + Email)' },
              { value: '2', label: 'High (Email Only)' },
            ],
          },
        ]}
        onFilterChange={newFilters => setFilters({ ...filters, ...newFilters })}
        onClear={() => setFilters({ isActive: '', owner: '', priority: '', search: '' })}
        onSearch={searchTerm => setFilters({ ...filters, search: searchTerm })}
        searchPlaceholder="Search KPIs by name, owner, or stored procedure..."
        defaultExpanded={false}
      />

      <UltimateDataTable
        columns={columns}
        data={filteredKpis}
        loading={isLoading}
        selectable={true}
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        defaultActions={{
          view: kpi => navigate(`/kpis/${kpi.kpiId}`),
          edit: kpi => navigate(`/kpis/${kpi.kpiId}/edit`),
          delete: handleDelete,
        }}
        actions={[
          {
            label: 'Execute Now',
            icon: <ExecuteIcon />,
            onClick: handleExecute,
            color: 'primary',
            disabled: kpi => !kpi.isActive,
          },
        ]}
        emptyMessage="No KPIs found. Create your first KPI to get started."
        rowKey="kpiId"
      />

      {/* Execution Progress Dialog */}
      <ExecutionProgressDialog
        open={progressDialog.open}
        onClose={() => setProgressDialog({ open: false })}
        kpiName={progressDialog.kpi?.indicator || ''}
        onExecute={handleProgressExecute}
      />
    </Box>
  );
};

export default KpiList;
