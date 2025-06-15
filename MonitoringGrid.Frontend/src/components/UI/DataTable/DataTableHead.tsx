import React from 'react';
import {
  TableHead,
  TableRow,
  TableCell,
  Checkbox,
  TableSortLabel,
} from '@mui/material';
import { DataTableColumn } from '../DataTable';

interface DataTableHeadProps {
  columns: DataTableColumn[];
  selectable: boolean;
  selected: any[];
  data: any[];
  gradient: string;
  orderBy: string;
  order: 'asc' | 'desc';
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
  onRowEdit?: (row: any) => void;
  onRowDelete?: (row: any) => void;
  onRowView?: (row: any) => void;
  onSelectAll: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onSort: (columnId: string) => void;
}

export const DataTableHead: React.FC<DataTableHeadProps> = ({
  columns,
  selectable,
  selected,
  data,
  gradient,
  orderBy,
  order,
  actions,
  defaultActions,
  onRowEdit,
  onRowDelete,
  onRowView,
  onSelectAll,
  onSort,
}) => {
  const hasActions = onRowEdit || onRowDelete || onRowView || 
    defaultActions?.edit || defaultActions?.delete || defaultActions?.view || 
    actions?.length;

  return (
    <TableHead>
      <TableRow>
        {selectable && (
          <TableCell padding="checkbox">
            <Checkbox
              indeterminate={selected.length > 0 && selected.length < data.length}
              checked={data.length > 0 && selected.length === data.length}
              onChange={onSelectAll}
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

        {columns.map(column => (
          <TableCell
            key={column.id}
            align={column.align || 'left'}
            style={{ width: column.width }}
            sx={{
              fontWeight: 'bold',
              backgroundColor: theme =>
                theme.palette.mode === 'dark'
                  ? 'rgba(255, 255, 255, 0.05)'
                  : 'rgba(102, 126, 234, 0.05)',
              borderBottom: theme =>
                theme.palette.mode === 'dark'
                  ? '2px solid rgba(255, 255, 255, 0.1)'
                  : '2px solid rgba(102, 126, 234, 0.2)',
            }}
          >
            {column.sortable ? (
              <TableSortLabel
                active={orderBy === column.id}
                direction={orderBy === column.id ? order : 'asc'}
                onClick={() => onSort(column.id)}
                sx={{
                  '&.Mui-active': {
                    color: '#667eea',
                  },
                  '&:hover': {
                    color: '#667eea',
                  },
                }}
              >
                {column.label}
              </TableSortLabel>
            ) : (
              column.label
            )}
          </TableCell>
        ))}

        {/* Actions Column */}
        {hasActions && (
          <TableCell
            align="center"
            sx={{
              fontWeight: 'bold',
              width: '120px',
              minWidth: '120px',
              maxWidth: '120px',
              backgroundColor: theme =>
                theme.palette.mode === 'dark'
                  ? 'rgba(255, 255, 255, 0.05)'
                  : 'rgba(102, 126, 234, 0.05)',
              borderBottom: theme =>
                theme.palette.mode === 'dark'
                  ? '2px solid rgba(255, 255, 255, 0.1)'
                  : '2px solid rgba(102, 126, 234, 0.2)',
            }}
          >
            Actions
          </TableCell>
        )}
      </TableRow>
    </TableHead>
  );
};

export default DataTableHead;
