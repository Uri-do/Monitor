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

    const commonTextFieldProps = {
      size: "small" as const,
      fullWidth: true,
      sx: {
        '& .MuiOutlinedInput-root': {
          borderRadius: 1,
          backgroundColor: 'rgba(102, 126, 234, 0.04)',
          '&:hover': {
            backgroundColor: 'rgba(102, 126, 234, 0.08)',
          },
          '&.Mui-focused': {
            backgroundColor: 'white',
            boxShadow: '0 0 0 2px rgba(102, 126, 234, 0.2)',
          },
        },
        '& .MuiInputLabel-root': {
          fontWeight: 500,
          '&.Mui-focused': {
            color: 'primary.main',
          },
        },
      },
    };

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
            {...commonTextFieldProps}
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
            SelectProps={{
              native: true,
            }}
            {...commonTextFieldProps}
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
            InputLabelProps={{
              shrink: true,
            }}
            {...commonTextFieldProps}
          />
        );

      default:
        return null;
    }
  };

  const hasActiveFilters = activeFilters.length > 0 || Object.keys(filters).some(key => filters[key]);

  return (
    <Card
      sx={{
        mb: 3,
        borderRadius: 1,
        boxShadow: '0px 2px 8px rgba(0, 0, 0, 0.06)',
        border: '1px solid',
        borderColor: 'divider',
      }}
    >
      <CardContent sx={{ pb: 2 }}>
        {/* Header */}
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            mb: 2,
            p: 2,
            borderRadius: 1,
            background: 'linear-gradient(135deg, rgba(102, 126, 234, 0.08) 0%, rgba(118, 75, 162, 0.08) 100%)',
            border: '1px solid',
            borderColor: 'primary.light',
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <FilterIcon sx={{ color: 'primary.main' }} />
            <Typography variant="h6" sx={{ fontWeight: 600, color: 'primary.main' }}>
              Filters
            </Typography>
            {hasActiveFilters && (
              <Chip
                label={`${activeFilters.length || Object.keys(filters).length} active`}
                size="small"
                sx={{
                  backgroundColor: 'primary.main',
                  color: 'white',
                  fontWeight: 600,
                }}
              />
            )}
          </Box>

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {hasActiveFilters && (
              <Button
                size="small"
                startIcon={<ClearIcon />}
                onClick={handleClear}
                variant="outlined"
                sx={{
                  borderColor: 'primary.light',
                  color: 'primary.main',
                  '&:hover': {
                    backgroundColor: 'primary.light',
                    color: 'white',
                  },
                }}
              >
                Clear All
              </Button>
            )}
            {collapsible && (
              <IconButton
                onClick={() => setExpanded(!expanded)}
                size="small"
                sx={{
                  color: 'primary.main',
                  backgroundColor: 'rgba(102, 126, 234, 0.08)',
                  '&:hover': {
                    backgroundColor: 'rgba(102, 126, 234, 0.16)',
                  },
                }}
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
              sx={{
                '& .MuiOutlinedInput-root': {
                  borderRadius: 1,
                  backgroundColor: 'rgba(102, 126, 234, 0.04)',
                  '&:hover': {
                    backgroundColor: 'rgba(102, 126, 234, 0.08)',
                  },
                  '&.Mui-focused': {
                    backgroundColor: 'white',
                    boxShadow: '0 0 0 2px rgba(102, 126, 234, 0.2)',
                  },
                },
              }}
              InputProps={{
                startAdornment: <SearchIcon sx={{ mr: 1, color: 'primary.main' }} />,
              }}
            />
          </Box>
        )}

        {/* Active Filters */}
        {activeFilters.length > 0 && (
          <Box sx={{ mb: 2 }}>
            <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 600, color: 'text.primary' }}>
              Active Filters:
            </Typography>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              {activeFilters.map((filter) => (
                <Chip
                  key={filter.field}
                  label={`${filter.label}: ${filter.displayValue}`}
                  onDelete={() => removeActiveFilter(filter.field)}
                  size="small"
                  sx={{
                    backgroundColor: 'primary.main',
                    color: 'white',
                    fontWeight: 500,
                    '& .MuiChip-deleteIcon': {
                      color: 'rgba(255, 255, 255, 0.8)',
                      '&:hover': {
                        color: 'white',
                      },
                    },
                  }}
                />
              ))}
            </Stack>
          </Box>
        )}

        {/* Filter Fields */}
        <Collapse in={!collapsible || expanded}>
          {fields.length > 0 && (
            <>
              <Divider sx={{ mb: 3, borderColor: 'primary.light' }} />
              <Box
                sx={{
                  p: 2,
                  borderRadius: 1,
                  backgroundColor: 'rgba(102, 126, 234, 0.02)',
                  border: '1px solid',
                  borderColor: 'rgba(102, 126, 234, 0.1)',
                }}
              >
                <Grid container spacing={3}>
                  {fields.map((field) => (
                    <Grid item xs={12} sm={6} md={4} key={field.name}>
                      {renderFilterField(field)}
                    </Grid>
                  ))}
                </Grid>
              </Box>
            </>
          )}
        </Collapse>
      </CardContent>
    </Card>
  );
};

export default FilterPanel;
