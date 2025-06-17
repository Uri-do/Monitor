import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Alert,
} from '@mui/material';
import {
  Add as AddIcon,
  Schedule,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { schedulerApi } from '@/services/api';
import { SchedulerDto, CreateSchedulerRequest } from '@/types/api';
import { PageHeader, LoadingSpinner } from '@/components';
import { multiFieldSearch } from '@/utils/stringUtils';
import {
  SchedulerTable,
  SchedulerCreateDialog,
  SchedulerFilters,
  SchedulerFiltersType,
} from './components';
import toast from 'react-hot-toast';

/**
 * SchedulerList component for managing schedulers
 */
const SchedulerList: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filters, setFilters] = useState<SchedulerFiltersType>({
    search: '',
    status: 'all',
    scheduleType: 'all',
    timezone: '',
  });
  const [selectedRows, setSelectedRows] = useState<SchedulerDto[]>([]);

  // Fetch schedulers with fallback data
  const {
    data: schedulers = [],
    isLoading,
    error: loadError,
    refetch,
  } = useQuery({
    queryKey: ['schedulers'],
    queryFn: () => schedulerApi.getSchedulers(),
    retry: false, // Don't retry failed API calls
  });



  // Create scheduler mutation
  const createMutation = useMutation({
    mutationFn: (data: CreateSchedulerRequest) => schedulerApi.createScheduler(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
      setCreateDialogOpen(false);
      setError(null);
      toast.success('Scheduler created successfully');
    },
    onError: (error: any) => {
      setError(error.response?.data?.message || 'Failed to create scheduler');
      toast.error('Failed to create scheduler');
    },
  });

  // Filter schedulers based on all filters
  const filteredSchedulers = useMemo(() => {
    if (!Array.isArray(schedulers)) {
      return [];
    }

    return schedulers.filter(scheduler => {
      // Safety check for scheduler object
      if (!scheduler || typeof scheduler !== 'object') {
        return false;
      }

      // Search filter
      if (filters.search && !multiFieldSearch(filters.search, [
        scheduler.schedulerName,
        scheduler.schedulerDescription,
        scheduler.scheduleType
      ])) {
        return false;
      }

      // Status filter
      if (filters.status !== 'all') {
        const isActive = scheduler.isActive || scheduler.isEnabled;
        if (filters.status === 'active' && !isActive) return false;
        if (filters.status === 'inactive' && isActive) return false;
      }

      // Schedule type filter
      if (filters.scheduleType !== 'all' && scheduler.scheduleType !== filters.scheduleType) {
        return false;
      }

      // Timezone filter
      if (filters.timezone && scheduler.timezone !== filters.timezone) {
        return false;
      }

      return true;
    });
  }, [schedulers, filters]);

  // Delete scheduler mutation
  const deleteMutation = useMutation({
    mutationFn: (schedulerId: number) => schedulerApi.deleteScheduler(schedulerId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedulers'] });
    },
    onError: (error: any) => {
      setError(error.response?.data?.message || 'Failed to delete scheduler');
    },
  });

  const handleCreateScheduler = async (data: CreateSchedulerRequest) => {
    setError(null);
    return createMutation.mutateAsync(data);
  };

  const handleDeleteScheduler = (scheduler: SchedulerDto) => {
    if (window.confirm(`Are you sure you want to delete scheduler "${scheduler.schedulerName}"?`)) {
      deleteMutation.mutate(scheduler.schedulerID);
    }
  };

  const handleToggleStatus = (scheduler: SchedulerDto) => {
    // TODO: Implement toggle functionality
    console.log('Toggle scheduler:', scheduler.schedulerID);
    toast.info('Toggle functionality coming soon');
  };

  const handleClearFilters = () => {
    setFilters({
      search: '',
      status: 'all',
      scheduleType: 'all',
      timezone: '',
    });
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (loadError) {
    return (
      <Box>
        <Alert severity="error">Failed to load schedulers. Please try again.</Alert>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title="Schedulers"
        subtitle={`Manage scheduling configurations for indicators (${filteredSchedulers.length} of ${schedulers.length} schedulers)`}
        icon={<Schedule />}
        primaryAction={{
          label: 'Create Scheduler',
          icon: <AddIcon />,
          onClick: () => setCreateDialogOpen(true),
        }}
        onRefresh={refetch}
        refreshing={isLoading}
      />

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Filters */}
      <SchedulerFilters
        filters={filters}
        onFiltersChange={setFilters}
        onClearFilters={handleClearFilters}
        totalCount={schedulers.length}
        filteredCount={filteredSchedulers.length}
      />

      {/* Scheduler Table */}
      <SchedulerTable
        schedulers={filteredSchedulers}
        loading={isLoading}
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        onView={(scheduler) => navigate(`/schedulers/${scheduler.schedulerID}`)}
        onEdit={(scheduler) => navigate(`/schedulers/${scheduler.schedulerID}/edit`)}
        onDelete={handleDeleteScheduler}
        onToggleStatus={handleToggleStatus}
      />

      {/* Create Scheduler Dialog */}
      <SchedulerCreateDialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        onSubmit={handleCreateScheduler}
        loading={createMutation.isPending}
      />
    </Box>
  );
};

export default SchedulerList;
