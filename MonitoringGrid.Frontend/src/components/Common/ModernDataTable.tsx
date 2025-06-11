import React, { useState, useMemo } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  InputAdornment,
  IconButton,
  Tooltip,
  Chip,
  Button,
  Menu,
  MenuItem,
  FormControl,
  InputLabel,
  Select,
  Checkbox,
  ListItemText,
  Alert,
} from '@mui/material';
import {
  Search as SearchIcon,
  FilterList as FilterIcon,
  ViewColumn as ColumnIcon,
  Download as ExportIcon,
  Refresh as RefreshIcon,
  Clear as ClearIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridRowSelectionModel, GridToolbar } from '@mui/x-data-grid';

interface FilterOption {
  field: string;
  label: string;
  type: 'text' | 'select' | 'boolean' | 'date';
  options?: { value: any; label: string }[];
}

interface ModernDataTableProps<T = any> {
  title?: string;
  subtitle?: string;
  data: T[];
  columns: GridColDef[];
  loading?: boolean;
  error?: string | null;
  searchable?: boolean;
  searchPlaceholder?: string;
  filterable?: boolean;
  filterOptions?: FilterOption[];
  exportable?: boolean;
  refreshable?: boolean;
  onRefresh?: () => void;
  onExport?: (data: T[]) => void;
  selectable?: boolean;
  selectedRows?: GridRowSelectionModel;
  onSelectionChange?: (selection: GridRowSelectionModel) => void;
  actions?: React.ReactNode;
  getRowId?: (row: T) => string | number;
  pageSize?: number;
  pageSizeOptions?: number[];
  height?: number;
  density?: 'compact' | 'standard' | 'comfortable';
  autoHeight?: boolean;
}

