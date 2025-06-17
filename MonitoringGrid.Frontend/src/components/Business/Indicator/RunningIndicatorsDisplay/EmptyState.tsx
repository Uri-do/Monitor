import React from 'react';
import {
  Box,
  Typography,
  useTheme,
} from '@mui/material';
import { PlayCircle } from '@mui/icons-material';

export const EmptyState: React.FC = () => {
  const theme = useTheme();

  return (
    <Box
      display="flex"
      flexDirection="column"
      alignItems="center"
      justifyContent="center"
      py={4}
      sx={{
        backgroundColor: theme.palette.mode === 'light' ? 'grey.50' : 'grey.900',
        borderRadius: 2,
        border: '2px dashed',
        borderColor: theme.palette.mode === 'light' ? 'grey.300' : 'grey.700',
      }}
    >
      <PlayCircle sx={{ fontSize: 48, color: 'grey.400', mb: 2 }} />
      <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
        No Indicators currently running
      </Typography>
      <Typography color="text.secondary" variant="caption">
        All Indicators are idle
      </Typography>
    </Box>
  );
};

export default EmptyState;
