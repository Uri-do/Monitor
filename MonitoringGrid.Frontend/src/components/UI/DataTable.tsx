import React, { useState, useMemo } from 'react';
import {
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TableSortLabel,
  TextField,
  IconButton,
  Checkbox,
  Menu,
  MenuItem,
  Tooltip,
  Typography,
  Paper,
  InputAdornment,
  FormControl,
  InputLabel,
  Select,
  Stack,
  Divider,
} from '@mui/material';
import {
  Search as SearchIcon,
  GetApp as ExportIcon,
  Refresh as RefreshIcon,
  MoreVert as MoreIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  Visibility as ViewIcon,
  FileDownload as DownloadIcon,
  Print as PrintIcon,
} from '@mui/icons-material';
import { CustomButton } from './Button';
import { CustomCard } from './Card';

export interface DataTableColumn<T = any> {
  id: string;
  label: string;
  sortable?: boolean;
  filterable?: boolean;
  width?: number | string;
  align?: 'left' | 'center' | 'right';
  format?: (value: any, row?: T) => React.ReactNode;
  render?: (value: any, row: T) => React.ReactNode;
  filterType?: 'text' | 'select' | 'date' | 'number';
  filterOptions?: { label: string; value: any }[];
}

export interface DataTableProps {
  title?: string;
  subtitle?: string;
  data: any[];
  columns: DataTableColumn[];
  loading?: boolean;
  searchable?: boolean;
  filterable?: boolean;
  exportable?: boolean;
  refreshable?: boolean;
  selectable?: boolean;
  pagination?: boolean | {
    page: number;
    rowsPerPage: number;
    totalCount: number;
    onPageChange: (page: number) => void;
    onRowsPerPageChange: (newSize: number) => void;
  };
  rowsPerPageOptions?: number[];
  defaultRowsPerPage?: number;
  onRefresh?: () => void;
  onExport?: (data: any[], format: 'csv' | 'excel' | 'pdf') => void;
  onRowClick?: (row: any) => void;
  onRowEdit?: (row: any) => void;
  onRowDelete?: (row: any) => void;
  onRowView?: (row: any) => void;
  onBulkAction?: (selectedRows: any[], action: string) => void;
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  height?: number | string;
  maxHeight?: number | string;
  selectedRows?: any[];
  onSelectionChange?: (selectedRows: any[]) => void;
  defaultActions?: {
    view?: (row: any) => void;
    edit?: (row: any) => void;
    delete?: (row: any) => void;
  };
  actions?: Array<{
    label: string;
    icon?: React.ReactNode;
    onClick: (row: any) => void;
    disabled?: (row: any) => boolean;
  }>;
  emptyMessage?: string;
  rowKey?: string;
}

