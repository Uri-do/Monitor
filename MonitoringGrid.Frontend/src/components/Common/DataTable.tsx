import React, { useState } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TableSortLabel,
  Paper,
  Checkbox,
  IconButton,
  Toolbar,
  Typography,
  Box,

  Tooltip,
  CircularProgress,
  Alert,
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,

} from '@mui/icons-material';

export interface DataTableColumn<T = any> {
  id: keyof T;
  label: string;
  minWidth?: number;
  align?: 'left' | 'right' | 'center';
  sortable?: boolean;
  format?: (value: any, row: T) => React.ReactNode;
  render?: (value: any, row: T) => React.ReactNode;
}

export interface DataTableAction<T = any> {
  label: string;
  icon: React.ReactNode;
  onClick: (row: T) => void;
  color?: 'primary' | 'secondary' | 'error' | 'warning' | 'info' | 'success';
  disabled?: (row: T) => boolean;
  hidden?: (row: T) => boolean;
}

export interface DataTableProps<T = any> {
  columns: DataTableColumn<T>[];
  data: T[];
  loading?: boolean;
  error?: string;
  title?: string;
  subtitle?: string;
  selectable?: boolean;
  selectedRows?: T[];
  onSelectionChange?: (selected: T[]) => void;
  actions?: DataTableAction<T>[];
  defaultActions?: {
    view?: (row: T) => void;
    edit?: (row: T) => void;
    delete?: (row: T) => void;
  };
  pagination?: {
    page: number;
    rowsPerPage: number;
    totalCount: number;
    onPageChange: (page: number) => void;
    onRowsPerPageChange: (rowsPerPage: number) => void;
  };
  sorting?: {
    orderBy: string;
    order: 'asc' | 'desc';
    onSort: (property: string) => void;
  };
  emptyMessage?: string;
  rowKey?: keyof T;
}

