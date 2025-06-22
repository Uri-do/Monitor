import React, { useMemo, useCallback, useRef, useEffect, useState } from 'react';
import {
  Box,
  Table,
  TableContainer,
  TablePagination,
  Paper,
  Divider,
  Typography,
} from '@mui/material';
import { FixedSizeList as List } from 'react-window';
import { CustomCard } from '../UI/Card';
import { DataTableHeader } from '../UI/DataTable/DataTableHeader';
import { DataTableFilters } from '../UI/DataTable/DataTableFilters';
import { DataTableHead } from '../UI/DataTable/DataTableHead';
import { useDataTable } from '@/hooks/useDataTable';
import { DataTableColumn } from '../UI/DataTable';

/**
 * Advanced Virtualized Data Table
 * High-performance table component for large datasets using react-window
 */

export interface VirtualizedDataTableProps {
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
  pagination?: boolean;
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
  // Virtualization specific props
  itemHeight?: number;
  overscan?: number;
  enableVirtualization?: boolean;
  virtualizationThreshold?: number;
}

// Virtualized row component
interface VirtualizedRowProps {
  index: number;
  style: React.CSSProperties;
  data: {
    items: any[];
    columns: DataTableColumn[];
    selected: any[];
    onSelectRow: (row: any) => void;
    isSelected: (row: any) => boolean;
    onRowClick?: (row: any) => void;
    actions?: any[];
    defaultActions?: any;
    rowKey?: string;
  };
}

const VirtualizedRow: React.FC<VirtualizedRowProps> = ({ index, style, data }) => {
  const {
    items,
    columns,
    selected,
    onSelectRow,
    isSelected,
    onRowClick,
    actions,
    defaultActions,
    rowKey,
  } = data;

  const row = items[index];
  const isRowSelected = isSelected(row);

  return (
    <div style={style}>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          px: 2,
          py: 1,
          borderBottom: '1px solid',
          borderColor: 'divider',
          backgroundColor: isRowSelected ? 'action.selected' : 'transparent',
          '&:hover': {
            backgroundColor: 'action.hover',
          },
          cursor: onRowClick ? 'pointer' : 'default',
        }}
        onClick={() => onRowClick?.(row)}
      >
        {columns.map((column) => (
          <Box
            key={column.id}
            sx={{
              flex: column.width || 1,
              textAlign: column.align || 'left',
              px: 1,
            }}
          >
            {column.render
              ? column.render(row[column.id], row)
              : column.format
              ? column.format(row[column.id], row)
              : row[column.id]}
          </Box>
        ))}
      </Box>
    </div>
  );
};