export const UltimateDataTable: React.FC<DataTableProps> = ({
  title = 'Ultimate Data Table',
  subtitle,
  data = [],
  columns = [],
  loading = false,
  searchable = true,
  filterable = true,
  exportable = true,
  refreshable = true,
  selectable = true,
  pagination = true,
  rowsPerPageOptions = [5, 10, 25, 50],
  defaultRowsPerPage = 10,
  onRefresh,
  onExport,
  onRowClick,
  onRowEdit,
  onRowDelete,
  onRowView,
  onBulkAction,
  gradient = 'primary',
  height,
  maxHeight = 600,
}) => {
  // State management
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(defaultRowsPerPage);
  const [orderBy, setOrderBy] = useState<string>('');
  const [order, setOrder] = useState<'asc' | 'desc'>('asc');
  const [searchTerm, setSearchTerm] = useState('');
  const [filters, setFilters] = useState<Record<string, any>>({});
  const [selected, setSelected] = useState<any[]>([]);
  const [exportMenuAnchor, setExportMenuAnchor] = useState<null | HTMLElement>(null);
  const [bulkMenuAnchor, setBulkMenuAnchor] = useState<null | HTMLElement>(null);

  // Filtering and sorting logic
  const filteredData = useMemo(() => {
    let filtered = data;

    // Apply search filter
    if (searchTerm) {
      filtered = filtered.filter(row =>
        columns.some(column =>
          String(row[column.id] || '')
            .toLowerCase()
            .includes(searchTerm.toLowerCase())
        )
      );
    }

    // Apply column filters
    Object.entries(filters).forEach(([columnId, filterValue]) => {
      if (filterValue !== '' && filterValue != null) {
        filtered = filtered.filter(row => {
          const cellValue = row[columnId];
          if (typeof filterValue === 'string') {
            return String(cellValue || '')
              .toLowerCase()
              .includes(filterValue.toLowerCase());
          }
          return cellValue === filterValue;
        });
      }
    });

    return filtered;
  }, [data, searchTerm, filters, columns]);

  const sortedData = useMemo(() => {
    if (!orderBy) return filteredData;

    return [...filteredData].sort((a, b) => {
      const aValue = a[orderBy];
      const bValue = b[orderBy];

      if (aValue < bValue) {
        return order === 'asc' ? -1 : 1;
      }
      if (aValue > bValue) {
        return order === 'asc' ? 1 : -1;
      }
      return 0;
    });
  }, [filteredData, orderBy, order]);

  const paginatedData = useMemo(() => {
    if (!pagination) return sortedData;
    const startIndex = page * rowsPerPage;
    return sortedData.slice(startIndex, startIndex + rowsPerPage);
  }, [sortedData, page, rowsPerPage, pagination]);

  // Event handlers
  const handleSort = (columnId: string) => {
    const isAsc = orderBy === columnId && order === 'asc';
    setOrder(isAsc ? 'desc' : 'asc');
    setOrderBy(columnId);
  };

  const handleSelectAll = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      setSelected(paginatedData);
    } else {
      setSelected([]);
    }
  };

  const handleSelectRow = (row: any) => {
    const selectedIndex = selected.findIndex(item => item.id === row.id);
    let newSelected: any[] = [];

    if (selectedIndex === -1) {
      newSelected = [...selected, row];
    } else {
      newSelected = selected.filter(item => item.id !== row.id);
    }

    setSelected(newSelected);
  };

  const handleExport = (format: 'csv' | 'excel' | 'pdf') => {
    if (onExport) {
      onExport(selected.length > 0 ? selected : sortedData, format);
    }
    setExportMenuAnchor(null);
  };

  const handleBulkAction = (action: string) => {
    if (onBulkAction && selected.length > 0) {
      onBulkAction(selected, action);
    }
    setBulkMenuAnchor(null);
    setSelected([]);
  };

  const isSelected = (row: any) => selected.findIndex(item => item.id === row.id) !== -1;

  return (
    <CustomCard gradient={gradient} sx={{ height: height || 'auto' }}>
      <Box sx={{ p: 3 }}>
        {/* Header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Box>
            <Typography variant="h5" fontWeight="bold" gutterBottom>
              {title}
            </Typography>
            {subtitle && (
              <Typography variant="body2" color="text.secondary">
                {subtitle}
              </Typography>
            )}
          </Box>

          {/* Action Buttons */}
          <Stack direction="row" spacing={1}>
            {refreshable && (
              <CustomButton
                variant="outlined"
                gradient={gradient}
                icon={<RefreshIcon />}
                onClick={onRefresh}
                disabled={loading}
              >
                Refresh
              </CustomButton>
            )}

            {exportable && (
              <>
                <CustomButton
                  variant="outlined"
                  gradient="success"
                  icon={<ExportIcon />}
                  onClick={e => setExportMenuAnchor(e.currentTarget)}
                >
                  Export
                </CustomButton>
                <Menu
                  anchorEl={exportMenuAnchor}
                  open={Boolean(exportMenuAnchor)}
                  onClose={() => setExportMenuAnchor(null)}
                >
                  <MenuItem onClick={() => handleExport('csv')}>
                    <DownloadIcon sx={{ mr: 1 }} /> Export CSV
                  </MenuItem>
                  <MenuItem onClick={() => handleExport('excel')}>
                    <DownloadIcon sx={{ mr: 1 }} /> Export Excel
                  </MenuItem>
                  <MenuItem onClick={() => handleExport('pdf')}>
                    <PrintIcon sx={{ mr: 1 }} /> Export PDF
                  </MenuItem>
                </Menu>
              </>
            )}

            {selected.length > 0 && (
              <>
                <CustomButton
                  variant="outlined"
                  gradient="warning"
                  icon={<MoreIcon />}
                  onClick={e => setBulkMenuAnchor(e.currentTarget)}
                >
                  Bulk Actions ({selected.length})
                </CustomButton>
                <Menu
                  anchorEl={bulkMenuAnchor}
                  open={Boolean(bulkMenuAnchor)}
                  onClose={() => setBulkMenuAnchor(null)}
                >
                  <MenuItem onClick={() => handleBulkAction('delete')}>
                    <DeleteIcon sx={{ mr: 1 }} /> Delete Selected
                  </MenuItem>
                  <MenuItem onClick={() => handleBulkAction('export')}>
                    <ExportIcon sx={{ mr: 1 }} /> Export Selected
                  </MenuItem>
                </Menu>
              </>
            )}
          </Stack>
        </Box>

        {/* Search and Filters */}
        <Box sx={{ mb: 3 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems="center">
            {searchable && (
              <TextField
                placeholder="Search across all columns..."
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon />
                    </InputAdornment>
                  ),
                }}
                sx={{ minWidth: 300 }}
              />
            )}

            {filterable && (
              <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                {columns
                  .filter(col => col.filterable)
                  .map(column => (
                    <FormControl key={column.id} size="small" sx={{ minWidth: 120 }}>
                      <InputLabel>{column.label}</InputLabel>
                      {column.filterType === 'select' ? (
                        <Select
                          value={filters[column.id] || ''}
                          onChange={e =>
                            setFilters(prev => ({ ...prev, [column.id]: e.target.value }))
                          }
                          label={column.label}
                        >
                          <MenuItem value="">All</MenuItem>
                          {column.filterOptions?.map(option => (
                            <MenuItem key={option.value} value={option.value}>
                              {option.label}
                            </MenuItem>
                          ))}
                        </Select>
                      ) : (
                        <TextField
                          size="small"
                          value={filters[column.id] || ''}
                          onChange={e =>
                            setFilters(prev => ({ ...prev, [column.id]: e.target.value }))
                          }
                          label={column.label}
                        />
                      )}
                    </FormControl>
                  ))}
              </Box>
            )}
          </Stack>
        </Box>

        <Divider sx={{ mb: 2 }} />

        {/* Data Table */}
        <TableContainer
          component={Paper}
          sx={{
            maxHeight,
            borderRadius: 2,
          }}
        >
          <Table stickyHeader>
            <TableHead>
              <TableRow>
                {selectable && (
                  <TableCell padding="checkbox">
                    <Checkbox
                      indeterminate={selected.length > 0 && selected.length < paginatedData.length}
                      checked={paginatedData.length > 0 && selected.length === paginatedData.length}
                      onChange={handleSelectAll}
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
                      background:
                        'linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%)',
                      borderBottom: '2px solid rgba(102, 126, 234, 0.2)',
                    }}
                  >
                    {column.sortable ? (
                      <TableSortLabel
                        active={orderBy === column.id}
                        direction={orderBy === column.id ? order : 'asc'}
                        onClick={() => handleSort(column.id)}
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
                {(onRowEdit || onRowDelete || onRowView) && (
                  <TableCell
                    align="center"
                    sx={{
                      fontWeight: 'bold',
                      background:
                        'linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%)',
                      borderBottom: '2px solid rgba(102, 126, 234, 0.2)',
                    }}
                  >
                    Actions
                  </TableCell>
                )}
              </TableRow>
            </TableHead>

            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell
                    colSpan={
                      columns.length +
                      (selectable ? 1 : 0) +
                      (onRowEdit || onRowDelete || onRowView ? 1 : 0)
                    }
                  >
                    <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                      <Typography>Loading...</Typography>
                    </Box>
                  </TableCell>
                </TableRow>
              ) : paginatedData.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={
                      columns.length +
                      (selectable ? 1 : 0) +
                      (onRowEdit || onRowDelete || onRowView ? 1 : 0)
                    }
                  >
                    <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                      <Typography color="text.secondary">No data available</Typography>
                    </Box>
                  </TableCell>
                </TableRow>
              ) : (
                paginatedData.map((row, index) => {
                  const isItemSelected = isSelected(row);
                  return (
                    <TableRow
                      key={row.id || index}
                      hover
                      selected={isItemSelected}
                      onClick={() => onRowClick?.(row)}
                      sx={{
                        cursor: onRowClick ? 'pointer' : 'default',
                        '&:hover': {
                          background:
                            'linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%)',
                        },
                        '&.Mui-selected': {
                          background:
                            'linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%)',
                        },
                      }}
                    >
                      {selectable && (
                        <TableCell padding="checkbox">
                          <Checkbox
                            checked={isItemSelected}
                            onChange={() => handleSelectRow(row)}
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

                      {columns.map(column => (
                        <TableCell key={column.id} align={column.align || 'left'}>
                          {column.render
                            ? column.render(row[column.id], row)
                            : column.format
                              ? column.format(row[column.id], row)
                              : row[column.id]
                          }
                        </TableCell>
                      ))}

                      {/* Action Buttons */}
                      {(onRowEdit || onRowDelete || onRowView) && (
                        <TableCell align="center">
                          <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center' }}>
                            {onRowView && (
                              <Tooltip title="View">
                                <IconButton
                                  size="small"
                                  onClick={e => {
                                    e.stopPropagation();
                                    onRowView(row);
                                  }}
                                  sx={{ color: '#4facfe' }}
                                >
                                  <ViewIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )}
                            {onRowEdit && (
                              <Tooltip title="Edit">
                                <IconButton
                                  size="small"
                                  onClick={e => {
                                    e.stopPropagation();
                                    onRowEdit(row);
                                  }}
                                  sx={{ color: '#43e97b' }}
                                >
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )}
                            {onRowDelete && (
                              <Tooltip title="Delete">
                                <IconButton
                                  size="small"
                                  onClick={e => {
                                    e.stopPropagation();
                                    onRowDelete(row);
                                  }}
                                  sx={{ color: '#ff6b6b' }}
                                >
                                  <DeleteIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )}
                          </Box>
                        </TableCell>
                      )}
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </TableContainer>

        {/* Pagination */}
        {pagination && (
          <Box
            sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 2 }}
          >
            <Typography variant="body2" color="text.secondary">
              Showing {page * rowsPerPage + 1} to{' '}
              {Math.min((page + 1) * rowsPerPage, sortedData.length)} of {sortedData.length} entries
              {searchTerm && ` (filtered from ${data.length} total entries)`}
            </Typography>

            <TablePagination
              component="div"
              count={sortedData.length}
              page={page}
              onPageChange={(_, newPage) => setPage(newPage)}
              rowsPerPage={rowsPerPage}
              onRowsPerPageChange={e => {
                setRowsPerPage(parseInt(e.target.value, 10));
                setPage(0);
              }}
              rowsPerPageOptions={rowsPerPageOptions}
              sx={{
                '& .MuiTablePagination-selectLabel, & .MuiTablePagination-displayedRows': {
                  color: 'text.secondary',
                },
                '& .MuiTablePagination-select': {
                  color: '#667eea',
                },
                '& .MuiIconButton-root': {
                  color: '#667eea',
                },
              }}
            />
          </Box>
        )}
      </Box>
    </CustomCard>
  );
};

// Simple DataTable component that properly handles defaultActions and actions
export const DataTable: React.FC<DataTableProps> = ({
  data = [],
  columns = [],
  loading = false,
  selectable = false,
  selectedRows = [],
  onSelectionChange,
  defaultActions,
  actions = [],
  emptyMessage = 'No data available',
  rowKey = 'id',
  pagination,
  ...props
}) => {
  const [internalSelectedRows, setInternalSelectedRows] = useState<any[]>([]);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);

  // Use controlled or uncontrolled selection
  const currentSelectedRows = selectedRows || internalSelectedRows;
  const handleSelectionChange = onSelectionChange || setInternalSelectedRows;

  // Handle pagination
  const isPaginationObject = typeof pagination === 'object' && pagination !== null;
  const currentPage = isPaginationObject ? pagination.page - 1 : page; // Convert to 0-based
  const currentRowsPerPage = isPaginationObject ? pagination.rowsPerPage : rowsPerPage;
  const totalCount = isPaginationObject ? pagination.totalCount : data.length;

  // Paginate data only if pagination is boolean true (client-side pagination)
  const displayData = pagination === true
    ? data.slice(currentPage * currentRowsPerPage, (currentPage + 1) * currentRowsPerPage)
    : data;

  const handleSelectAll = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      handleSelectionChange(displayData);
    } else {
      handleSelectionChange([]);
    }
  };

  const handleSelectRow = (row: any) => {
    const rowId = row[rowKey];
    const isSelected = currentSelectedRows.some(selected => selected[rowKey] === rowId);

    if (isSelected) {
      handleSelectionChange(currentSelectedRows.filter(selected => selected[rowKey] !== rowId));
    } else {
      handleSelectionChange([...currentSelectedRows, row]);
    }
  };

  const isRowSelected = (row: any) => {
    return currentSelectedRows.some(selected => selected[rowKey] === row[rowKey]);
  };

  // Check if we need to show actions column
  const hasActions = defaultActions || actions.length > 0;

  return (
    <Box>
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              {selectable && (
                <TableCell padding="checkbox">
                  <Checkbox
                    indeterminate={currentSelectedRows.length > 0 && currentSelectedRows.length < displayData.length}
                    checked={displayData.length > 0 && currentSelectedRows.length === displayData.length}
                    onChange={handleSelectAll}
                  />
                </TableCell>
              )}

              {columns.map(column => (
                <TableCell key={column.id} align={column.align || 'left'}>
                  {column.label}
                </TableCell>
              ))}

              {hasActions && (
                <TableCell align="center">Actions</TableCell>
              )}
            </TableRow>
          </TableHead>

          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={columns.length + (selectable ? 1 : 0) + (hasActions ? 1 : 0)}>
                  <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                    <Typography>Loading...</Typography>
                  </Box>
                </TableCell>
              </TableRow>
            ) : displayData.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length + (selectable ? 1 : 0) + (hasActions ? 1 : 0)}>
                  <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                    <Typography color="text.secondary">{emptyMessage}</Typography>
                  </Box>
                </TableCell>
              </TableRow>
            ) : (
              displayData.map((row, index) => (
                <TableRow key={row[rowKey] || index} hover>
                  {selectable && (
                    <TableCell padding="checkbox">
                      <Checkbox
                        checked={isRowSelected(row)}
                        onChange={() => handleSelectRow(row)}
                      />
                    </TableCell>
                  )}

                  {columns.map(column => (
                    <TableCell key={column.id} align={column.align || 'left'}>
                      {column.render
                        ? column.render(row[column.id], row)
                        : column.format
                          ? column.format(row[column.id], row)
                          : row[column.id]
                      }
                    </TableCell>
                  ))}

                  {hasActions && (
                    <TableCell align="center">
                      <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center' }}>
                        {defaultActions?.view && (
                          <Tooltip title="View">
                            <IconButton
                              size="small"
                              onClick={() => defaultActions.view!(row)}
                              sx={{ color: 'primary.main' }}
                            >
                              <ViewIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
                        {defaultActions?.edit && (
                          <Tooltip title="Edit">
                            <IconButton
                              size="small"
                              onClick={() => defaultActions.edit!(row)}
                              sx={{ color: 'success.main' }}
                            >
                              <EditIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
                        {defaultActions?.delete && (
                          <Tooltip title="Delete">
                            <IconButton
                              size="small"
                              onClick={() => defaultActions.delete!(row)}
                              sx={{ color: 'error.main' }}
                            >
                              <DeleteIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
                        {actions.map((action, actionIndex) => (
                          <Tooltip key={actionIndex} title={action.label}>
                            <IconButton
                              size="small"
                              onClick={() => action.onClick(row)}
                              disabled={action.disabled ? action.disabled(row) : false}
                              sx={{ color: 'primary.main' }}
                            >
                              {action.icon}
                            </IconButton>
                          </Tooltip>
                        ))}
                      </Box>
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
          component="div"
          count={totalCount}
          page={currentPage}
          onPageChange={(_, newPage) => {
            if (isPaginationObject) {
              pagination.onPageChange(newPage + 1); // Convert back to 1-based
            } else {
              setPage(newPage);
            }
          }}
          rowsPerPage={currentRowsPerPage}
          onRowsPerPageChange={(e) => {
            const newSize = parseInt(e.target.value, 10);
            if (isPaginationObject) {
              pagination.onRowsPerPageChange(newSize);
            } else {
              setRowsPerPage(newSize);
              setPage(0);
            }
          }}
          rowsPerPageOptions={[5, 10, 25, 50]}
        />
      )}
    </Box>
  );
};

export default UltimateDataTable;
