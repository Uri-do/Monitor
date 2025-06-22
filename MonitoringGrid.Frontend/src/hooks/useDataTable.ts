import { useState, useMemo } from 'react';
import { DataTableColumn } from '@/components/UI/DataTable';

interface UseDataTableProps {
  data: any[];
  columns: DataTableColumn[];
  defaultRowsPerPage?: number;
  rowKey?: string;
}

export const useDataTable = ({
  data,
  columns,
  defaultRowsPerPage = 10,
  rowKey,
}: UseDataTableProps) => {
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
    // Ensure data is always an array
    let filtered = Array.isArray(data) ? data : [];

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
    const startIndex = page * rowsPerPage;
    return sortedData.slice(startIndex, startIndex + rowsPerPage);
  }, [sortedData, page, rowsPerPage]);

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
    const rowId = rowKey ? row[rowKey] : row.id;
    const selectedIndex = selected.findIndex(item => {
      const itemId = rowKey ? item[rowKey] : item.id;
      return itemId === rowId;
    });
    let newSelected: any[] = [];

    if (selectedIndex === -1) {
      newSelected = [...selected, row];
    } else {
      newSelected = selected.filter(item => {
        const itemId = rowKey ? item[rowKey] : item.id;
        return itemId !== rowId;
      });
    }

    setSelected(newSelected);
  };

  const handleExport = (format: 'csv' | 'excel' | 'pdf', onExport?: (data: any[], format: 'csv' | 'excel' | 'pdf') => void) => {
    if (onExport) {
      onExport(selected.length > 0 ? selected : sortedData, format);
    }
    setExportMenuAnchor(null);
  };

  const handleBulkAction = (action: string, onBulkAction?: (selectedRows: any[], action: string) => void) => {
    if (onBulkAction && selected.length > 0) {
      onBulkAction(selected, action);
    }
    setBulkMenuAnchor(null);
    setSelected([]);
  };

  const isSelected = (row: any) => {
    const rowId = rowKey ? row[rowKey] : row.id;
    return (
      selected.findIndex(item => {
        const itemId = rowKey ? item[rowKey] : item.id;
        return itemId === rowId;
      }) !== -1
    );
  };

  const handlePageChange = (newPage: number) => {
    setPage(newPage);
  };

  const handleRowsPerPageChange = (newRowsPerPage: number) => {
    setRowsPerPage(newRowsPerPage);
    setPage(0);
  };

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    setPage(0); // Reset to first page when searching
  };

  const handleFilterChange = (columnId: string, value: any) => {
    setFilters(prev => ({ ...prev, [columnId]: value }));
    setPage(0); // Reset to first page when filtering
  };

  return {
    // State
    page,
    rowsPerPage,
    orderBy,
    order,
    searchTerm,
    filters,
    selected,
    exportMenuAnchor,
    bulkMenuAnchor,
    
    // Computed data
    filteredData,
    sortedData,
    paginatedData,
    
    // Handlers
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
    
    // Menu handlers
    setExportMenuAnchor,
    setBulkMenuAnchor,
  };
};

export default useDataTable;
