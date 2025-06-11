import React, { useMemo, useState, useCallback } from 'react';
import { FixedSizeList as List } from 'react-window';
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
  Stack,
  Button,
  Checkbox,
  TableHead,
  TableRow,
  TableCell,
  Paper,
  useTheme,
} from '@mui/material';
import {
  Search as SearchIcon,
  FilterList as FilterIcon,
  Clear as ClearIcon,
  Sort as SortIcon,
  ArrowUpward as SortAscIcon,
  ArrowDownward as SortDescIcon,
} from '@mui/icons-material';

interface VirtualizedColumn<T = any> {
  field: keyof T;
  headerName: string;
  width: number;
  sortable?: boolean;
  filterable?: boolean;
  renderCell?: (value: any, row: T, index: number) => React.ReactNode;
  align?: 'left' | 'center' | 'right';
}

interface VirtualizedDataTableProps<T = any> {
  data: T[];
  columns: VirtualizedColumn<T>[];
  height?: number;
  rowHeight?: number;
  loading?: boolean;
  searchable?: boolean;
  sortable?: boolean;
  selectable?: boolean;
  selectedRows?: Set<string | number>;
  onSelectionChange?: (selected: Set<string | number>) => void;
  getRowId?: (row: T) => string | number;
  onRowClick?: (row: T, index: number) => void;
  onRowDoubleClick?: (row: T, index: number) => void;
  overscan?: number;
  stickyHeader?: boolean;
  striped?: boolean;
  dense?: boolean;
}

interface SortConfig {
  field: keyof any;
  direction: 'asc' | 'desc';
}

