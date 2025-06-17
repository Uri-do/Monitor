import React from 'react';
import {
  Box,
  List,
  Typography,
} from '@mui/material';
import {
  Error as ErrorIcon,
} from '@mui/icons-material';
import { ErrorItem, ExecutionError } from './ErrorItem';

interface ErrorListProps {
  errors: ExecutionError[];
  onViewError: (error: ExecutionError) => void;
}

export const ErrorList: React.FC<ErrorListProps> = ({ errors, onViewError }) => {
  if (errors.length === 0) {
    return (
      <Box
        display="flex"
        flexDirection="column"
        alignItems="center"
        justifyContent="center"
        py={4}
        sx={{
          backgroundColor: 'success.50',
          borderRadius: 2,
          border: '2px dashed',
          borderColor: 'success.200',
        }}
      >
        <ErrorIcon sx={{ fontSize: 48, color: 'success.400', mb: 2 }} />
        <Typography color="success.main" variant="body2" sx={{ fontWeight: 500 }}>
          No execution errors
        </Typography>
        <Typography color="text.secondary" variant="caption">
          All indicators are running successfully
        </Typography>
      </Box>
    );
  }

  return (
    <List sx={{ p: 0, maxHeight: 300, overflow: 'auto' }}>
      {errors.map((error) => (
        <ErrorItem
          key={error.id}
          error={error}
          onViewDetails={onViewError}
        />
      ))}
    </List>
  );
};

export default ErrorList;
