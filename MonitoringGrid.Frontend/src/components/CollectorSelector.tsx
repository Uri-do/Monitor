import React, { useState, useEffect } from 'react';
import {
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  AlertTitle,
  Box,
  Typography,
  CircularProgress,
  Grid,
  Chip,
  SelectChangeEvent,
  Card,
  CardContent,
  Divider,
  Tooltip,
  IconButton
} from '@mui/material';
import {
  Database,
  Clock,
  Activity,
  Info,
  RefreshCw as Refresh
} from 'lucide-react';
import { useActiveCollectors, useCollectorItemNames } from '../hooks/useMonitorStatistics';
import { useQueryClient } from '@tanstack/react-query';
import StatisticsBrowserButton from './StatisticsBrowserButton';

interface CollectorSelectorProps {
  selectedCollectorId?: number;
  selectedItemName?: string;
  onCollectorChange: (collectorId: number | undefined) => void;
  onItemNameChange: (itemName: string) => void;
  disabled?: boolean;
  className?: string;
  required?: boolean;
  variant?: 'standard' | 'detailed' | 'compact';
  showStatisticsButton?: boolean;
  showRefreshButton?: boolean;
  showCollectorInfo?: boolean;
  title?: string;
  subtitle?: string;
}

export const CollectorSelector: React.FC<CollectorSelectorProps> = ({
  selectedCollectorId,
  selectedItemName,
  onCollectorChange,
  onItemNameChange,
  disabled = false,
  className = '',
  required = false,
  variant = 'standard',
  showStatisticsButton = true,
  showRefreshButton = false,
  showCollectorInfo = false,
  title,
  subtitle,
}) => {
  const [internalCollectorId, setInternalCollectorId] = useState<number | undefined>(selectedCollectorId);
  const queryClient = useQueryClient();
  
  const {
    data: collectors,
    isLoading: collectorsLoading,
    error: collectorsError,
    refetch: refetchCollectors,
  } = useActiveCollectors();

  const {
    data: itemNames,
    isLoading: itemNamesLoading,
    error: itemNamesError,
    refetch: refetchItemNames,
  } = useCollectorItemNames(internalCollectorId || 0);

  // Update internal state when props change
  useEffect(() => {
    if (selectedCollectorId !== internalCollectorId) {
      // Invalidate the old collector's item names cache
      if (internalCollectorId) {
        queryClient.invalidateQueries({
          queryKey: ['monitor-statistics', 'collectors', internalCollectorId, 'items']
        });
      }

      setInternalCollectorId(selectedCollectorId);

      // If the collector changed and we have a selected item name,
      // only clear it if it's not available in the new collector's items
      // We'll let the item validation logic handle this after items are loaded

      // Invalidate the new collector's item names cache to force fresh data
      if (selectedCollectorId) {
        queryClient.invalidateQueries({
          queryKey: ['monitor-statistics', 'collectors', selectedCollectorId, 'items']
        });
      }
    }
  }, [selectedCollectorId, internalCollectorId, selectedItemName, onItemNameChange, queryClient]);

  // Validate selected item name when items are loaded
  React.useEffect(() => {
    if (selectedItemName && itemNames && itemNames.length > 0) {
      // If the selected item is not in the available items, clear it
      if (!itemNames.includes(selectedItemName)) {
        onItemNameChange('');
      }
    }
  }, [selectedItemName, itemNames, onItemNameChange]);

  const handleCollectorChange = (event: SelectChangeEvent<string>) => {
    const value = event.target.value;

    // Invalidate current collector's cache before changing
    if (internalCollectorId) {
      queryClient.invalidateQueries({
        queryKey: ['monitor-statistics', 'collectors', internalCollectorId, 'items']
      });
    }

    if (value === '') {
      setInternalCollectorId(undefined);
      onCollectorChange(undefined);
    } else {
      const collectorId = parseInt(value, 10);
      if (!isNaN(collectorId)) {
        setInternalCollectorId(collectorId);
        onCollectorChange(collectorId);

        // Invalidate new collector's cache to force fresh data
        queryClient.invalidateQueries({
          queryKey: ['monitor-statistics', 'collectors', collectorId, 'items']
        });
      }
    }
    // Only clear item name when collector changes if user manually changed it
    // (not during initial form loading with existing data)
    if (selectedItemName) {
      onItemNameChange('');
    }
  };

  const handleItemNameChange = (event: SelectChangeEvent<string>) => {
    const value = event.target.value;
    onItemNameChange(value);
  };

  const selectedCollector = collectors?.find(c => c.collectorID === internalCollectorId);



  if (collectorsError) {
    return (
      <Alert severity="error" className={className}>
        <AlertTitle>Error</AlertTitle>
        Failed to load collectors: {collectorsError.message}
      </Alert>
    );
  }

  return (
    <Box className={className} sx={{ mb: 2 }}>
      {/* Collector Selection */}
      <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
        <FormControl fullWidth>
          <InputLabel id="collector-select-label">
            Data Collector {required && <span style={{ color: 'red' }}>*</span>}
          </InputLabel>
          <Select
            labelId="collector-select-label"
            value={internalCollectorId ? internalCollectorId.toString() : ''}
            onChange={handleCollectorChange}
            disabled={disabled || collectorsLoading}
            label={`Data Collector ${required ? '*' : ''}`}
          >
            {!required && (
              <MenuItem value="">
                <Typography color="text.secondary">No collector (optional)</Typography>
              </MenuItem>
            )}
            {collectors?.map((collector) => (
              <MenuItem key={collector.collectorID} value={collector.collectorID.toString()}>
                <Box>
                  <Typography variant="body2" fontWeight="medium">
                    {collector.displayName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    ID: {collector.collectorID} • {collector.frequencyDisplay}
                  </Typography>
                </Box>
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        {/* Statistics Browser Button */}
        <Box sx={{ display: 'flex', alignItems: 'flex-end' }}>
          <StatisticsBrowserButton
            collectorId={internalCollectorId}
            itemName={selectedItemName}
            variant="icon"
            size="medium"
            disabled={disabled}
            tooltip="Browse All Statistics"
          />
        </Box>
      </Box>

      {/* Collector Details */}
      {selectedCollector && (
        <Box sx={{
          p: 2,
          bgcolor: (theme) => theme.palette.mode === 'dark' ? 'grey.800' : 'grey.50',
          borderRadius: 1,
          mb: 2
        }}>
          <Grid container spacing={2}>
            <Grid item xs={6}>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Activity style={{ width: 12, height: 12, marginRight: 4 }} />
                <Typography variant="caption" color="text.secondary">Status:</Typography>
                <Chip
                  label={selectedCollector.statusDisplay}
                  color={selectedCollector.isActiveStatus ? 'success' : 'error'}
                  size="small"
                  sx={{ ml: 1 }}
                />
              </Box>
            </Grid>
            <Grid item xs={6}>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Clock style={{ width: 12, height: 12, marginRight: 4 }} />
                <Typography variant="caption" color="text.secondary">Last Run:</Typography>
                <Typography variant="caption" fontWeight="medium" sx={{ ml: 1 }}>
                  {selectedCollector.lastRunDisplay}
                </Typography>
              </Box>
            </Grid>
          </Grid>
          {selectedCollector.collectorDesc && (
            <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
              {selectedCollector.collectorDesc}
            </Typography>
          )}
        </Box>
      )}

      {/* Item Name Selection */}
      {internalCollectorId && (
        <FormControl fullWidth>
          <InputLabel id="item-select-label">Item Name</InputLabel>

          {itemNamesError ? (
            <Alert severity="error" sx={{ mt: 1 }}>
              <AlertTitle>Error</AlertTitle>
              Failed to load item names: {itemNamesError.message}
            </Alert>
          ) : (
            <Select
              labelId="item-select-label"
              value={selectedItemName || ''}
              onChange={handleItemNameChange}
              disabled={disabled || itemNamesLoading}
              label="Item Name"
            >
              {/* Show the selected item even if it's not in the current list (for edit mode) */}
              {selectedItemName && itemNames && !itemNames.includes(selectedItemName) && (
                <MenuItem key={selectedItemName} value={selectedItemName}>
                  <Box>
                    <Typography variant="body2">{selectedItemName}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      (Previously selected - may not be available)
                    </Typography>
                  </Box>
                </MenuItem>
              )}
              {itemNames?.map((itemName) => (
                <MenuItem key={itemName} value={itemName}>
                  {itemName}
                </MenuItem>
              ))}
            </Select>
          )}

          {itemNamesLoading && (
            <Alert severity="info" sx={{ mt: 1 }}>
              Loading item names...
            </Alert>
          )}

          {itemNames && itemNames.length === 0 && !itemNamesLoading && (
            <Alert severity="info" sx={{ mt: 1 }}>
              No item names found for this collector. The collector may not have any data yet.
            </Alert>
          )}

          {selectedItemName && itemNames && !itemNames.includes(selectedItemName) && !itemNamesLoading && (
            <Alert severity="warning" sx={{ mt: 1 }}>
              The previously selected item "{selectedItemName}" is not available in the current data.
              You may need to select a different item or check if the data source has changed.
            </Alert>
          )}
        </FormControl>
      )}

    </Box>
  );
};