export const ModernDataTable = <T extends Record<string, any>>({
  title,
  subtitle,
  data,
  columns,
  loading = false,
  error,
  searchable = true,
  searchPlaceholder = 'Search...',
  filterable = false,
  filterOptions = [],
  exportable = false,
  refreshable = false,
  onRefresh,
  onExport,
  selectable = false,
  selectedRows = [],
  onSelectionChange,
  actions,
  getRowId,
  pageSize = 25,
  pageSizeOptions = [10, 25, 50, 100],
  height = 600,
  density = 'standard',
  autoHeight = false,
}: ModernDataTableProps<T>) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filters, setFilters] = useState<Record<string, any>>({});
  const [filterMenuAnchor, setFilterMenuAnchor] = useState<null | HTMLElement>(null);
  const [columnMenuAnchor, setColumnMenuAnchor] = useState<null | HTMLElement>(null);
  const [visibleColumns, setVisibleColumns] = useState<string[]>(
    columns.map(col => col.field)
  );

  // Filter and search data
  const filteredData = useMemo(() => {
    let result = [...data];

    // Apply search
    if (searchable && searchTerm) {
      result = result.filter(row =>
        Object.values(row).some(value =>
          String(value).toLowerCase().includes(searchTerm.toLowerCase())
        )
      );
    }

    // Apply filters
    Object.entries(filters).forEach(([field, value]) => {
      if (value !== undefined && value !== '' && value !== null) {
        result = result.filter(row => {
          const rowValue = row[field];
          if (Array.isArray(value)) {
            return value.includes(rowValue);
          }
          return String(rowValue).toLowerCase().includes(String(value).toLowerCase());
        });
      }
    });

    return result;
  }, [data, searchTerm, filters, searchable]);

  // Filter visible columns
  const visibleColumnDefs = useMemo(() => {
    return columns.filter(col => visibleColumns.includes(col.field));
  }, [columns, visibleColumns]);

  const handleFilterChange = (field: string, value: any) => {
    setFilters(prev => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleClearFilters = () => {
    setFilters({});
    setSearchTerm('');
  };

  const handleExport = () => {
    if (onExport) {
      onExport(filteredData);
    }
  };

  const handleColumnVisibilityChange = (field: string) => {
    setVisibleColumns(prev =>
      prev.includes(field)
        ? prev.filter(col => col !== field)
        : [...prev, field]
    );
  };

  const activeFiltersCount = Object.values(filters).filter(
    value => value !== undefined && value !== '' && value !== null
  ).length + (searchTerm ? 1 : 0);

  return (
    <Card>
      <CardContent>
        {/* Header */}
        {(title || subtitle || actions) && (
          <Box sx={{ mb: 2 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
              {(title || subtitle) && (
                <Box>
                  {title && (
                    <Typography variant="h6" component="h2">
                      {title}
                    </Typography>
                  )}
                  {subtitle && (
                    <Typography variant="body2" color="text.secondary">
                      {subtitle}
                    </Typography>
                  )}
                </Box>
              )}
              {actions && <Box>{actions}</Box>}
            </Box>
          </Box>
        )}

        {/* Toolbar */}
        <Box sx={{ display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap', alignItems: 'center' }}>
          {/* Search */}
          {searchable && (
            <TextField
              size="small"
              placeholder={searchPlaceholder}
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
                endAdornment: searchTerm && (
                  <InputAdornment position="end">
                    <IconButton size="small" onClick={() => setSearchTerm('')}>
                      <ClearIcon />
                    </IconButton>
                  </InputAdornment>
                ),
              }}
              sx={{ minWidth: 250 }}
            />
          )}

          {/* Filter Button */}
          {filterable && filterOptions.length > 0 && (
            <Tooltip title="Filters">
              <IconButton
                onClick={(e) => setFilterMenuAnchor(e.currentTarget)}
                color={activeFiltersCount > 0 ? 'primary' : 'default'}
              >
                <FilterIcon />
                {activeFiltersCount > 0 && (
                  <Chip
                    label={activeFiltersCount}
                    size="small"
                    color="primary"
                    sx={{ position: 'absolute', top: -8, right: -8, minWidth: 20, height: 20 }}
                  />
                )}
              </IconButton>
            </Tooltip>
          )}

          {/* Column Visibility */}
          <Tooltip title="Columns">
            <IconButton onClick={(e) => setColumnMenuAnchor(e.currentTarget)}>
              <ColumnIcon />
            </IconButton>
          </Tooltip>

          {/* Refresh */}
          {refreshable && onRefresh && (
            <Tooltip title="Refresh">
              <IconButton onClick={onRefresh}>
                <RefreshIcon />
              </IconButton>
            </Tooltip>
          )}

          {/* Export */}
          {exportable && onExport && (
            <Tooltip title="Export">
              <IconButton onClick={handleExport}>
                <ExportIcon />
              </IconButton>
            </Tooltip>
          )}

          {/* Clear Filters */}
          {activeFiltersCount > 0 && (
            <Button
              size="small"
              startIcon={<ClearIcon />}
              onClick={handleClearFilters}
            >
              Clear Filters
            </Button>
          )}

          {/* Results Count */}
          <Typography variant="body2" color="text.secondary" sx={{ ml: 'auto' }}>
            {filteredData.length} of {data.length} items
          </Typography>
        </Box>

        {/* Error Display */}
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {/* Data Grid */}
        <DataGrid
          rows={filteredData}
          columns={visibleColumnDefs}
          loading={loading}
          getRowId={getRowId}
          checkboxSelection={selectable}
          rowSelectionModel={selectedRows}
          onRowSelectionModelChange={onSelectionChange}
          pageSizeOptions={pageSizeOptions}
          initialState={{
            pagination: { paginationModel: { pageSize } },
            density,
          }}
          slots={{
            toolbar: GridToolbar,
          }}
          slotProps={{
            toolbar: {
              showQuickFilter: false,
              printOptions: { disableToolbarButton: true },
              csvOptions: { disableToolbarButton: !exportable },
            },
          }}
          disableRowSelectionOnClick={!selectable}
          autoHeight={autoHeight}
          sx={{ height: autoHeight ? 'auto' : height }}
        />

        {/* Filter Menu */}
        <Menu
          anchorEl={filterMenuAnchor}
          open={Boolean(filterMenuAnchor)}
          onClose={() => setFilterMenuAnchor(null)}
          PaperProps={{ sx: { minWidth: 300, maxHeight: 400 } }}
        >
          {filterOptions.map((option) => (
            <MenuItem key={option.field} sx={{ flexDirection: 'column', alignItems: 'stretch' }}>
              {option.type === 'select' ? (
                <FormControl fullWidth size="small">
                  <InputLabel>{option.label}</InputLabel>
                  <Select
                    value={filters[option.field] || ''}
                    onChange={(e) => handleFilterChange(option.field, e.target.value)}
                    label={option.label}
                  >
                    <MenuItem value="">All</MenuItem>
                    {option.options?.map((opt) => (
                      <MenuItem key={opt.value} value={opt.value}>
                        {opt.label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              ) : (
                <TextField
                  fullWidth
                  size="small"
                  label={option.label}
                  value={filters[option.field] || ''}
                  onChange={(e) => handleFilterChange(option.field, e.target.value)}
                  type={option.type === 'date' ? 'date' : 'text'}
                />
              )}
            </MenuItem>
          ))}
        </Menu>

        {/* Column Visibility Menu */}
        <Menu
          anchorEl={columnMenuAnchor}
          open={Boolean(columnMenuAnchor)}
          onClose={() => setColumnMenuAnchor(null)}
          PaperProps={{ sx: { maxHeight: 400 } }}
        >
          {columns.map((column) => (
            <MenuItem key={column.field} onClick={() => handleColumnVisibilityChange(column.field)}>
              <Checkbox checked={visibleColumns.includes(column.field)} />
              <ListItemText primary={column.headerName || column.field} />
            </MenuItem>
          ))}
        </Menu>
      </CardContent>
    </Card>
  );
};
