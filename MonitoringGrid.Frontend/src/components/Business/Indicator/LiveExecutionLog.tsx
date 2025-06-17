import React from 'react';
import {
  Card,
  CardContent,
  CardHeader,
  Box,
  Chip,
} from '@mui/material';
import {
  Pause as PauseIcon,
} from '@mui/icons-material';
import { LogHeader } from './LiveExecutionLog/LogHeader';
import { LogContainer } from './LiveExecutionLog/LogContainer';
import { useLiveExecutionLog } from './LiveExecutionLog/useLiveExecutionLog';

// Re-export types for backward compatibility
export type { ExecutionLogEntry } from './types';

interface LiveExecutionLogProps {
  maxEntries?: number;
  autoScroll?: boolean;
  showOnlyErrors?: boolean;
}

const LiveExecutionLog: React.FC<LiveExecutionLogProps> = ({
  maxEntries = 50,
  autoScroll = true,
  showOnlyErrors = false,
}) => {
  const {
    logEntries,
    isPaused,
    isExpanded,
    filterErrors,
    logContainerRef,
    errorCount,
    setIsPaused,
    setIsExpanded,
    setFilterErrors,
    clearLog,
  } = useLiveExecutionLog({
    maxEntries,
    autoScroll,
    showOnlyErrors,
  });

  return (
    <Card>
      <CardHeader
        title={
          <LogHeader
            errorCount={errorCount}
            isPaused={isPaused}
            isExpanded={isExpanded}
            filterErrors={filterErrors}
            onPauseToggle={() => setIsPaused(!isPaused)}
            onExpandToggle={() => setIsExpanded(!isExpanded)}
            onFilterToggle={setFilterErrors}
            onClearLog={clearLog}
          />
        }
      />

      {isExpanded && (
        <CardContent sx={{ pt: 0 }}>
          <LogContainer
            entries={logEntries}
            filterErrors={filterErrors}
            logContainerRef={logContainerRef}
          />

          {isPaused && (
            <Box mt={1} textAlign="center">
              <Chip
                label="Logging Paused"
                color="warning"
                size="small"
                icon={<PauseIcon />}
              />
            </Box>
          )}
        </CardContent>
      )}
    </Card>
  );
};

export default LiveExecutionLog;
