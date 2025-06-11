import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TableSortLabel,
  Checkbox,
  IconButton,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
  Chip,
  Typography,
  Skeleton,
} from '@mui/material';
import {
  MoreVert as MoreVertIcon,
  Visibility as ViewIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';

export interface EnhancedDataTableColumn<T = any> {
  id: keyof T;
  label: string;
  minWidth?: number;
  align?: 'left' | 'right' | 'center';
  sortable?: boolean;
  render?: (value: any, row: T) => React.ReactNode;
}

export interface EnhancedDataTableAction<T = any> {
  label: string;
  icon: React.ReactNode;
  onClick: (row: T) => void;
  color?: 'primary' | 'secondary' | 'error' | 'warning' | 'info' | 'success';
  disabled?: (row: T) => boolean;
  hidden?: (row: T) => boolean;
}

interface EnhancedDataTableProps<T = any> {
  columns: EnhancedDataTableColumn<T>[];
  data: T[];
  loading?: boolean;
  selectable?: boolean;
  selectedRows?: T[];
  onSelectionChange?: (selected: T[]) => void;
  actions?: EnhancedDataTableAction<T>[];
  defaultActions?: {
    view?: (row: T) => void;
    edit?: (row: T) => void;
    delete?: (row: T) => void;
  };
  emptyMessage?: string;
  rowKey: keyof T;
  pagination?: boolean;
  pageSize?: number;
  onSort?: (column: keyof T, direction: 'asc' | 'desc') => void;
}

export const EnhancedDataTable = <T extends Record<string, any>>({
  columns,
  data,
  loading = false,
  selectable = false,
  selectedRows = [],
  onSelectionChange,
  actions = [],
  defaultActions,
  emptyMessage = 'No data available',
  rowKey,
  pagination = true,
  pageSize = 10,
  onSort,
}: EnhancedDataTableProps<T>) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(pageSize);
  const [sortColumn, setSortColumn] = useState<keyof T | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [actionMenuAnchor, setActionMenuAnchor] = useState<{
    element: HTMLElement;
    row: T;
  } | null>(null);

  const handleSort = (column: keyof T) => {
    const isAsc = sortColumn === column && sortDirection === 'asc';
    const direction = isAsc ? 'desc' : 'asc';
    setSortColumn(column);
    setSortDirection(direction);
    onSort?.(column, direction);
  };

  const handleSelectAll = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      onSelectionChange?.(data);
    } else {
      onSelectionChange?.([]);
    }
  };

  const handleSelectRow = (row: T) => {
    const isSelected = selectedRows.some(selected => selected[rowKey] === row[rowKey]);
    if (isSelected) {
      onSelectionChange?.(selectedRows.filter(selected => selected[rowKey] !== row[rowKey]));
    } else {
      onSelectionChange?.([...selectedRows, row]);
    }
  };

  const isRowSelected = (row: T) => {
    return selectedRows.some(selected => selected[rowKey] === row[rowKey]);
  };

  const paginatedData = pagination 
    ? data.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
    : data;

  const allActions = [
    ...(defaultActions?.view ? [{ 
      label: 'View', 
      icon: <ViewIcon />, 
      onClick: defaultActions.view,
      color: 'primary' as const 
    }] : []),
    ...(defaultActions?.edit ? [{ 
      label: 'Edit', 
      icon: <EditIcon />, 
      onClick: defaultActions.edit,
      color: 'primary' as const 
    }] : []),
    ...actions,
    ...(defaultActions?.delete ? [{ 
      label: 'Delete', 
      icon: <DeleteIcon />, 
      onClick: defaultActions.delete,
      color: 'error' as const 
    }] : []),
  ];

  const LoadingSkeleton = () => (
    <>
      {Array.from({ length: rowsPerPage }).map((_, index) => (
        <TableRow key={index}>
          {selectable && (
            <TableCell padding="checkbox">
              <Skeleton variant="rectangular" width={20} height={20} />
            </TableCell>
          )}
          {columns.map((column) => (
            <TableCell key={String(column.id)}>
              <Skeleton variant="text" />
            </TableCell>
          ))}
          {allActions.length > 0 && (
            <TableCell>
              <Skeleton variant="circular" width={24} height={24} />
            </TableCell>
          )}
        </TableRow>
      ))}
    </>
  );

  return (
    <Card>
      <CardContent sx={{ p: 0 }}>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                {selectable && (
                  <TableCell padding="checkbox">
                    <Checkbox
                      indeterminate={selectedRows.length > 0 && selectedRows.length < data.length}
                      checked={data.length > 0 && selectedRows.length === data.length}
                      onChange={handleSelectAll}
                      disabled={loading}
                    />
                  </TableCell>
                )}
                {columns.map((column) => (
                  <TableCell
                    key={String(column.id)}
                    align={column.align}
                    style={{ minWidth: column.minWidth }}
                  >
                    {column.sortable ? (
                      <TableSortLabel
                        active={sortColumn === column.id}
                        direction={sortColumn === column.id ? sortDirection : 'asc'}
                        onClick={() => handleSort(column.id)}
                      >
                        {column.label}
                      </TableSortLabel>
                    ) : (
                      column.label
                    )}
                  </TableCell>
                ))}
                {allActions.length > 0 && (
                  <TableCell align="center">Actions</TableCell>
                )}
              </TableRow>
            </TableHead>
            <TableBody>
              {loading ? (
                <LoadingSkeleton />
              ) : paginatedData.length === 0 ? (
                <TableRow>
                  <TableCell 
                    colSpan={columns.length + (selectable ? 1 : 0) + (allActions.length > 0 ? 1 : 0)}
                    align="center"
                    sx={{ py: 4 }}
                  >
                    <Typography variant="body2" color="text.secondary">
                      {emptyMessage}
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                paginatedData.map((row) => (
                  <TableRow
                    key={String(row[rowKey])}
                    hover
                    selected={isRowSelected(row)}
                  >
                    {selectable && (
                      <TableCell padding="checkbox">
                        <Checkbox
                          checked={isRowSelected(row)}
                          onChange={() => handleSelectRow(row)}
                        />
                      </TableCell>
                    )}
                    {columns.map((column) => (
                      <TableCell key={String(column.id)} align={column.align}>
                        {column.render 
                          ? column.render(row[column.id], row)
                          : row[column.id]
                        }
                      </TableCell>
                    ))}
                    {allActions.length > 0 && (
                      <TableCell align="center">
                        <IconButton
                          size="small"
                          onClick={(event) => setActionMenuAnchor({ element: event.currentTarget, row })}
                        >
                          <MoreVertIcon />
                        </IconButton>
                      </TableCell>
                    )}
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>

        {pagination && (
          <TablePagination
            rowsPerPageOptions={[5, 10, 25, 50]}
            component="div"
            count={data.length}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={(_, newPage) => setPage(newPage)}
            onRowsPerPageChange={(event) => {
              setRowsPerPage(parseInt(event.target.value, 10));
              setPage(0);
            }}
          />
        )}

        <Menu
          anchorEl={actionMenuAnchor?.element}
          open={Boolean(actionMenuAnchor)}
          onClose={() => setActionMenuAnchor(null)}
        >
          {allActions
            .filter(action => !action.hidden?.(actionMenuAnchor?.row!))
            .map((action, index) => (
              <MenuItem
                key={index}
                onClick={() => {
                  action.onClick(actionMenuAnchor!.row);
                  setActionMenuAnchor(null);
                }}
                disabled={action.disabled?.(actionMenuAnchor?.row!)}
              >
                <ListItemIcon sx={{ color: `${action.color}.main` }}>
                  {action.icon}
                </ListItemIcon>
                <ListItemText>{action.label}</ListItemText>
              </MenuItem>
            ))}
        </Menu>
      </CardContent>
    </Card>
  );
};

export default EnhancedDataTable;
