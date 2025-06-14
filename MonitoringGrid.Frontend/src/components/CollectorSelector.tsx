import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Grid,
  Chip,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  AlertTitle,
  SelectChangeEvent,
} from '@mui/material';
import { Clock, Activity } from 'lucide-react';
import { useActiveCollectors, useCollectorItemNames } from '../hooks/useMonitorStatistics';
import StatisticsBrowserButton from './StatisticsBrowserButton';
import { GenericSelector, GenericSelectorOption } from './UI/GenericSelector';

// Transform collector data to match GenericSelectorOption interface
interface CollectorOption extends GenericSelectorOption {
  collectorID: number;
  collectorName: string;
  collectorDescription?: string;
  isActive: boolean;
  lastRunTime?: string;
  status?: string;
}

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
  const [internalCollectorId, setInternalCollectorId] = useState<number | undefined>(
    selectedCollectorId
  );

  const {
    data: collectors,
    isLoading: collectorsLoading,
    error: collectorsError,
  } = useActiveCollectors();

  const {
    data: itemNames,
    isLoading: itemNamesLoading,
    error: itemNamesError,
  } = useCollectorItemNames(internalCollectorId || 0);

  // Transform collectors to match GenericSelectorOption interface
  const collectorOptions: CollectorOption[] = (collectors || []).map(collector => ({
    id: collector.collectorID,
    name: collector.displayName || collector.collectorCode || `Collector ${collector.collectorID}`,
    description: collector.collectorDesc,
    isEnabled: collector.isActiveStatus,
    isActive: collector.isActiveStatus,
    displayText: `${collector.displayName} - ${collector.statusDisplay}`,
    metadata: {
      type: 'collector',
      status: collector.statusDisplay,
      lastRunTime: collector.lastRunDisplay,
      frequency: collector.frequencyDisplay,
    },
    // Original properties for backward compatibility
    collectorID: collector.collectorID,
    collectorName: collector.displayName,
    collectorDescription: collector.collectorDesc,
    lastRunTime: collector.lastRunDisplay,
    status: collector.statusDisplay,
  }));

  // Sync internal state with props
  useEffect(() => {
    if (selectedCollectorId !== internalCollectorId) {
      setInternalCollectorId(selectedCollectorId);
    }
  }, [selectedCollectorId, internalCollectorId]);

  // Validate selected item name when items are loaded
  useEffect(() => {
    if (selectedItemName && itemNames && itemNames.length > 0) {
      // If the selected item is not in the available items, clear it
      if (!itemNames.includes(selectedItemName)) {
        onItemNameChange('');
      }
    }
  }, [selectedItemName, itemNames, onItemNameChange]);

  // Handle collector selection for GenericSelector
  const handleCollectorChange = (collectorId: string | number | undefined) => {
    const numericId = collectorId ? Number(collectorId) : undefined;
    setInternalCollectorId(numericId);
    onCollectorChange(numericId);

    // Clear item name when collector changes
    if (numericId !== selectedCollectorId) {
      onItemNameChange('');
    }
  };

  // Handle item name selection
  const handleItemNameChange = (event: SelectChangeEvent<string>) => {
    const itemName = event.target.value;
    onItemNameChange(itemName);
  };

  // Get selected collector for display
  const selectedCollector = collectors?.find(c => c.collectorID === internalCollectorId);

  // Custom render for collector info card
  const renderCollectorInfo = (collector: CollectorOption) => (
    <Box>
      <Typography variant="subtitle2" fontWeight="medium" gutterBottom>
        {collector.name}
      </Typography>
      {collector.description && (
        <Typography variant="body2" color="text.secondary" gutterBottom>
          {collector.description}
        </Typography>
      )}
      <Box sx={{ display: 'flex', gap: 2, mt: 1 }}>
        <Typography variant="caption" color="text.secondary">
          <strong>Status:</strong> {collector.metadata?.status}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          <strong>Frequency:</strong> {collector.metadata?.frequency}
        </Typography>
      </Box>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
        <strong>Last Run:</strong> {collector.metadata?.lastRunTime}
      </Typography>
    </Box>
  );

  if (collectorsError) {
    return (
      <Alert severity="error" className={className}>
        <AlertTitle>Error</AlertTitle>
        Failed to load collectors: {collectorsError.message}
      </Alert>
    );
  }

  return (
    <Box className={className}>
      {/* Title and Subtitle */}
      {(title || subtitle) && (
        <Box sx={{ mb: 2 }}>
          {title && (
            <Typography variant="h6" gutterBottom>
              {title}
            </Typography>
          )}
          {subtitle && (
            <Typography variant="body2" color="text.secondary">
              {subtitle}
            </Typography>
          )}
        </Box>
      )}

      <Grid container spacing={2}>
        {/* Collector Selection using GenericSelector */}
        <Grid item xs={12} md={showStatisticsButton ? 10 : 12}>
          <GenericSelector
            data={collectorOptions}
            loading={collectorsLoading}
            error={collectorsError}
            selectedId={internalCollectorId}
            onSelectionChange={handleCollectorChange}
            label="Data Collector"
            required={required}
            disabled={disabled}
            variant={variant}
            showRefreshButton={showRefreshButton}
            showInfoCard={showCollectorInfo}
            queryKey={['monitor-statistics', 'collectors', 'active']}
            renderInfoCard={renderCollectorInfo}
            emptyMessage="No collectors found."
          />
        </Grid>

        {/* Statistics Browser Button */}
        {showStatisticsButton && (
          <Grid item xs={12} md={2}>
            <Box sx={{ display: 'flex', alignItems: 'flex-end', height: '100%' }}>
              <StatisticsBrowserButton
                collectorId={internalCollectorId}
                itemName={selectedItemName}
                variant="icon"
                size="medium"
                disabled={disabled}
                tooltip="Browse All Statistics"
              />
            </Box>
          </Grid>
        )}
      </Grid>

      {/* Collector Details */}
      {selectedCollector && (
        <Box
          sx={{
            p: 2,
            bgcolor: theme => (theme.palette.mode === 'dark' ? 'grey.800' : 'grey.50'),
            borderRadius: 1,
            mb: 2,
          }}
        >
          <Grid container spacing={2}>
            <Grid item xs={6}>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Activity style={{ width: 12, height: 12, marginRight: 4 }} />
                <Typography variant="caption" color="text.secondary">
                  Status:
                </Typography>
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
                <Typography variant="caption" color="text.secondary">
                  Last Run:
                </Typography>
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
              {itemNames?.map(itemName => (
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

          {selectedItemName &&
            itemNames &&
            !itemNames.includes(selectedItemName) &&
            !itemNamesLoading && (
              <Alert severity="warning" sx={{ mt: 1 }}>
                The previously selected item "{selectedItemName}" is not available in the current
                data. You may need to select a different item or check if the data source has
                changed.
              </Alert>
            )}
        </FormControl>
      )}
    </Box>
  );
};