export const VirtualizedDataTable = <T extends Record<string, any>>({
  data,
  columns,
  height = 600,
  rowHeight = 52,
  loading = false,
  searchable = true,
  sortable = true,
  selectable = false,
  selectedRows = new Set(),
  onSelectionChange,
  getRowId = (row, index) => row.id || index,
  onRowClick,
  onRowDoubleClick,
  overscan = 5,
  stickyHeader = true,
  striped = true,
  dense = false,
}: VirtualizedDataTableProps<T>) => {
  const theme = useTheme();
  const [searchTerm, setSearchTerm] = useState('');
  const [sortConfig, setSortConfig] = useState<SortConfig | null>(null);

  // Calculate total width for horizontal scrolling
  const totalWidth = useMemo(() => {
    const baseWidth = columns.reduce((sum, col) => sum + col.width, 0);
    return selectable ? baseWidth + 60 : baseWidth; // Add checkbox column width
  }, [columns, selectable]);

  // Filter and sort data
  const processedData = useMemo(() => {
    let result = [...data];

    // Apply search filter
    if (searchable && searchTerm) {
      result = result.filter(row =>
        Object.values(row).some(value =>
          String(value).toLowerCase().includes(searchTerm.toLowerCase())
        )
      );
    }

    // Apply sorting
    if (sortConfig && sortable) {
      result.sort((a, b) => {
        const aValue = a[sortConfig.field];
        const bValue = b[sortConfig.field];
        
        if (aValue === bValue) return 0;
        
        const comparison = aValue < bValue ? -1 : 1;
        return sortConfig.direction === 'desc' ? -comparison : comparison;
      });
    }

    return result;
  }, [data, searchTerm, sortConfig, searchable, sortable]);

  const handleSort = useCallback((field: keyof T) => {
    if (!sortable) return;
    
    setSortConfig(prev => {
      if (prev?.field === field) {
        return prev.direction === 'asc' 
          ? { field, direction: 'desc' }
          : null; // Remove sort
      }
      return { field, direction: 'asc' };
    });
  }, [sortable]);

  const handleSelectAll = useCallback(() => {
    if (!selectable || !onSelectionChange) return;
    
    const allIds = processedData.map((row, index) => getRowId(row, index));
    const newSelected = selectedRows.size === allIds.length 
      ? new Set() 
      : new Set(allIds);
    
    onSelectionChange(newSelected);
  }, [selectable, onSelectionChange, processedData, selectedRows, getRowId]);

  const handleRowSelect = useCallback((rowId: string | number) => {
    if (!selectable || !onSelectionChange) return;
    
    const newSelected = new Set(selectedRows);
    if (newSelected.has(rowId)) {
      newSelected.delete(rowId);
    } else {
      newSelected.add(rowId);
    }
    onSelectionChange(newSelected);
  }, [selectable, onSelectionChange, selectedRows]);

  // Row renderer for react-window
  const Row = useCallback(({ index, style }: { index: number; style: React.CSSProperties }) => {
    const row = processedData[index];
    const rowId = getRowId(row, index);
    const isSelected = selectedRows.has(rowId);
    const isEven = index % 2 === 0;

    return (
      <div
        style={{
          ...style,
          display: 'flex',
          alignItems: 'center',
          borderBottom: `1px solid ${theme.palette.divider}`,
          backgroundColor: striped && !isEven 
            ? theme.palette.action.hover 
            : 'transparent',
          cursor: onRowClick ? 'pointer' : 'default',
        }}
        onClick={() => onRowClick?.(row, index)}
        onDoubleClick={() => onRowDoubleClick?.(row, index)}
      >
        {/* Selection checkbox */}
        {selectable && (
          <div style={{ width: 60, display: 'flex', justifyContent: 'center' }}>
            <Checkbox
              checked={isSelected}
              onChange={() => handleRowSelect(rowId)}
              size={dense ? 'small' : 'medium'}
              onClick={(e) => e.stopPropagation()}
            />
          </div>
        )}

        {/* Data cells */}
        {columns.map((column) => {
          const value = row[column.field];
          const cellContent = column.renderCell 
            ? column.renderCell(value, row, index)
            : String(value || '');

          return (
            <div
              key={String(column.field)}
              style={{
                width: column.width,
                padding: dense ? '8px 12px' : '12px 16px',
                textAlign: column.align || 'left',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {cellContent}
            </div>
          );
        })}
      </div>
    );
  }, [
    processedData,
    getRowId,
    selectedRows,
    theme,
    striped,
    onRowClick,
    onRowDoubleClick,
    selectable,
    columns,
    dense,
    handleRowSelect,
  ]);

  return (
    <Card>
      <CardContent>
        {/* Search and controls */}
        <Box sx={{ mb: 2 }}>
          <Stack direction="row" spacing={2} alignItems="center">
            {searchable && (
              <TextField
                size="small"
                placeholder="Search..."
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
                sx={{ minWidth: 300 }}
              />
            )}

            {sortConfig && (
              <Chip
                label={`Sorted by ${String(sortConfig.field)} (${sortConfig.direction})`}
                onDelete={() => setSortConfig(null)}
                size="small"
                color="primary"
              />
            )}

            <Typography variant="body2" color="text.secondary" sx={{ ml: 'auto' }}>
              {processedData.length} of {data.length} rows
            </Typography>
          </Stack>
        </Box>

        {/* Table container */}
        <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
          {/* Header */}
          {stickyHeader && (
            <Box
              sx={{
                display: 'flex',
                backgroundColor: theme.palette.grey[50],
                borderBottom: `2px solid ${theme.palette.divider}`,
                position: 'sticky',
                top: 0,
                zIndex: 1,
              }}
            >
              {/* Select all checkbox */}
              {selectable && (
                <div style={{ width: 60, display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
                  <Checkbox
                    checked={processedData.length > 0 && selectedRows.size === processedData.length}
                    indeterminate={selectedRows.size > 0 && selectedRows.size < processedData.length}
                    onChange={handleSelectAll}
                    size={dense ? 'small' : 'medium'}
                  />
                </div>
              )}

              {/* Column headers */}
              {columns.map((column) => (
                <div
                  key={String(column.field)}
                  style={{
                    width: column.width,
                    padding: dense ? '8px 12px' : '12px 16px',
                    fontWeight: 600,
                    textAlign: column.align || 'left',
                    cursor: column.sortable !== false && sortable ? 'pointer' : 'default',
                    display: 'flex',
                    alignItems: 'center',
                    gap: 4,
                  }}
                  onClick={() => column.sortable !== false && handleSort(column.field)}
                >
                  <span>{column.headerName}</span>
                  {column.sortable !== false && sortable && (
                    <Box sx={{ display: 'flex', flexDirection: 'column', ml: 0.5 }}>
                      {sortConfig?.field === column.field ? (
                        sortConfig.direction === 'asc' ? (
                          <SortAscIcon fontSize="small" />
                        ) : (
                          <SortDescIcon fontSize="small" />
                        )
                      ) : (
                        <SortIcon fontSize="small" sx={{ opacity: 0.5 }} />
                      )}
                    </Box>
                  )}
                </div>
              ))}
            </Box>
          )}

          {/* Virtualized rows */}
          <Box sx={{ width: totalWidth, minWidth: '100%' }}>
            {loading ? (
              <Box sx={{ p: 4, textAlign: 'center' }}>
                <Typography>Loading...</Typography>
              </Box>
            ) : processedData.length === 0 ? (
              <Box sx={{ p: 4, textAlign: 'center' }}>
                <Typography color="text.secondary">
                  {searchTerm ? 'No matching results found' : 'No data available'}
                </Typography>
              </Box>
            ) : (
              <List
                height={height - (stickyHeader ? 60 : 0)}
                itemCount={processedData.length}
                itemSize={dense ? 40 : rowHeight}
                overscanCount={overscan}
                width="100%"
              >
                {Row}
              </List>
            )}
          </Box>
        </Paper>
      </CardContent>
    </Card>
  );
};
