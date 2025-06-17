import React from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Button,
  Chip,
  Stack,
} from '@mui/material';
import {
  FilterList as FilterIcon,
  Clear as ClearIcon,
  Search as SearchIcon,
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { ExecutionHistoryFilters } from '../types';

interface ExecutionFiltersProps {
  filters: ExecutionHistoryFilters;
  onFiltersChange: (filters: ExecutionHistoryFilters) => void;
  onApplyFilters: () => void;
  onClearFilters: () => void;
  loading?: boolean;
}

export const ExecutionFilters: React.FC<ExecutionFiltersProps> = ({
  filters,
  onFiltersChange,
  onApplyFilters,
  onClearFilters,
  loading = false,
}) => {
  const handleFilterChange = (field: keyof ExecutionHistoryFilters, value: any) => {
    onFiltersChange({
      ...filters,
      [field]: value,
    });
  };

  const getActiveFiltersCount = () => {
    let count = 0;
    if (filters.search) count++;
    if (filters.startDate) count++;
    if (filters.endDate) count++;
    if (filters.status !== 'all') count++;
    if (filters.performanceCategory !== 'all') count++;
    if (filters.executedBy) count++;
    return count;
  };

  const activeFiltersCount = getActiveFiltersCount();

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
          <Stack direction="row" spacing={1}>
            <Button
              variant="outlined"
              startIcon={<ClearIcon />}
              onClick={onClearFilters}
              disabled={activeFiltersCount === 0 || loading}
            >
              Clear
            </Button>
            <Button
              variant="contained"
              startIcon={<SearchIcon />}
              onClick={onApplyFilters}
              disabled={loading}
            >
              Apply Filters
            </Button>
          </Stack>
        </Box>

        <Grid container spacing={2}>
          {/* Search */}
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              label="Search KPI"
              placeholder="Search by KPI name..."
              value={filters.search || ''}
              onChange={(e) => handleFilterChange('search', e.target.value)}
              size="small"
            />
          </Grid>

          {/* Status Filter */}
          <Grid item xs={12} md={2}>
            <FormControl fullWidth size="small">
              <InputLabel>Status</InputLabel>
              <Select
                value={filters.status || 'all'}
                label="Status"
                onChange={(e) => handleFilterChange('status', e.target.value)}
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value="success">Success</MenuItem>
                <MenuItem value="failed">Failed</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Performance Category */}
          <Grid item xs={12} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Performance</InputLabel>
              <Select
                value={filters.performanceCategory || 'all'}
                label="Performance"
                onChange={(e) => handleFilterChange('performanceCategory', e.target.value)}
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value="fast">Fast</MenuItem>
                <MenuItem value="normal">Normal</MenuItem>
                <MenuItem value="slow">Slow</MenuItem>
                <MenuItem value="very slow">Very Slow</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Executed By */}
          <Grid item xs={12} md={3}>
            <TextField
              fullWidth
              label="Executed By"
              placeholder="User or system..."
              value={filters.executedBy || ''}
              onChange={(e) => handleFilterChange('executedBy', e.target.value)}
              size="small"
            />
          </Grid>

          {/* Date Range */}
          <Grid item xs={12} md={3}>
            <DatePicker
              label="Start Date"
              value={filters.startDate}
              onChange={(date) => handleFilterChange('startDate', date)}
              slotProps={{
                textField: {
                  fullWidth: true,
                  size: 'small',
                },
              }}
            />
          </Grid>

          <Grid item xs={12} md={3}>
            <DatePicker
              label="End Date"
              value={filters.endDate}
              onChange={(date) => handleFilterChange('endDate', date)}
              slotProps={{
                textField: {
                  fullWidth: true,
                  size: 'small',
                },
              }}
            />
          </Grid>

          {/* Quick Date Filters */}
          <Grid item xs={12} md={6}>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              Quick Filters:
            </Typography>
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Chip
                label="Today"
                size="small"
                variant="outlined"
                onClick={() => {
                  const today = new Date();
                  today.setHours(0, 0, 0, 0);
                  handleFilterChange('startDate', today);
                  handleFilterChange('endDate', new Date());
                }}
              />
              <Chip
                label="Yesterday"
                size="small"
                variant="outlined"
                onClick={() => {
                  const yesterday = new Date();
                  yesterday.setDate(yesterday.getDate() - 1);
                  yesterday.setHours(0, 0, 0, 0);
                  const endOfYesterday = new Date(yesterday);
                  endOfYesterday.setHours(23, 59, 59, 999);
                  handleFilterChange('startDate', yesterday);
                  handleFilterChange('endDate', endOfYesterday);
                }}
              />
              <Chip
                label="Last 7 Days"
                size="small"
                variant="outlined"
                onClick={() => {
                  const sevenDaysAgo = new Date();
                  sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);
                  sevenDaysAgo.setHours(0, 0, 0, 0);
                  handleFilterChange('startDate', sevenDaysAgo);
                  handleFilterChange('endDate', new Date());
                }}
              />
              <Chip
                label="Last 30 Days"
                size="small"
                variant="outlined"
                onClick={() => {
                  const thirtyDaysAgo = new Date();
                  thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);
                  thirtyDaysAgo.setHours(0, 0, 0, 0);
                  handleFilterChange('startDate', thirtyDaysAgo);
                  handleFilterChange('endDate', new Date());
                }}
              />
            </Stack>
          </Grid>
        </Grid>
      </CardContent>
    </Card>
  );
};

export default ExecutionFilters;
