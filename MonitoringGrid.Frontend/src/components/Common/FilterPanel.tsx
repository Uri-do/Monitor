import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  IconButton,
  Collapse,
  Grid,
  Chip,
  Stack,
  Divider,
} from '@mui/material';
import {
  FilterList as FilterIcon,
  Clear as ClearIcon,
  ExpandMore as ExpandIcon,
  ExpandLess as CollapseIcon,
  Search as SearchIcon,
} from '@mui/icons-material';
import { FormFieldOption } from './FormField';

export interface FilterField {
  name: string;
  label: string;
  type: 'text' | 'select' | 'date' | 'dateRange' | 'number' | 'multiSelect';
  options?: FormFieldOption[];
  placeholder?: string;
  defaultValue?: any;
}

export interface ActiveFilter {
  field: string;
  label: string;
  value: any;
  displayValue: string;
}

interface FilterPanelProps {
  fields: FilterField[];
  onFilterChange: (filters: Record<string, any>) => void;
  onClear: () => void;
  initialFilters?: Record<string, any>;
  showSearch?: boolean;
  searchPlaceholder?: string;
  onSearch?: (searchTerm: string) => void;
  collapsible?: boolean;
  defaultExpanded?: boolean;
  activeFilters?: ActiveFilter[];
}

const FilterPanel: React.FC<FilterPanelProps> = ({
  fields,
  onFilterChange,
  onClear,
  initialFilters = {},
  showSearch = true,
  searchPlaceholder = 'Search...',
  onSearch,
  collapsible = true,
  defaultExpanded = false,
  activeFilters = [],
}) => {
  const [expanded, setExpanded] = useState(defaultExpanded);
  const [filters, setFilters] = useState<Record<string, any>>(initialFilters);
  const [searchTerm, setSearchTerm] = useState('');

  const handleFilterChange = (fieldName: string, value: any) => {
    const newFilters = { ...filters, [fieldName]: value };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  const handleClear = () => {
    setFilters({});
    setSearchTerm('');
    onClear();
  };

  const handleSearch = (value: string) => {
    setSearchTerm(value);
    onSearch?.(value);
  };

  const removeActiveFilter = (fieldName: string) => {
    const newFilters = { ...filters };
    delete newFilters[fieldName];
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  const renderFilterField = (field: FilterField) => {
    const value = filters[field.name] || field.defaultValue || '';

    switch (field.type) {
      case 'text':
      case 'number':
        return (
          <TextField
            key={field.name}
            label={field.label}
            type={field.type}
            value={value}
            onChange={(e) => handleFilterChange(field.name, e.target.value)}
            placeholder={field.placeholder}
            size="small"
            fullWidth
          />
        );

      case 'select':
        return (
          <TextField
            key={field.name}
            select
            label={field.label}
            value={value}
            onChange={(e) => handleFilterChange(field.name, e.target.value)}
            size="small"
            fullWidth
            SelectProps={{
              native: true,
            }}
          >
            <option value="">All</option>
            {field.options?.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </TextField>
        );

      case 'date':
        return (
          <TextField
            key={field.name}
            label={field.label}
            type="date"
            value={value}
            onChange={(e) => handleFilterChange(field.name, e.target.value)}
            size="small"
            fullWidth
            InputLabelProps={{
              shrink: true,
            }}
          />
        );

      default:
        return null;
    }
  };

  const hasActiveFilters = activeFilters.length > 0 || Object.keys(filters).some(key => filters[key]);

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent sx={{ pb: 2 }}>
        {/* Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <FilterIcon color="primary" />
            <Typography variant="h6">Filters</Typography>
            {hasActiveFilters && (
              <Chip
                label={`${activeFilters.length || Object.keys(filters).length} active`}
                size="small"
                color="primary"
                variant="outlined"
              />
            )}
          </Box>
          
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {hasActiveFilters && (
              <Button
                size="small"
                startIcon={<ClearIcon />}
                onClick={handleClear}
                color="secondary"
              >
                Clear All
              </Button>
            )}
            {collapsible && (
              <IconButton
                onClick={() => setExpanded(!expanded)}
                size="small"
              >
                {expanded ? <CollapseIcon /> : <ExpandIcon />}
              </IconButton>
            )}
          </Box>
        </Box>

        {/* Search Bar */}
        {showSearch && (
          <Box sx={{ mb: 2 }}>
            <TextField
              fullWidth
              placeholder={searchPlaceholder}
              value={searchTerm}
              onChange={(e) => handleSearch(e.target.value)}
              size="small"
              InputProps={{
                startAdornment: <SearchIcon sx={{ mr: 1, color: 'text.secondary' }} />,
              }}
            />
          </Box>
        )}

        {/* Active Filters */}
        {activeFilters.length > 0 && (
          <Box sx={{ mb: 2 }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Active Filters:
            </Typography>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              {activeFilters.map((filter) => (
                <Chip
                  key={filter.field}
                  label={`${filter.label}: ${filter.displayValue}`}
                  onDelete={() => removeActiveFilter(filter.field)}
                  size="small"
                  variant="outlined"
                  color="primary"
                />
              ))}
            </Stack>
          </Box>
        )}

        {/* Filter Fields */}
        <Collapse in={!collapsible || expanded}>
          {fields.length > 0 && (
            <>
              <Divider sx={{ mb: 2 }} />
              <Grid container spacing={2}>
                {fields.map((field) => (
                  <Grid item xs={12} sm={6} md={4} key={field.name}>
                    {renderFilterField(field)}
                  </Grid>
                ))}
              </Grid>
            </>
          )}
        </Collapse>
      </CardContent>
    </Card>
  );
};

export default FilterPanel;
