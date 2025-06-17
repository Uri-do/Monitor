import React from 'react';
import {
  Box,
  Typography,
  Paper,
  List,
  Divider,
} from '@mui/material';
import { LogEntry } from './LogEntry';
import { ExecutionLogEntry } from '../types';

interface LogContainerProps {
  entries: ExecutionLogEntry[];
  filterErrors: boolean;
  logContainerRef: React.RefObject<HTMLDivElement>;
}

export const LogContainer: React.FC<LogContainerProps> = ({
  entries,
  filterErrors,
  logContainerRef,
}) => {
  const filteredEntries = filterErrors 
    ? entries.filter(entry => entry.type === 'error')
    : entries;

  return (
    <Paper 
      variant="outlined" 
      sx={{ 
        maxHeight: 400, 
        overflow: 'auto',
        backgroundColor: 'grey.50'
      }}
      ref={logContainerRef}
    >
      {filteredEntries.length === 0 ? (
        <Box p={3} textAlign="center">
          <Typography variant="body2" color="text.secondary">
            {filterErrors ? 'No errors logged' : 'No execution logs yet'}
          </Typography>
        </Box>
      ) : (
        <List dense>
          {filteredEntries.map((entry, index) => (
            <React.Fragment key={entry.id}>
              <LogEntry entry={entry} />
              {index < filteredEntries.length - 1 && <Divider />}
            </React.Fragment>
          ))}
        </List>
      )}
    </Paper>
  );
};

export default LogContainer;
