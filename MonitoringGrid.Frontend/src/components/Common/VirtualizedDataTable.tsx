import React from 'react';
import DataTable, { DataTableProps } from '../UI/DataTable';

// VirtualizedDataTable is a wrapper around DataTable for backward compatibility
// In a real implementation, this would use react-window or react-virtualized
// for performance with large datasets

export interface VirtualizedDataTableProps extends DataTableProps {
  // Additional props for virtualization could be added here
  itemHeight?: number;
  overscan?: number;
}

export const VirtualizedDataTable: React.FC<VirtualizedDataTableProps> = ({
  itemHeight: _itemHeight = 50,
  overscan: _overscan = 5,
  ...props
}) => {
  // For now, this is just a wrapper around the regular DataTable
  // In a production environment, you would implement virtualization here
  return <DataTable {...props} />;
};

export default VirtualizedDataTable;