const DataTable = <T extends Record<string, any>>({
  columns,
  data,
  loading = false,
  error,
  title,
  subtitle,
  selectable = false,
  selectedRows = [],
  onSelectionChange,
  actions = [],
  defaultActions,
  pagination,
  sorting,
  emptyMessage = 'No data available',
  rowKey = 'id' as keyof T,
}: DataTableProps<T>) => {
  const [selected, setSelected] = useState<T[]>(selectedRows);

  const handleSelectAllClick = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      setSelected(data);
      onSelectionChange?.(data);
    } else {
      setSelected([]);
      onSelectionChange?.([]);
    }
  };

  const handleRowSelect = (row: T) => {
    const selectedIndex = selected.findIndex(item => item[rowKey] === row[rowKey]);
    let newSelected: T[] = [];

    if (selectedIndex === -1) {
      newSelected = [...selected, row];
    } else {
      newSelected = selected.filter(item => item[rowKey] !== row[rowKey]);
    }

    setSelected(newSelected);
    onSelectionChange?.(newSelected);
  };

  const isSelected = (row: T) => selected.some(item => item[rowKey] === row[rowKey]);

  const handleSort = (property: string) => {
    if (sorting) {
      sorting.onSort(property);
    }
  };

  const renderActions = (row: T) => {
    const allActions = [...actions];
    
    if (defaultActions?.view) {
      allActions.unshift({
        label: 'View',
        icon: <ViewIcon />,
        onClick: defaultActions.view,
        color: 'primary'
      });
    }
    
    if (defaultActions?.edit) {
      allActions.push({
        label: 'Edit',
        icon: <EditIcon />,
        onClick: defaultActions.edit,
        color: 'primary'
      });
    }
    
    if (defaultActions?.delete) {
      allActions.push({
        label: 'Delete',
        icon: <DeleteIcon />,
        onClick: defaultActions.delete,
        color: 'error'
      });
    }

    const visibleActions = allActions.filter(action => !action.hidden?.(row));

    return (
      <Box sx={{ display: 'flex', gap: 0.5 }}>
        {visibleActions.map((action, index) => (
          <Tooltip key={index} title={action.label}>
            <span>
              <IconButton
                size="small"
                onClick={() => action.onClick(row)}
                color={action.color || 'default'}
                disabled={action.disabled?.(row)}
              >
                {action.icon}
              </IconButton>
            </span>
          </Tooltip>
        ))}
      </Box>
    );
  };

  if (loading) {
    return (
      <Paper sx={{ p: 3, textAlign: 'center' }}>
        <CircularProgress />
        <Typography variant="body2" sx={{ mt: 2 }}>
          Loading data...
        </Typography>
      </Paper>
    );
  }

  if (error) {
    return (
      <Paper sx={{ p: 3 }}>
        <Alert severity="error">{error}</Alert>
      </Paper>
    );
  }

  return (
    <Paper sx={{ width: '100%', overflow: 'hidden' }}>
      {(title || subtitle) && (
        <Toolbar>
          <Box>
            {title && (
              <Typography variant="h6" component="div">
                {title}
              </Typography>
            )}
            {subtitle && (
              <Typography variant="body2" color="text.secondary">
                {subtitle}
              </Typography>
            )}
          </Box>
        </Toolbar>
      )}
      
      <TableContainer sx={{ maxHeight: 440 }}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              {selectable && (
                <TableCell padding="checkbox">
                  <Checkbox
                    indeterminate={selected.length > 0 && selected.length < data.length}
                    checked={data.length > 0 && selected.length === data.length}
                    onChange={handleSelectAllClick}
                  />
                </TableCell>
              )}
              {columns.map((column) => (
                <TableCell
                  key={String(column.id)}
                  align={column.align}
                  style={{ minWidth: column.minWidth }}
                >
                  {column.sortable && sorting ? (
                    <TableSortLabel
                      active={sorting.orderBy === column.id}
                      direction={sorting.orderBy === column.id ? sorting.order : 'asc'}
                      onClick={() => handleSort(String(column.id))}
                    >
                      {column.label}
                    </TableSortLabel>
                  ) : (
                    column.label
                  )}
                </TableCell>
              ))}
              {(actions.length > 0 || defaultActions) && (
                <TableCell align="center">Actions</TableCell>
              )}
            </TableRow>
          </TableHead>
          <TableBody>
            {data.length === 0 ? (
              <TableRow>
                <TableCell 
                  colSpan={columns.length + (selectable ? 1 : 0) + ((actions.length > 0 || defaultActions) ? 1 : 0)}
                  align="center"
                  sx={{ py: 4 }}
                >
                  <Typography color="text.secondary">
                    {emptyMessage}
                  </Typography>
                </TableCell>
              </TableRow>
            ) : (
              data.map((row, index) => {
                const isItemSelected = isSelected(row);
                return (
                  <TableRow
                    hover
                    key={String(row[rowKey]) || index}
                    selected={isItemSelected}
                  >
                    {selectable && (
                      <TableCell padding="checkbox">
                        <Checkbox
                          checked={isItemSelected}
                          onChange={() => handleRowSelect(row)}
                        />
                      </TableCell>
                    )}
                    {columns.map((column) => {
                      const value = row[column.id];
                      return (
                        <TableCell key={String(column.id)} align={column.align}>
                          {column.render 
                            ? column.render(value, row)
                            : column.format 
                            ? column.format(value, row)
                            : value
                          }
                        </TableCell>
                      );
                    })}
                    {(actions.length > 0 || defaultActions) && (
                      <TableCell align="center">
                        {renderActions(row)}
                      </TableCell>
                    )}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </TableContainer>
      
      {pagination && (
        <TablePagination
          rowsPerPageOptions={[5, 10, 25, 50]}
          component="div"
          count={pagination.totalCount}
          rowsPerPage={pagination.rowsPerPage}
          page={pagination.page}
          onPageChange={(_, newPage) => pagination.onPageChange(newPage)}
          onRowsPerPageChange={(event) => 
            pagination.onRowsPerPageChange(parseInt(event.target.value, 10))
          }
        />
      )}
    </Paper>
  );
};

export default DataTable;
