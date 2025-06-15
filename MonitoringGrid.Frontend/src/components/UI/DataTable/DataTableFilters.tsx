import React from 'react';
import {
  Box,
  Stack,
  TextField,
  InputAdornment,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material';
import { Search as SearchIcon } from '@mui/icons-material';
import { DataTableColumn } from '../DataTable';

interface DataTableFiltersProps {
  searchable: boolean;
  filterable: boolean;
  searchTerm: string;
  filters: Record<string, any>;
  columns: DataTableColumn[];
  onSearchChange: (value: string) => void;
  onFilterChange: (columnId: string, value: any) => void;
}

export const DataTableFilters: React.FC<DataTableFiltersProps> = ({
  searchable,
  filterable,
  searchTerm,
  filters,
  columns,
  onSearchChange,
  onFilterChange,
}) => {
  return (
    <Box sx={{ mb: 3 }}>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems="center">
        {searchable && (
          <TextField
            placeholder="Search across all columns..."
            value={searchTerm}
            onChange={e => onSearchChange(e.target.value)}
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
                      onChange={e => onFilterChange(column.id, e.target.value)}
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
                      onChange={e => onFilterChange(column.id, e.target.value)}
                      label={column.label}
                    />
                  )}
                </FormControl>
              ))}
          </Box>
        )}
      </Stack>
    </Box>
  );
};

export default DataTableFilters;