export const VirtualizedDataTable: React.FC<VirtualizedDataTableProps> = ({
  title = 'Data Table',
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
  rowsPerPageOptions = [5, 10, 25, 50, 100],
  defaultRowsPerPage = 25,
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
  defaultActions,
  actions,
  selectedRows,
  onSelectionChange,
  emptyMessage,
  rowKey,
  itemHeight = 50,
  overscan = 5,
  enableVirtualization = true,
  virtualizationThreshold = 100,
}) => {
  const listRef = useRef<List>(null);
  const [containerHeight, setContainerHeight] = useState(maxHeight);

  const {
    page,
    rowsPerPage,
    orderBy,
    order,
    searchTerm,
    filters,
    selected,
    exportMenuAnchor,
    bulkMenuAnchor,
    paginatedData,
    sortedData,
    handleSort,
    handleSelectAll,
    handleSelectRow,
    handleExport,
    handleBulkAction,
    handlePageChange,
    handleRowsPerPageChange,
    handleSearchChange,
    handleFilterChange,
    isSelected,
    setExportMenuAnchor,
    setBulkMenuAnchor,
  } = useDataTable({
    data,
    columns,
    defaultRowsPerPage,
    rowKey,
  });

  // Determine if virtualization should be used
  const shouldVirtualize = enableVirtualization && sortedData.length > virtualizationThreshold;

  // Calculate container height
  useEffect(() => {
    if (typeof maxHeight === 'number') {
      setContainerHeight(maxHeight);
    } else if (height) {
      setContainerHeight(typeof height === 'number' ? height : 400);
    }
  }, [height, maxHeight]);

  // Sync external selection state
  useEffect(() => {
    if (onSelectionChange) {
      onSelectionChange(selected);
    }
  }, [selected, onSelectionChange]);

  // Prepare data for virtualized list
  const virtualizedData = useMemo(() => ({
    items: pagination ? paginatedData : sortedData,
    columns,
    selected,
    onSelectRow: handleSelectRow,
    isSelected,
    onRowClick,
    actions,
    defaultActions,
    rowKey,
  }), [
    pagination,
    paginatedData,
    sortedData,
    columns,
    selected,
    handleSelectRow,
    isSelected,
    onRowClick,
    actions,
    defaultActions,
    rowKey,
  ]);

  return (
    <CustomCard gradient={gradient} style={{ height: height || 'auto' }}>
      <div style={{ padding: '24px' }}>
        {/* Header */}
        <DataTableHeader
          title={title}
          subtitle={subtitle}
          gradient={gradient}
          loading={loading}
          refreshable={refreshable}
          exportable={exportable}
          selected={selected}
          exportMenuAnchor={exportMenuAnchor}
          bulkMenuAnchor={bulkMenuAnchor}
          onRefresh={onRefresh}
          onExportMenuOpen={(e) => setExportMenuAnchor(e.currentTarget)}
          onExportMenuClose={() => setExportMenuAnchor(null)}
          onBulkMenuOpen={(e) => setBulkMenuAnchor(e.currentTarget)}
          onBulkMenuClose={() => setBulkMenuAnchor(null)}
          onExport={(format) => handleExport(format, onExport)}
          onBulkAction={(action) => handleBulkAction(action, onBulkAction)}
        />

        {/* Search and Filters */}
        <DataTableFilters
          searchable={searchable}
          filterable={filterable}
          searchTerm={searchTerm}
          filters={filters}
          columns={columns}
          onSearchChange={handleSearchChange}
          onFilterChange={handleFilterChange}
        />

        <Divider sx={{ mb: 2 }} />

        {/* Data Display */}
        {sortedData.length === 0 ? (
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              minHeight: 200,
            }}
          >
            <Typography variant="body2" color="text.secondary">
              {emptyMessage || 'No data available'}
            </Typography>
          </Box>
        ) : shouldVirtualize ? (
          // Virtualized rendering for large datasets
          <Box>
            {/* Table Header */}
            <DataTableHead
              columns={columns}
              selectable={selectable}
              selected={selected}
              data={virtualizedData.items}
              gradient={gradient}
              orderBy={orderBy}
              order={order}
              actions={actions}
              defaultActions={defaultActions}
              onRowEdit={onRowEdit}
              onRowDelete={onRowDelete}
              onRowView={onRowView}
              onSelectAll={handleSelectAll}
              onSort={handleSort}
            />

            {/* Virtualized List */}
            <List
              ref={listRef}
              height={containerHeight}
              itemCount={virtualizedData.items.length}
              itemSize={itemHeight}
              itemData={virtualizedData}
              overscanCount={overscan}
            >
              {VirtualizedRow}
            </List>
          </Box>
        ) : (
          // Regular table rendering for smaller datasets
          <TableContainer
            component={Paper}
            sx={{
              maxHeight: containerHeight,
              borderRadius: 2,
            }}
          >
            <Table stickyHeader>
              <DataTableHead
                columns={columns}
                selectable={selectable}
                selected={selected}
                data={virtualizedData.items}
                gradient={gradient}
                orderBy={orderBy}
                order={order}
                actions={actions}
                defaultActions={defaultActions}
                onRowEdit={onRowEdit}
                onRowDelete={onRowDelete}
                onRowView={onRowView}
                onSelectAll={handleSelectAll}
                onSort={handleSort}
              />
              {/* Regular table body would go here */}
            </Table>
          </TableContainer>
        )}

        {/* Pagination */}
        {pagination && (
          <TablePagination
            rowsPerPageOptions={rowsPerPageOptions}
            component="div"
            count={sortedData.length}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={(_, newPage) => handlePageChange(newPage)}
            onRowsPerPageChange={(e) => handleRowsPerPageChange(parseInt(e.target.value, 10))}
            sx={{
              borderTop: theme =>
                theme.palette.mode === 'dark'
                  ? '1px solid rgba(255, 255, 255, 0.1)'
                  : '1px solid rgba(102, 126, 234, 0.2)',
              mt: 2,
            }}
          />
        )}
      </div>
    </CustomCard>
  );
};

export default VirtualizedDataTable;
