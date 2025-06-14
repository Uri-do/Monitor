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
  Grid,
  Chip,
  SelectChangeEvent,
  Card,
  CardContent,
  Tooltip,
  IconButton,
  Button,
  CircularProgress,
} from '@mui/material';
import { Add, Refresh } from '@mui/icons-material';
import { useQueryClient } from '@tanstack/react-query';

// Generic selector component that can be used for any entity type
export interface GenericSelectorOption {
  id: string | number;
  name: string;
  description?: string;
  isEnabled?: boolean;
  isActive?: boolean;
  displayText?: string;
  metadata?: Record<string, any>;
}

export interface GenericSelectorProps<T extends GenericSelectorOption> {
  // Data props
  data?: T[];
  loading?: boolean;
  error?: Error | null;

  // Selection props
  selectedId?: string | number;
  onSelectionChange: (id: string | number | undefined) => void;

  // Configuration props
  label: string;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  variant?: 'standard' | 'detailed' | 'compact';

  // Display props
  title?: string;
  subtitle?: string;
  emptyMessage?: string;

  // Action props
  showCreateButton?: boolean;
  showRefreshButton?: boolean;
  showInfoCard?: boolean;
  onCreateClick?: () => void;
  onRefresh?: () => void;

  // Query props
  queryKey?: string[];

  // Render customization
  renderOption?: (item: T) => React.ReactNode;
  renderInfoCard?: (item: T) => React.ReactNode;
  getOptionValue?: (item: T) => string | number;
  getOptionLabel?: (item: T) => string;
  getOptionDescription?: (item: T) => string | undefined;
  filterEnabled?: boolean;

  // Styling
  className?: string;
  maxHeight?: number;
}

