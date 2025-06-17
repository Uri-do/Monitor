import React from 'react';
import {
  Box,
  Typography,
  IconButton,
  Badge,
  Tooltip,
} from '@mui/material';
import {
  Error as ErrorIcon,
  Clear as ClearIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';

interface ErrorHeaderProps {
  errorCount: number;
  isExpanded: boolean;
  onToggleExpanded: () => void;
  onClearErrors: () => void;
}

export const ErrorHeader: React.FC<ErrorHeaderProps> = ({
  errorCount,
  isExpanded,
  onToggleExpanded,
  onClearErrors,
}) => {
  return (
    <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
      <Box display="flex" alignItems="center" gap={1}>
        <Badge
          badgeContent={errorCount}
          color="error"
          sx={{ '& .MuiBadge-badge': { fontSize: '0.7rem' } }}
        >
          <ErrorIcon sx={{ color: 'error.main' }} />
        </Badge>
        <Typography variant="h6" sx={{ fontWeight: 600 }}>
          Recent Execution Errors
        </Typography>
      </Box>
      <Box display="flex" alignItems="center" gap={1}>
        <Tooltip title="Clear all errors">
          <IconButton 
            size="small" 
            onClick={onClearErrors}
            disabled={errorCount === 0}
          >
            <ClearIcon />
          </IconButton>
        </Tooltip>
        <Tooltip title={isExpanded ? "Collapse" : "Expand"}>
          <IconButton 
            size="small" 
            onClick={onToggleExpanded}
          >
            {isExpanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          </IconButton>
        </Tooltip>
      </Box>
    </Box>
  );
};

export default ErrorHeader;
