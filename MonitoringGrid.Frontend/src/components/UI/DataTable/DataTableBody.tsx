import React from 'react';
import {
  TableBody,
  TableRow,
  TableCell,
  Checkbox,
  Box,
  Typography,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Visibility as ViewIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { DataTableColumn } from '../DataTable';

interface DataTableBodyProps {
  loading: boolean;
  data: any[];
  columns: DataTableColumn[];
  selectable: boolean;
  selected: any[];
  gradient: string;
  rowKey?: string;
  emptyMessage?: string;
  actions?: Array<{
    icon: React.ReactElement;
    label: string;
    onClick: (row: any) => void;
    color?: string;
  }>;
  defaultActions?: {
    view?: (row: any) => void;
    edit?: (row: any) => void;
    delete?: (row: any) => void;
  };
  onRowClick?: (row: any) => void;
  onRowEdit?: (row: any) => void;
  onRowDelete?: (row: any) => void;
  onRowView?: (row: any) => void;
  onSelectRow: (row: any) => void;
  isSelected: (row: any) => boolean;
}

export const DataTableBody: React.FC<DataTableBodyProps> = ({
  loading,
  data,
  columns,
  selectable,
  selected,
  gradient,
  rowKey,
  emptyMessage,
  actions,
  defaultActions,
  onRowClick,
  onRowEdit,
  onRowDelete,
  onRowView,
  onSelectRow,
  isSelected,
}) => {
  const hasActions = onRowEdit || onRowDelete || onRowView || 
    defaultActions?.edit || defaultActions?.delete || defaultActions?.view || 
    actions?.length;

  const getColSpan = () => {
    return columns.length + (selectable ? 1 : 0) + (hasActions ? 1 : 0);
  };

  if (loading) {
    return (
      <TableBody>
        <TableRow>
          <TableCell colSpan={getColSpan()}>
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <Typography>Loading...</Typography>
            </Box>
          </TableCell>
        </TableRow>
      </TableBody>
    );
  }

  if (data.length === 0) {
    return (
      <TableBody>
        <TableRow>
          <TableCell colSpan={getColSpan()}>
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <Typography color="text.secondary">
                {emptyMessage || 'No data available'}
              </Typography>
            </Box>
          </TableCell>
        </TableRow>
      </TableBody>
    );
  }

  return (
    <TableBody>
      {(Array.isArray(data) ? data : []).map((row, index) => {
        const isItemSelected = isSelected(row);
        return (
          <TableRow
            key={rowKey ? row[rowKey] : row.id || index}
            hover
            selected={isItemSelected}
            onClick={() => onRowClick?.(row)}
            sx={{
              cursor: onRowClick ? 'pointer' : 'default',
              '&:hover': {
                backgroundColor: theme =>
                  theme.palette.mode === 'dark'
                    ? 'rgba(255, 255, 255, 0.05)'
                    : 'rgba(102, 126, 234, 0.05)',
              },
              '&.Mui-selected': {
                backgroundColor: theme =>
                  theme.palette.mode === 'dark'
                    ? 'rgba(255, 255, 255, 0.1)'
                    : 'rgba(102, 126, 234, 0.1)',
              },
            }}
          >
            {selectable && (
              <TableCell padding="checkbox">
                <Checkbox
                  checked={isItemSelected}
                  onChange={() => onSelectRow(row)}
                  onClick={e => e.stopPropagation()}
                  sx={{
                    color:
                      gradient === 'primary'
                        ? '#667eea'
                        : gradient === 'secondary'
                          ? '#f093fb'
                          : gradient === 'success'
                            ? '#43e97b'
                            : gradient === 'warning'
                              ? '#fa709a'
                              : gradient === 'error'
                                ? '#ff6b6b'
                                : '#4facfe',
                  }}
                />
              </TableCell>
            )}

            {(Array.isArray(columns) ? columns : []).map(column => (
              <TableCell
                key={column.id}
                align={column.align || 'left'}
                onClick={e => e.stopPropagation()}
              >
                {column.render ? column.render(row[column.id], row) : row[column.id]}
              </TableCell>
            ))}

            {/* Actions Column */}
            {hasActions && (
              <TableCell align="center" onClick={e => e.stopPropagation()}>
                <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'center' }}>
                  {(onRowView || defaultActions?.view) && (
                    <Tooltip title="View">
                      <IconButton
                        size="small"
                        onClick={() => (onRowView || defaultActions?.view)?.(row)}
                        sx={{ color: '#4facfe' }}
                      >
                        <ViewIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}

                  {(onRowEdit || defaultActions?.edit) && (
                    <Tooltip title="Edit">
                      <IconButton
                        size="small"
                        onClick={() => (onRowEdit || defaultActions?.edit)?.(row)}
                        sx={{ color: '#43e97b' }}
                      >
                        <EditIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}

                  {(onRowDelete || defaultActions?.delete) && (
                    <Tooltip title="Delete">
                      <IconButton
                        size="small"
                        onClick={() => (onRowDelete || defaultActions?.delete)?.(row)}
                        sx={{ color: '#ff6b6b' }}
                      >
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}

                  {(Array.isArray(actions) ? actions : []).map((action, actionIndex) => (
                    <Tooltip key={actionIndex} title={action.label}>
                      <IconButton
                        size="small"
                        onClick={() => action.onClick(row)}
                        sx={{ color: action.color || '#667eea' }}
                      >
                        {action.icon}
                      </IconButton>
                    </Tooltip>
                  ))}
                </Box>
              </TableCell>
            )}
          </TableRow>
        );
      })}
    </TableBody>
  );
};

export default DataTableBody;
