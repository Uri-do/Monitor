import React from 'react';
import { Table, Pagination, Divider } from 'antd';
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

        <Divider style={{ marginBottom: '16px' }} />

        {/* Data Table */}
        <div
          style={{
            ...(maxHeight && maxHeight !== 'none' && { maxHeight }),
            borderRadius: '8px',
            overflow: 'auto',
          }}
        >
          <Table
            sticky
            dataSource={paginatedData}
            columns={[
              ...(selectable ? [{
                title: '',
                key: 'selection',
                width: 50,
                render: (_: any, record: any) => (
                  <input
                    type="checkbox"
                    checked={isSelected(record)}
                    onChange={() => handleSelectRow(record)}
                  />
                ),
              }] : []),
              ...columns.map((col: any) => ({
                title: col.label,
                dataIndex: col.id || col.key,
                key: col.id || col.key,
                sorter: col.sortable,
                render: col.render || ((text: any) => {
                  // Safely render primitive values only
                  if (text === null || text === undefined) return '';
                  if (typeof text === 'object') return JSON.stringify(text);
                  return String(text);
                }),
              })),
              ...(actions || defaultActions ? [{
                title: 'Actions',
                key: 'actions',
                width: 120,
                render: (_: any, record: any) => (
                  <div style={{ display: 'flex', gap: '2px', alignItems: 'center' }}>
                    {/* Default Actions */}
                    {defaultActions?.view && (
                      <button
                        onClick={() => defaultActions.view!(record)}
                        style={{
                          padding: '2px 4px',
                          border: 'none',
                          borderRadius: '2px',
                          background: 'transparent',
                          cursor: 'pointer',
                          fontSize: '14px',
                          minWidth: '20px',
                          height: '20px'
                        }}
                        title="View"
                      >
                        👁️
                      </button>
                    )}
                    {defaultActions?.edit && (
                      <button
                        onClick={() => defaultActions.edit!(record)}
                        style={{
                          padding: '2px 4px',
                          border: 'none',
                          borderRadius: '2px',
                          background: 'transparent',
                          cursor: 'pointer',
                          fontSize: '14px',
                          minWidth: '20px',
                          height: '20px'
                        }}
                        title="Edit"
                      >
                        ✏️
                      </button>
                    )}
                    {defaultActions?.delete && (
                      <button
                        onClick={() => defaultActions.delete!(record)}
                        style={{
                          padding: '2px 4px',
                          border: 'none',
                          borderRadius: '2px',
                          background: 'transparent',
                          cursor: 'pointer',
                          fontSize: '14px',
                          minWidth: '20px',
                          height: '20px'
                        }}
                        title="Delete"
                      >
                        🗑️
                      </button>
                    )}

                    {/* Custom Actions */}
                    {actions?.map((action, index) => (
                      <button
                        key={index}
                        onClick={() => action.onClick(record)}
                        disabled={action.disabled?.(record)}
                        style={{
                          padding: '2px 4px',
                          border: 'none',
                          borderRadius: '2px',
                          background: 'transparent',
                          cursor: action.disabled?.(record) ? 'not-allowed' : 'pointer',
                          fontSize: '14px',
                          minWidth: '20px',
                          height: '20px',
                          opacity: action.disabled?.(record) ? 0.5 : 1
                        }}
                        title={action.label}
                      >
                        ▶️
                      </button>
                    ))}
                  </div>
                ),
              }] : []),
            ]}
            rowKey={rowKey}
            loading={loading}
            pagination={false}
            onRow={(record) => ({
              onClick: () => onRowClick?.(record),
            })}
            locale={{
              emptyText: emptyMessage || 'No data available',
            }}
          />
        </div>

        {/* Pagination */}
        {pagination && (
          <Pagination
            current={page + 1}
            total={sortedData.length}
            pageSize={rowsPerPage}
            showSizeChanger
            showQuickJumper
            showTotal={(total, range) => `${range[0]}-${range[1]} of ${total} items`}
            onChange={(newPage) => handlePageChange(newPage - 1)}
            onShowSizeChange={(current, size) => handleRowsPerPageChange(size)}
            style={{
              marginTop: '16px',
              textAlign: 'center',
              borderTop: '1px solid rgba(102, 126, 234, 0.2)',
              paddingTop: '16px',
            }}
          />
        )}
      </div>
    </CustomCard>
  );
};

export default DataTable;
