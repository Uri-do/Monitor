import React, { useState } from 'react';
import { Box, Collapse, Grid, IconButton, Stack, Typography, InputAdornment, TextField } from '@mui/material';
import {
  ExpandMore as ExpandIcon,
  Search as SearchIcon,
  Clear as ClearIcon,
  FilterList as FilterIcon,
} from '@mui/icons-material';
import Card from './Card';
import InputField from './InputField';
import Select from './Select';
import Button from './Button';

interface FilterField {
  name: string;
  label: string;
  type: 'text' | 'select' | 'number' | 'date';
  placeholder?: string;
  options?: Array<{ value: string | number; label: string }>;
  defaultValue?: any;
}

interface FilterPanelProps {
  fields: FilterField[];
  onFilterChange: (filters: Record<string, any>) => void;
  onClear: () => void;
  onSearch?: (searchTerm: string) => void;
  searchPlaceholder?: string;
  defaultExpanded?: boolean;
  showFilterCount?: boolean;
}

export const FilterPanel: React.FC<FilterPanelProps> = ({
  fields,
  onFilterChange,
  onClear,
  onSearch,
  searchPlaceholder = 'Search...',
  defaultExpanded = false,
  showFilterCount = true,
}) => {
  const [expanded, setExpanded] = useState(defaultExpanded);
  const [filters, setFilters] = useState<Record<string, any>>({});
  const [searchTerm, setSearchTerm] = useState('');

  const handleFilterChange = (name: string, value: any) => {
    const newFilters = { ...filters, [name]: value };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    if (onSearch) {
      onSearch(value);
    }
  };

  const handleClear = () => {
    setFilters({});
    setSearchTerm('');
    onClear();
  };

  const activeFilterCount =
    Object.values(filters).filter(value => value !== '' && value !== null && value !== undefined)
      .length + (searchTerm ? 1 : 0);

  return (
    <Card sx={{ mb: 3 }}>
      <Box sx={{ p: 2 }}>
        {/* Header */}
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          sx={{ mb: expanded ? 2 : 0 }}
        >
          <Stack direction="row" alignItems="center" spacing={1}>
            <FilterIcon sx={{ color: 'primary.main' }} />
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              Filters
            </Typography>
            {showFilterCount && activeFilterCount > 0 && (
              <Box
                sx={{
                  backgroundColor: 'primary.main',
                  color: 'white',
                  borderRadius: '50%',
                  width: 24,
                  height: 24,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '0.75rem',
                  fontWeight: 600,
                }}
              >
                {activeFilterCount}
              </Box>
            )}
          </Stack>

          <Stack direction="row" alignItems="center" spacing={1}>
            {activeFilterCount > 0 && (
              <Button
                size="small"
                variant="outlined"
                gradient="error"
                startIcon={<ClearIcon />}
                onClick={handleClear}
              >
                Clear All
              </Button>
            )}
            <IconButton
              onClick={() => setExpanded(!expanded)}
              sx={{
                transform: expanded ? 'rotate(180deg)' : 'rotate(0deg)',
                transition: 'transform 0.3s ease-in-out',
              }}
            >
              <ExpandIcon />
            </IconButton>
          </Stack>
        </Stack>

        {/* Search Bar - Always Visible */}
        {onSearch && (
          <Box sx={{ mb: expanded ? 2 : 0 }}>
            <TextField
              fullWidth
              placeholder={searchPlaceholder}
              value={searchTerm}
              onChange={e => handleSearchChange(e.target.value)}
              variant="outlined"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon sx={{ color: 'primary.main' }} />
                  </InputAdornment>
                ),
                endAdornment: searchTerm && (
                  <InputAdornment position="end">
                    <IconButton size="small" onClick={() => handleSearchChange('')}>
                      <ClearIcon />
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />
          </Box>
        )}

        {/* Filter Fields */}
        <Collapse in={expanded}>
          <Grid container spacing={2}>
            {fields.map(field => (
              <Grid item xs={12} sm={6} md={4} key={field.name}>
                {field.type === 'select' ? (
                  <Select
                    fullWidth
                    label={field.label}
                    value={filters[field.name] || field.defaultValue || ''}
                    onChange={e => handleFilterChange(field.name, e.target.value)}
                    options={field.options || []}
                  />
                ) : (
                  <InputField
                    fullWidth
                    label={field.label}
                    type={field.type}
                    placeholder={field.placeholder}
                    value={filters[field.name] || field.defaultValue || ''}
                    onChange={e => handleFilterChange(field.name, e.target.value)}
                  />
                )}
              </Grid>
            ))}
          </Grid>
        </Collapse>
      </Box>
    </Card>
  );
};

export default FilterPanel;
