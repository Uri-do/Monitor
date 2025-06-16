import React, { useState, useMemo } from 'react';
import { Box, Tooltip } from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  PlayArrow as ExecuteIcon,
  Visibility as ViewIcon,
  Assessment as IndicatorIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { IndicatorDto, TestIndicatorRequest } from '@/types/api';
import { useIndicators } from '@/hooks/useIndicators';
import { useDeleteIndicator, useExecuteIndicator } from '@/hooks/useIndicatorMutations';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import {
  DataTable,
  DataTableColumn,
  PageHeader,
  FilterPanel,
  StatusChip,
  LoadingSpinner,
} from '@/components';

const IndicatorList: React.FC = () => {
  const navigate = useNavigate();
  const [filters, setFilters] = useState({
    isActive: '',
    search: '',
  });
  const [selectedRows, setSelectedRows] = useState<IndicatorDto[]>([]);

  // Use our enhanced Indicator hook
  const {
    data: indicators = [],
    isLoading,
    refetch,
  } = useIndicators({
    isActive: filters.isActive ? filters.isActive === 'true' : undefined,
    search: filters.search || undefined,
  });

  // Mutations
  const deleteIndicatorMutation = useDeleteIndicator();
  const executeIndicatorMutation = useExecuteIndicator();

  // Handle filter changes
  const handleFilterChange = (newFilters: Record<string, any>) => {
    setFilters(prev => ({ ...prev, ...newFilters }));
  };

  // Filter indicators based on search
  const filteredIndicators = useMemo(() => {
    // Ensure indicators is an array before filtering
    if (!Array.isArray(indicators)) {
      console.warn('indicators is not an array, returning empty array');
      return [];
    }

    return indicators.filter(indicator => {
      const matchesSearch =
        !filters.search ||
        (indicator.indicatorName && indicator.indicatorName.toLowerCase().includes(filters.search.toLowerCase())) ||
        (indicator.indicatorCode && indicator.indicatorCode.toLowerCase().includes(filters.search.toLowerCase())) ||
        (indicator.collectorItemName && indicator.collectorItemName.toLowerCase().includes(filters.search.toLowerCase()));

      return matchesSearch;
    });
  }, [indicators, filters.search]);

  // Helper function to get indicator status
  const getIndicatorStatus = (indicator: IndicatorDto): string => {
    if (!indicator.isActive) return 'inactive';
    if (indicator.isCurrentlyRunning) return 'running';
    return 'active';
  };

  // Handle delete
  const handleDelete = (indicator: IndicatorDto) => {
    if (window.confirm(`Are you sure you want to delete "${indicator.indicatorName}"?`)) {
      deleteIndicatorMutation.mutate(indicator.indicatorID);
    }
  };

  // Handle execute
  const handleExecute = (indicator: IndicatorDto) => {
    if (!indicator.isActive) {
      toast.error('Cannot execute inactive indicator');
      return;
    }

    const request: TestIndicatorRequest = {
      indicatorID: indicator.indicatorID,
    };

    executeIndicatorMutation.mutate(request);
  };

  // Define table columns
  const columns: DataTableColumn<IndicatorDto>[] = [
    {
      id: 'indicatorName',
      label: 'Indicator Name',
      sortable: true,
      width: 200,
      render: (value, row) => (
        <Box>
          <Box sx={{ fontWeight: 'medium', mb: 0.5 }}>{value}</Box>
          <Box sx={{ fontSize: '0.75rem', color: 'text.secondary' }}>{row.indicatorCode}</Box>
        </Box>
      ),
    },
    {
      id: 'collectorItemName',
      label: 'Collector Item',
      sortable: true,
      width: 150,
    },
    {
      id: 'priority',
      label: 'Priority',
      sortable: true,
      width: 100,
      render: value => (
        <StatusChip status={value === 1 ? 'high' : value === 2 ? 'medium' : 'low'} />
      ),
    },
    {
      id: 'isActive',
      label: 'Status',
      sortable: true,
      width: 100,
      render: (_value, row) => <StatusChip status={getIndicatorStatus(row)} />,
    },
    {
      id: 'lastMinutes',
      label: 'Time Range',
      sortable: true,
      width: 100,
      render: value => `${value} min`,
    },
    {
      id: 'lastExecuted',
      label: 'Last Executed',
      sortable: true,
      width: 150,
      render: value => (value ? format(new Date(value), 'MMM dd, HH:mm') : 'Never'),
    },
    {
      id: 'ownerContact',
      label: 'Owner',
      sortable: true,
      width: 120,
      render: (_value, row) => row.ownerContact?.name || 'Unknown',
    },
  ];

  // Filter fields for FilterPanel
  const filterFields = [
    {
      name: 'isActive',
      label: 'Status',
      type: 'select' as const,
      options: [
        { value: '', label: 'All' },
        { value: 'true', label: 'Active' },
        { value: 'false', label: 'Inactive' },
      ],
    },
  ];

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return (
    <Box>
      <PageHeader
        title="Indicators"
        subtitle="Monitor and manage your performance indicators"
        icon={<IndicatorIcon />}
        primaryAction={{
          label: 'Create Indicator',
          icon: <AddIcon />,
          onClick: () => navigate('/indicators/create'),
        }}
        actions={[
          {
            label: 'Refresh',
            onClick: () => refetch(),
          },
        ]}
      />

      <FilterPanel
        fields={filterFields}
        onFilterChange={handleFilterChange}
        onClear={() => setFilters({ isActive: '', search: '' })}
        onSearch={(searchTerm) => setFilters(prev => ({ ...prev, search: searchTerm }))}
        searchPlaceholder="Search indicators..."
      />

      <DataTable
        columns={columns}
        data={filteredIndicators}
        loading={isLoading}
        selectable={true}
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        defaultActions={{
          view: indicator => navigate(`/indicators/${indicator.indicatorID}`),
          edit: indicator => navigate(`/indicators/${indicator.indicatorID}/edit`),
          delete: handleDelete,
        }}
        actions={[
          {
            label: 'Execute Now',
            icon: <ExecuteIcon />,
            onClick: handleExecute,
            disabled: indicator => !indicator.isActive,
          },
        ]}
        emptyMessage="No indicators found. Create your first indicator to get started."
        rowKey="indicatorID"
      />
    </Box>
  );
};

export default IndicatorList;
