import React, { useState, useMemo } from 'react';
import { Box, Chip, Typography, Tooltip } from '@mui/material';
import {
  CheckCircle as ResolveIcon,
  Download as ExportIcon,
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
  Notifications as AlertIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { alertApi } from '@/services/api';
import { AlertLogDto } from '@/types/api';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import {
  DataTable,
  DataTableColumn,
  PageHeader,
  FilterPanel,
  StatusChip,
  LoadingSpinner,
} from '@/components/UI';

const AlertList: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState({
    isResolved: '',
    severity: '',
    kpiOwner: '',
    sentVia: '',
    search: '',
    startDate: '',
    endDate: '',
  });
  const [selectedRows, setSelectedRows] = useState<AlertLogDto[]>([]);

  // Fetch Alerts (using mock data for now)
  const {
    data: alertsData,
    isLoading,
    refetch,
  } = useQuery({
    queryKey: ['alerts', filters],
    queryFn: () =>
      alertApi.getAlerts({
        isResolved: filters.isResolved ? filters.isResolved === 'true' : undefined,
        searchText: filters.search || undefined,
        startDate: filters.startDate || undefined,
        endDate: filters.endDate || undefined,
        page: 1,
        pageSize: 100,
        sortBy: 'triggerTime',
        sortDirection: 'desc',
      }),
  });

  // Resolve Alert mutation
  const resolveMutation = useMutation({
    mutationFn: (alertId: number) =>
      alertApi.resolveAlert({
        alertId,
        resolvedBy: 'Current User', // TODO: Get from auth context
        resolutionNotes: 'Resolved from alert list',
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alerts'] });
      toast.success('Alert resolved successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to resolve alert');
    },
  });

  // Bulk resolve mutation
  const bulkResolveMutation = useMutation({
    mutationFn: (alertIds: number[]) =>
      alertApi.bulkResolveAlerts({
        alertIds,
        resolvedBy: 'Current User',
        resolutionNotes: 'Bulk resolved from alert list',
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alerts'] });
      toast.success(`${selectedRows.length} alerts resolved successfully`);
      setSelectedRows([]);
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to resolve alerts');
    },
  });

  const handleResolve = (alert: AlertLogDto) => {
    if (alert.isResolved) {
      toast('Alert is already resolved', { icon: 'ℹ️' });
      return;
    }
    resolveMutation.mutate(alert.alertID);
  };

  const handleBulkResolve = () => {
    const unresolvedAlerts = selectedRows.filter(alert => !alert.isResolved);
    if (unresolvedAlerts.length === 0) {
      toast('No unresolved alerts selected', { icon: 'ℹ️' });
      return;
    }
    bulkResolveMutation.mutate(unresolvedAlerts.map(alert => alert.alertID));
  };

  // Filter data based on search and filters
  const filteredAlerts = useMemo(() => {
    const alerts = alertsData?.alerts || [];
    if (!filters.search) return alerts;

    const searchLower = filters.search.toLowerCase();
    return alerts.filter(
      alert =>
        alert.indicatorName.toLowerCase().includes(searchLower) ||
        alert.indicatorOwner.toLowerCase().includes(searchLower) ||
        alert.message.toLowerCase().includes(searchLower) ||
        alert.severity.toLowerCase().includes(searchLower)
    );
  }, [alertsData?.alerts, filters.search]);

  // Define table columns
  const columns: DataTableColumn<AlertLogDto>[] = [
    {
      id: 'triggerTime',
      label: 'Triggered',
      sortable: true,
      width: 140,
      render: value => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
            {format(new Date(value), 'MMM dd, HH:mm')}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {format(new Date(value), 'yyyy')}
          </Typography>
        </Box>
      ),
    },
    {
      id: 'severity',
      label: 'Severity',
      sortable: true,
      width: 100,
      render: value => <StatusChip status={value} />,
    },
    {
      id: 'indicatorName',
      label: 'Indicator',
      sortable: true,
      width: 200,
      render: (value, row) => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 'medium', mb: 0.5 }}>
            {value}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Owner: {row.indicatorOwner}
          </Typography>
        </Box>
      ),
    },
    {
      id: 'message',
      label: 'Message',
      width: 300,
      render: (value, row) => (
        <Box>
          <Typography variant="body2" sx={{ mb: 0.5 }}>
            {value}
          </Typography>
          {row.deviationPercent && (
            <Chip
              icon={row.deviationPercent > 0 ? <TrendingUpIcon /> : <TrendingDownIcon />}
              label={`${row.deviationPercent > 0 ? '+' : ''}${row.deviationPercent.toFixed(1)}%`}
              size="small"
              color={row.deviationPercent > 0 ? 'error' : 'success'}
              variant="outlined"
            />
          )}
        </Box>
      ),
    },
    {
      id: 'sentViaName',
      label: 'Sent Via',
      width: 100,
      render: (value, row) => (
        <Tooltip title={`Sent to: ${row.sentTo}`}>
          <Chip label={value} size="small" variant="outlined" />
        </Tooltip>
      ),
    },
    {
      id: 'isResolved',
      label: 'Status',
      sortable: true,
      width: 120,
      render: (value, row) => (
        <Box>
          <StatusChip status={value ? 'resolved' : 'unresolved'} />
          {value && row.resolvedTime && (
            <Typography variant="caption" color="text.secondary" display="block">
              {format(new Date(row.resolvedTime), 'MMM dd, HH:mm')}
            </Typography>
          )}
        </Box>
      ),
    },
  ];

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return (
    <Box>
      <PageHeader
        title="Alert Management"
        subtitle={`Monitor and manage system alerts (${filteredAlerts.length} total)`}
        icon={<AlertIcon />}
        secondaryActions={[
          {
            label: 'Bulk Resolve',
            icon: <ResolveIcon />,
            onClick: handleBulkResolve,
            gradient: 'success',
          },
          {
            label: 'Export',
            icon: <ExportIcon />,
            onClick: () => toast('Export feature coming soon', { icon: 'ℹ️' }),
            gradient: 'info',
          },
        ]}
        onRefresh={refetch}
        refreshing={isLoading}
      />

      <FilterPanel
        fields={[
          {
            name: 'isResolved',
            label: 'Status',
            type: 'select',
            options: [
              { value: '', label: 'All' },
              { value: 'false', label: 'Unresolved' },
              { value: 'true', label: 'Resolved' },
            ],
          },
          {
            name: 'severity',
            label: 'Severity',
            type: 'select',
            options: [
              { value: '', label: 'All' },
              { value: 'critical', label: 'Critical' },
              { value: 'high', label: 'High' },
              { value: 'medium', label: 'Medium' },
              { value: 'low', label: 'Low' },
            ],
          },
          {
            name: 'startDate',
            label: 'From Date',
            type: 'date',
          },
          {
            name: 'endDate',
            label: 'To Date',
            type: 'date',
          },
        ]}
        onFilterChange={newFilters => setFilters({ ...filters, ...newFilters })}
        onClear={() =>
          setFilters({
            isResolved: '',
            severity: '',
            kpiOwner: '',
            sentVia: '',
            search: '',
            startDate: '',
            endDate: '',
          })
        }
        onSearch={searchTerm => setFilters({ ...filters, search: searchTerm })}
        searchPlaceholder="Search alerts by Indicator, owner, message, or severity..."
        defaultExpanded={false}
      />

      <DataTable
        columns={columns}
        data={filteredAlerts}
        loading={isLoading}
        selectable={true}
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        defaultActions={{
          view: alert => navigate(`/alerts/${alert.alertID}`),
        }}
        actions={[
          {
            label: 'Resolve',
            icon: <ResolveIcon />,
            onClick: handleResolve,
            disabled: (alert: any) => alert.isResolved,
          },
        ]}
        emptyMessage="No alerts found."
        rowKey="alertID"
      />
    </Box>
  );
};

export default AlertList;
