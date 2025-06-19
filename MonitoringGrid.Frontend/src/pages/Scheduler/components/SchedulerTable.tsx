import React from 'react';
import {
  Box,
  Chip,
  Typography,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  PlayArrow,
  Stop,
  Visibility as ViewIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { SchedulerDto } from '@/types/api';
import {
  DataTable,
  DataTableColumn,
  StatusChip,
} from '@/components';

interface SchedulerTableProps {
  schedulers: SchedulerDto[];
  loading: boolean;
  selectedRows: SchedulerDto[];
  onSelectionChange: (rows: SchedulerDto[]) => void;
  onView: (scheduler: SchedulerDto) => void;
  onEdit: (scheduler: SchedulerDto) => void;
  onDelete: (scheduler: SchedulerDto) => void;
  onToggleStatus: (scheduler: SchedulerDto) => void;
}

export const SchedulerTable: React.FC<SchedulerTableProps> = ({
  schedulers,
  loading,
  selectedRows,
  onSelectionChange,
  onView,
  onEdit,
  onDelete,
  onToggleStatus,
}) => {
  // Get status color and text
  const getStatusInfo = (scheduler: SchedulerDto) => {
    const isActive = scheduler.isEnabled || scheduler.isCurrentlyActive;
    return {
      color: isActive ? 'success' : 'default',
      text: isActive ? 'Active' : 'Inactive',
    };
  };

  // Format cron expression for display
  const formatCronExpression = (cron: string) => {
    if (!cron) return 'N/A';
    
    // Common patterns
    const patterns: Record<string, string> = {
      '0 * * * *': 'Every hour',
      '0 0 * * *': 'Daily at midnight',
      '0 0 * * 0': 'Weekly on Sunday',
      '0 0 1 * *': 'Monthly on 1st',
      '*/5 * * * *': 'Every 5 minutes',
      '*/10 * * * *': 'Every 10 minutes',
      '*/15 * * * *': 'Every 15 minutes',
      '*/30 * * * *': 'Every 30 minutes',
    };

    return patterns[cron] || cron;
  };

  // Define table columns
  const columns: DataTableColumn<SchedulerDto>[] = [
    {
      id: 'schedulerName',
      label: 'Name',
      sortable: true,
      width: 200,
      render: (value, row) => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
            {value}
          </Typography>
          {row.schedulerDescription && (
            <Typography variant="caption" color="text.secondary">
              {row.schedulerDescription}
            </Typography>
          )}
        </Box>
      ),
    },
    {
      id: 'isEnabled',
      label: 'Status',
      sortable: true,
      width: 100,
      render: (value, row) => {
        const { color, text } = getStatusInfo(row);
        return <StatusChip status={text.toLowerCase()} />;
      },
    },
    {
      id: 'cronExpression',
      label: 'Schedule',
      sortable: true,
      width: 180,
      render: (value, row) => (
        <Box>
          <Typography variant="body2">
            {formatCronExpression(value)}
          </Typography>
          {value && value !== formatCronExpression(value) && (
            <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
              {value}
            </Typography>
          )}
        </Box>
      ),
    },
    {
      id: 'timezone',
      label: 'Timezone',
      sortable: true,
      width: 120,
      render: (value) => (
        <Typography variant="body2">
          {value || 'UTC'}
        </Typography>
      ),
    },
    {
      id: 'lastRunTime',
      label: 'Last Run',
      sortable: true,
      width: 160,
      render: (value) => (
        <Box>
          {value ? (
            <>
              <Typography variant="body2">
                {format(new Date(value), 'MMM dd, yyyy')}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {format(new Date(value), 'HH:mm:ss')}
              </Typography>
            </>
          ) : (
            <Typography variant="caption" color="text.secondary">
              Never
            </Typography>
          )}
        </Box>
      ),
    },
    {
      id: 'nextRunTime',
      label: 'Next Run',
      sortable: true,
      width: 160,
      render: (value, row) => {
        const isActive = row.isEnabled || row.isCurrentlyActive;
        return (
          <Box>
            {value && isActive ? (
              <>
                <Typography variant="body2">
                  {format(new Date(value), 'MMM dd, yyyy')}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {format(new Date(value), 'HH:mm:ss')}
                </Typography>
              </>
            ) : (
              <Typography variant="caption" color="text.secondary">
                {isActive ? 'Calculating...' : 'Inactive'}
              </Typography>
            )}
          </Box>
        );
      },
    },
    {
      id: 'actions',
      label: 'Actions',
      sortable: false,
      width: 120,
      align: 'center',
      render: (_, row) => (
        <Box display="flex" gap={0.5}>
          <Tooltip title="View details">
            <IconButton
              size="small"
              onClick={() => onView(row)}
              sx={{ color: 'primary.main' }}
            >
              <ViewIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Edit scheduler">
            <IconButton
              size="small"
              onClick={() => onEdit(row)}
              sx={{ color: 'info.main' }}
            >
              <EditIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          
          <Tooltip title={(row.isEnabled || row.isCurrentlyActive) ? 'Stop scheduler' : 'Start scheduler'}>
            <IconButton
              size="small"
              onClick={() => onToggleStatus(row)}
              sx={{ color: (row.isEnabled || row.isCurrentlyActive) ? 'warning.main' : 'success.main' }}
            >
              {(row.isEnabled || row.isCurrentlyActive) ? <Stop fontSize="small" /> : <PlayArrow fontSize="small" />}
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Delete scheduler">
            <IconButton
              size="small"
              onClick={() => onDelete(row)}
              sx={{ color: 'error.main' }}
            >
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>
      ),
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={schedulers}
      loading={loading}
      selectable={true}
      selectedRows={selectedRows}
      onSelectionChange={onSelectionChange}
      emptyMessage="No schedulers found. Create your first scheduler to get started."
      rowKey="schedulerID"
      stickyHeader
      maxHeight={600}
    />
  );
};

export default SchedulerTable;
