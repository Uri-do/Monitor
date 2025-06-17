import React from 'react';
import {
  Box,
  Typography,
  Chip,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  PlayArrow as PlayIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { ExecutionLogEntry } from '../types';

interface LogEntryProps {
  entry: ExecutionLogEntry;
}

export const LogEntry: React.FC<LogEntryProps> = ({ entry }) => {
  const getLogIcon = (type: ExecutionLogEntry['type']) => {
    switch (type) {
      case 'started':
        return <PlayIcon color="primary" />;
      case 'completed':
        return <SuccessIcon color="success" />;
      case 'error':
        return <ErrorIcon color="error" />;
      case 'info':
        return <InfoIcon color="info" />;
      default:
        return <InfoIcon />;
    }
  };

  const getLogColor = (type: ExecutionLogEntry['type']) => {
    switch (type) {
      case 'started':
        return 'primary';
      case 'completed':
        return 'success';
      case 'error':
        return 'error';
      case 'info':
        return 'info';
      default:
        return 'default';
    }
  };

  return (
    <ListItem alignItems="flex-start">
      <ListItemIcon sx={{ minWidth: 40 }}>
        {getLogIcon(entry.type)}
      </ListItemIcon>
      <ListItemText
        primary={
          <Box display="flex" alignItems="center" gap={1} mb={0.5}>
            <Typography variant="body2" fontWeight="medium">
              {entry.indicator}
            </Typography>
            <Chip 
              label={entry.type.toUpperCase()} 
              size="small" 
              color={getLogColor(entry.type) as any}
              variant="outlined"
            />
            {entry.duration && (
              <Chip 
                label={`${entry.duration}ms`} 
                size="small" 
                variant="outlined"
              />
            )}
          </Box>
        }
        secondary={
          <Box>
            <Typography variant="body2" color="text.primary" sx={{ mb: 0.5 }}>
              {entry.message}
            </Typography>
            {entry.details && (
              <Box display="flex" flexWrap="wrap" gap={0.5} mb={0.5}>
                {entry.details.collectorID && (
                  <Chip 
                    label={`Collector: ${entry.details.collectorID}`} 
                    size="small" 
                    variant="outlined"
                  />
                )}
                {entry.details.collectorItemName && (
                  <Chip 
                    label={`Item: ${entry.details.collectorItemName}`} 
                    size="small" 
                    variant="outlined"
                  />
                )}
                {entry.details.lastMinutes && (
                  <Chip
                    label={`${entry.details.lastMinutes}min`}
                    size="small"
                    variant="outlined"
                  />
                )}
                {entry.details.executionContext && (
                  <Chip
                    label={entry.details.executionContext}
                    size="small"
                    variant="outlined"
                    color="info"
                  />
                )}
                {entry.details.alertsGenerated !== undefined && entry.details.alertsGenerated > 0 && (
                  <Chip
                    label={`${entry.details.alertsGenerated} alerts`}
                    size="small"
                    variant="outlined"
                    color="warning"
                  />
                )}
                {entry.details.value !== undefined && (
                  <Chip
                    label={`Value: ${entry.details.value}`}
                    size="small"
                    variant="outlined"
                    color="success"
                  />
                )}
              </Box>
            )}
            <Typography variant="caption" color="text.secondary">
              {format(entry.timestamp, 'HH:mm:ss')}
            </Typography>
          </Box>
        }
      />
    </ListItem>
  );
};

export default LogEntry;