export function GenericSelector<T extends GenericSelectorOption>({
  data = [],
  loading = false,
  error = null,
  selectedId,
  onSelectionChange,
  label,
  placeholder,
  required = false,
  disabled = false,
  variant = 'standard',
  title,
  subtitle,
  emptyMessage,
  showCreateButton = false,
  showRefreshButton = false,
  showInfoCard = false,
  onCreateClick,
  onRefresh,
  queryKey,
  renderOption,
  renderInfoCard,
  getOptionValue = item => item.id,
  getOptionLabel = item => item.name,
  getOptionDescription = item => item.description,
  filterEnabled = true,
  className = '',
  maxHeight = 400,
}: GenericSelectorProps<T>) {
  const queryClient = useQueryClient();

  // Filter enabled items if filterEnabled is true
  const filteredData = filterEnabled
    ? data.filter(item => item.isEnabled !== false && item.isActive !== false)
    : data;

  const selectedItem = data.find(item => getOptionValue(item) === selectedId);

  const handleSelectionChange = (event: SelectChangeEvent<string>) => {
    const value = event.target.value;
    if (value === '') {
      onSelectionChange(undefined);
    } else {
      // Try to parse as number first, fallback to string
      const numericValue = Number(value);
      const finalValue =
        !isNaN(numericValue) && numericValue.toString() === value ? numericValue : value;
      onSelectionChange(finalValue);
    }
  };

  const handleRefresh = () => {
    if (onRefresh) {
      onRefresh();
    } else if (queryKey && queryClient) {
      queryClient.invalidateQueries({ queryKey });
    }
  };

  const defaultRenderOption = (item: T): React.ReactNode => {
    if (variant === 'detailed') {
      return (
        <Box sx={{ py: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
            <Typography variant="body2" fontWeight="medium">
              {getOptionLabel(item)}
            </Typography>
            {item.metadata?.type && (
              <Chip label={item.metadata.type} size="small" variant="outlined" color="primary" />
            )}
            {item.isEnabled === false && (
              <Chip label="Disabled" size="small" color="error" variant="outlined" />
            )}
          </Box>
          {getOptionDescription(item) && (
            <Typography variant="caption" color="text.secondary" sx={{ ml: 0, display: 'block' }}>
              {getOptionDescription(item)}
            </Typography>
          )}
          {item.displayText && item.displayText !== getOptionLabel(item) && (
            <Typography variant="caption" color="text.secondary" sx={{ ml: 0, display: 'block' }}>
              {item.displayText}
            </Typography>
          )}
        </Box>
      );
    } else {
      return (
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="body2" fontWeight="medium">
              {getOptionLabel(item)}
            </Typography>
            {item.metadata?.type && (
              <Chip label={item.metadata.type} size="small" variant="outlined" color="primary" />
            )}
          </Box>
          {getOptionDescription(item) && (
            <Typography variant="caption" color="text.secondary">
              {getOptionDescription(item)}
            </Typography>
          )}
        </Box>
      );
    }
  };

  if (error) {
    return (
      <Alert severity="error" className={className}>
        <AlertTitle>Error</AlertTitle>
        Failed to load {label.toLowerCase()}: {error.message}
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

      {/* Selection Controls */}
      <Box sx={{ display: 'flex', gap: 1, mb: showInfoCard && selectedItem ? 2 : 0 }}>
        <FormControl fullWidth>
          <InputLabel id={`${label.toLowerCase().replace(/\s+/g, '-')}-select-label`}>
            {label} {required && <span style={{ color: 'red' }}>*</span>}
          </InputLabel>
          <Select
            labelId={`${label.toLowerCase().replace(/\s+/g, '-')}-select-label`}
            value={selectedId ? selectedId.toString() : ''}
            onChange={handleSelectionChange}
            disabled={disabled || loading}
            label={`${label} ${required ? '*' : ''}`}
            MenuProps={{
              PaperProps: {
                sx: { maxHeight: variant === 'detailed' ? maxHeight + 100 : maxHeight },
              },
            }}
          >
            {!required && (
              <MenuItem value="">
                <Typography color="text.secondary">
                  {placeholder || `No ${label.toLowerCase()} selected`}
                </Typography>
              </MenuItem>
            )}
            {filteredData.map(item => (
              <MenuItem key={getOptionValue(item)} value={getOptionValue(item).toString()}>
                {renderOption ? renderOption(item) : defaultRenderOption(item)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        {/* Action Buttons */}
        {(showRefreshButton || showCreateButton) && (
          <Box sx={{ display: 'flex', alignItems: 'flex-end', gap: 0.5 }}>
            {showRefreshButton && (
              <Tooltip title={`Refresh ${label}`}>
                <IconButton
                  onClick={handleRefresh}
                  disabled={disabled || loading}
                  size="medium"
                  color="primary"
                >
                  {loading ? <CircularProgress size={20} /> : <Refresh />}
                </IconButton>
              </Tooltip>
            )}

            {showCreateButton && onCreateClick && (
              <Tooltip title={`Create New ${label}`}>
                <IconButton
                  onClick={onCreateClick}
                  disabled={disabled}
                  size="medium"
                  color="primary"
                >
                  <Add />
                </IconButton>
              </Tooltip>
            )}
          </Box>
        )}
      </Box>

      {/* Loading State */}
      {loading && (
        <Alert severity="info" sx={{ mt: 1 }}>
          Loading {label.toLowerCase()}...
        </Alert>
      )}

      {/* Empty State */}
      {!loading && filteredData.length === 0 && (
        <Alert severity="info" sx={{ mt: 1 }}>
          {emptyMessage || `No ${label.toLowerCase()} found.`}
        </Alert>
      )}

      {/* Selected Item Info Card */}
      {showInfoCard && selectedItem && (
        <Card variant="outlined" sx={{ mt: 2 }}>
          <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
            {renderInfoCard ? (
              renderInfoCard(selectedItem)
            ) : (
              <Box>
                <Typography variant="subtitle2" fontWeight="medium" gutterBottom>
                  {getOptionLabel(selectedItem)}
                </Typography>
                {getOptionDescription(selectedItem) && (
                  <Typography variant="body2" color="text.secondary">
                    {getOptionDescription(selectedItem)}
                  </Typography>
                )}
                {selectedItem.displayText && (
                  <Typography
                    variant="caption"
                    color="text.secondary"
                    sx={{ display: 'block', mt: 1 }}
                  >
                    {selectedItem.displayText}
                  </Typography>
                )}
              </Box>
            )}
          </CardContent>
        </Card>
      )}
    </Box>
  );
}

export default GenericSelector;
