import React from 'react';
import {
  Box,
  Table,
  TableContainer,
  TablePagination,
  Paper,
  Divider,
} from '@mui/material';
import { CustomCard } from './Card';
import { DataTableHeader } from './DataTable/DataTableHeader';
import { DataTableFilters } from './DataTable/DataTableFilters';
import { DataTableHead } from './DataTable/DataTableHead';
import { DataTableBody } from './DataTable/DataTableBody';
import { useDataTable } from '@/hooks/useDataTable';

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
  pagination?:
    | boolean
    | {
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

export const DataTable: React.FC<DataTableProps> = ({
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
  defaultActions,
  actions,
  selectedRows,
  onSelectionChange,
  emptyMessage,
  rowKey,
}) => {
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

  // Sync external selection state
  React.useEffect(() => {
    if (onSelectionChange) {
      onSelectionChange(selected);
    }
  }, [selected, onSelectionChange]);

  return (
    <CustomCard gradient={gradient} sx={{ height: height || 'auto' }}>
      <Box sx={{ p: 3 }}>
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

        {/* Data Table */}
        <TableContainer
          component={Paper}
          sx={{
            maxHeight,
            borderRadius: 2,
            '&::-webkit-scrollbar': {
              width: '8px',
              height: '8px',
            },
            '&::-webkit-scrollbar-track': {
              backgroundColor: theme =>
                theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.05)' : 'rgba(0, 0, 0, 0.05)',
              borderRadius: '4px',
            },
            '&::-webkit-scrollbar-thumb': {
              backgroundColor: theme =>
                theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.2)' : 'rgba(0, 0, 0, 0.2)',
              borderRadius: '4px',
              '&:hover': {
                backgroundColor: theme =>
                  theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.3)' : 'rgba(0, 0, 0, 0.3)',
              },
            },
          }}
        >
          <Table stickyHeader>
            <DataTableHead
              columns={columns}
              selectable={selectable}
              selected={selected}
              data={paginatedData}
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

            <DataTableBody
              loading={loading}
              data={paginatedData}
              columns={columns}
              selectable={selectable}
              selected={selected}
              gradient={gradient}
              rowKey={rowKey}
              emptyMessage={emptyMessage}
              actions={actions}
              defaultActions={defaultActions}
              onRowClick={onRowClick}
              onRowEdit={onRowEdit}
              onRowDelete={onRowDelete}
              onRowView={onRowView}
              onSelectRow={handleSelectRow}
              isSelected={isSelected}
            />
          </Table>
        </TableContainer>

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
      </Box>
    </CustomCard>
  );
};

export default DataTable;
