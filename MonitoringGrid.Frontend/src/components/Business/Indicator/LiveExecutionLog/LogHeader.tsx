import React from 'react';
import {
  Box,
  Typography,
  Badge,
} from '@mui/material';
import {
  Timeline as TimelineIcon,
  Error as ErrorIcon,
} from '@mui/icons-material';
import { LogControls } from './LogControls';

interface LogHeaderProps {
  errorCount: number;
  isPaused: boolean;
  isExpanded: boolean;
  filterErrors: boolean;
  onPauseToggle: () => void;
  onExpandToggle: () => void;
  onFilterToggle: (checked: boolean) => void;
  onClearLog: () => void;
}

export const LogHeader: React.FC<LogHeaderProps> = ({
  errorCount,
  isPaused,
  isExpanded,
  filterErrors,
  onPauseToggle,
  onExpandToggle,
  onFilterToggle,
  onClearLog,
}) => {
  return (
    <>
      <Box display="flex" alignItems="center" gap={1}>
        <TimelineIcon />
        <Typography variant="h6">Live Execution Log</Typography>
        {errorCount > 0 && (
          <Badge badgeContent={errorCount} color="error">
            <ErrorIcon color="error" />
          </Badge>
        )}
      </Box>
      <LogControls
        isPaused={isPaused}
        isExpanded={isExpanded}
        filterErrors={filterErrors}
        onPauseToggle={onPauseToggle}
        onExpandToggle={onExpandToggle}
        onFilterToggle={onFilterToggle}
        onClearLog={onClearLog}
      />
    </>
  );
};

export default LogHeader;
