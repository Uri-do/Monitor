import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Chip,
  Stack,
  Button,
} from '@mui/material';
import {
  FilterList as FilterIcon,
  Clear as ClearIcon,
  Search as SearchIcon,
} from '@mui/icons-material';

export interface SchedulerFilters {
  search: string;
  status: 'all' | 'active' | 'inactive';
  scheduleType: 'all' | 'interval' | 'cron' | 'onetime';
  timezone: string;
}

interface SchedulerFiltersProps {
  filters: SchedulerFilters;
  onFiltersChange: (filters: SchedulerFilters) => void;
  onClearFilters: () => void;
  totalCount: number;
  filteredCount: number;
}

export const SchedulerFiltersComponent: React.FC<SchedulerFiltersProps> = ({
  filters,
  onFiltersChange,
  onClearFilters,
  totalCount,
  filteredCount,
}) => {
  const handleFilterChange = (field: keyof SchedulerFilters, value: string) => {
    onFiltersChange({
      ...filters,
      [field]: value,
    });
  };

  const getActiveFiltersCount = () => {
    let count = 0;
    if (filters.search) count++;
    if (filters.status !== 'all') count++;
    if (filters.scheduleType !== 'all') count++;
    if (filters.timezone) count++;
    return count;
  };

  const activeFiltersCount = getActiveFiltersCount();

  const timezones = [
    { label: 'All Timezones', value: '' },
    { label: 'UTC', value: 'UTC' },
    { label: 'Eastern Time', value: 'America/New_York' },
    { label: 'Central Time', value: 'America/Chicago' },
    { label: 'Mountain Time', value: 'America/Denver' },
    { label: 'Pacific Time', value: 'America/Los_Angeles' },
    { label: 'London', value: 'Europe/London' },
    { label: 'Paris', value: 'Europe/Paris' },
    { label: 'Tokyo', value: 'Asia/Tokyo' },
    { label: 'Sydney', value: 'Australia/Sydney' },
  ];

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Box display="flex" alignItems="center" gap={1}>
            <FilterIcon />
            <Typography variant="h6">Filters</Typography>
            {activeFiltersCount > 0 && (
              <Chip
                label={`${activeFiltersCount} active`}
                size="small"
                color="primary"
              />
            )}
          </Box>
          
          <Box display="flex" alignItems="center" gap={2}>
            <Typography variant="body2" color="text.secondary">
              Showing {filteredCount} of {totalCount} schedulers
            </Typography>
            {activeFiltersCount > 0 && (
              <Button
                variant="outlined"
                size="small"
                startIcon={<ClearIcon />}
                onClick={onClearFilters}
              >
                Clear Filters
              </Button>
            )}
          </Box>
        </Box>

        <Grid container spacing={2}>
          {/* Search */}
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              label="Search Schedulers"
              placeholder="Search by name or description..."
              value={filters.search}
              onChange={(e) => handleFilterChange('search', e.target.value)}
              size="small"
              InputProps={{
                startAdornment: <SearchIcon sx={{ mr: 1, color: 'action.active' }} />,
              }}
            />
          </Grid>

          {/* Status Filter */}
          <Grid item xs={12} md={2}>
            <FormControl fullWidth size="small">
              <InputLabel>Status</InputLabel>
              <Select
                value={filters.status}
                label="Status"
                onChange={(e) => handleFilterChange('status', e.target.value)}
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value="active">Active</MenuItem>
                <MenuItem value="inactive">Inactive</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Schedule Type Filter */}
          <Grid item xs={12} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Schedule Type</InputLabel>
              <Select
                value={filters.scheduleType}
                label="Schedule Type"
                onChange={(e) => handleFilterChange('scheduleType', e.target.value)}
              >
                <MenuItem value="all">All Types</MenuItem>
                <MenuItem value="interval">Interval</MenuItem>
                <MenuItem value="cron">Cron Expression</MenuItem>
                <MenuItem value="onetime">One Time</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Timezone Filter */}
          <Grid item xs={12} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Timezone</InputLabel>
              <Select
                value={filters.timezone}
                label="Timezone"
                onChange={(e) => handleFilterChange('timezone', e.target.value)}
              >
                {timezones.map((tz) => (
                  <MenuItem key={tz.value} value={tz.value}>
                    {tz.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
        </Grid>

        {/* Quick Filters */}
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            Quick Filters:
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip
              label="Active Only"
              size="small"
              variant={filters.status === 'active' ? 'filled' : 'outlined'}
              onClick={() => handleFilterChange('status', filters.status === 'active' ? 'all' : 'active')}
              color={filters.status === 'active' ? 'primary' : 'default'}
            />
            <Chip
              label="Inactive Only"
              size="small"
              variant={filters.status === 'inactive' ? 'filled' : 'outlined'}
              onClick={() => handleFilterChange('status', filters.status === 'inactive' ? 'all' : 'inactive')}
              color={filters.status === 'inactive' ? 'primary' : 'default'}
            />
            <Chip
              label="Cron Schedules"
              size="small"
              variant={filters.scheduleType === 'cron' ? 'filled' : 'outlined'}
              onClick={() => handleFilterChange('scheduleType', filters.scheduleType === 'cron' ? 'all' : 'cron')}
              color={filters.scheduleType === 'cron' ? 'primary' : 'default'}
            />
            <Chip
              label="Interval Schedules"
              size="small"
              variant={filters.scheduleType === 'interval' ? 'filled' : 'outlined'}
              onClick={() => handleFilterChange('scheduleType', filters.scheduleType === 'interval' ? 'all' : 'interval')}
              color={filters.scheduleType === 'interval' ? 'primary' : 'default'}
            />
            <Chip
              label="UTC Timezone"
              size="small"
              variant={filters.timezone === 'UTC' ? 'filled' : 'outlined'}
              onClick={() => handleFilterChange('timezone', filters.timezone === 'UTC' ? '' : 'UTC')}
              color={filters.timezone === 'UTC' ? 'primary' : 'default'}
            />
          </Stack>
        </Box>
      </CardContent>
    </Card>
  );
};

export default SchedulerFiltersComponent;
