import React from 'react';
import {
  Box,
  Chip,
  Typography,
} from '@mui/material';
import {
  Visibility as ViewIcon,
  TrendingUp as KpiIcon,
} from '@mui/icons-material';
import { safeFormatDate } from '@/utils/dateUtils';
import { ExecutionHistoryDto } from '@/types/api';
import {
  DataTable,
  DataTableColumn,
  StatusChip,
} from '@/components';

interface ExecutionHistoryTableProps {
  executions: ExecutionHistoryDto[];
  loading: boolean;
  selectedRows: ExecutionHistoryDto[];
  onSelectionChange: (rows: ExecutionHistoryDto[]) => void;
  onViewDetail: (execution: ExecutionHistoryDto) => void;
  onViewKpi: (execution: ExecutionHistoryDto) => void;
  pagination: {
    page: number;
    rowsPerPage: number;
    totalCount: number;
    onPageChange: (page: number) => void;
    onRowsPerPageChange: (size: number) => void;
  };
}

export const ExecutionHistoryTable: React.FC<ExecutionHistoryTableProps> = ({
  executions,
  loading,
  selectedRows,
  onSelectionChange,
  onViewDetail,
  onViewKpi,
  pagination,
}) => {
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
  const columns: DataTableColumn<ExecutionHistoryDto>[] = [
    {
      id: 'timestamp',
      label: 'Execution Time',
      sortable: true,
      width: 160,
      render: value => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
            {safeFormatDate(value, 'MMM dd, yyyy', 'Invalid Date')}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {safeFormatDate(value, 'HH:mm:ss', 'Invalid Time')}
          </Typography>
        </Box>
      ),
    },
    {
      id: 'indicator',
      label: 'KPI',
      sortable: true,
      width: 200,
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
      width: 100,
      render: (value, row) => <StatusChip status={value ? 'success' : 'error'} />,
    },
    {
      id: 'currentValue',
      label: 'Value',
      sortable: true,
      width: 100,
      render: (value, row) => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
            {value?.toFixed(2) || 'N/A'}
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
      width: 120,
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
      width: 120,
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
      width: 80,
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

  return (
    <DataTable
      columns={columns}
      data={executions}
      loading={loading}
      selectable={true}
      selectedRows={selectedRows}
      onSelectionChange={onSelectionChange}
      actions={[
        {
          label: 'View KPI',
          icon: <KpiIcon />,
          onClick: onViewKpi,
        },
        {
          label: 'View Details',
          icon: <ViewIcon />,
          onClick: onViewDetail,
        },
      ]}
      pagination={pagination}
      emptyMessage="No execution history found."
      rowKey="historicalId"
    />
  );
};

export default ExecutionHistoryTable;
