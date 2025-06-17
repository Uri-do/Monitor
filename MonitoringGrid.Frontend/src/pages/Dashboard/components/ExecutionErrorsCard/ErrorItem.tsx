import React from 'react';
import {
  ListItem,
  ListItemIcon,
  ListItemText,
  Typography,
  Box,
  Chip,
  Button,
} from '@mui/material';
import {
  Error as ErrorIcon,
  Warning as WarningIcon,
  Visibility as ViewIcon,
} from '@mui/icons-material';
import { formatDistanceToNow } from 'date-fns';

export interface ExecutionError {
  id: string;
  indicatorID: number;
  indicator: string;
  owner: string;
  errorMessage: string;
  duration: number;
  completedAt: string;
  collectorID?: number;
  collectorItemName?: string;
  lastMinutes?: number;
  executionContext?: string;
  timestamp: Date;
}

interface ErrorItemProps {
  error: ExecutionError;
  onViewDetails: (error: ExecutionError) => void;
}

export const ErrorItem: React.FC<ErrorItemProps> = ({ error, onViewDetails }) => {
  const getErrorSeverity = (errorMessage: string) => {
    if (errorMessage.includes('not found in collector results')) {
      return 'warning'; // Configuration issue
    }
    if (errorMessage.includes('timeout') || errorMessage.includes('cancelled')) {
      return 'info'; // Operational issue
    }
    return 'error'; // Critical error
  };

  const getErrorIcon = (errorMessage: string) => {
    const severity = getErrorSeverity(errorMessage);
    switch (severity) {
      case 'warning':
        return <WarningIcon color="warning" />;
      case 'info':
        return <ErrorIcon color="info" />;
      default:
        return <ErrorIcon color="error" />;
    }
  };

  const severity = getErrorSeverity(error.errorMessage);

  return (
    <ListItem
      sx={{
        borderRadius: 2,
        mb: 1,
        border: '1px solid',
        borderColor: `${severity}.light`,
        backgroundColor: `${severity}.50`,
        '&:hover': {
          backgroundColor: `${severity}.100`,
        },
      }}
    >
      <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
        {getErrorIcon(error.errorMessage)}
      </ListItemIcon>
      <ListItemText
        primary={
          <Box display="flex" alignItems="center" gap={1} mb={0.5}>
            <Typography variant="subtitle2" fontWeight="medium">
              {error.indicator}
            </Typography>
            <Chip 
              label={`${error.duration}ms`} 
              size="small" 
              variant="outlined"
            />
          </Box>
        }
        secondary={
          <Box>
            <Typography 
              variant="body2" 
              color="text.primary" 
              sx={{ mb: 0.5 }}
              noWrap
            >
              {error.errorMessage.length > 60 
                ? `${error.errorMessage.substring(0, 60)}...`
                : error.errorMessage
              }
            </Typography>
            <Box display="flex" justifyContent="space-between" alignItems="center">
              <Typography variant="caption" color="text.secondary">
                {formatDistanceToNow(error.timestamp, { addSuffix: true })}
              </Typography>
              <Button
                size="small"
                startIcon={<ViewIcon />}
                onClick={() => onViewDetails(error)}
                sx={{ minWidth: 'auto', px: 1 }}
              >
                Details
              </Button>
            </Box>
          </Box>
        }
        primaryTypographyProps={{
          component: 'div',
        }}
        secondaryTypographyProps={{
          component: 'div',
        }}
      />
    </ListItem>
  );
};

export default ErrorItem;
