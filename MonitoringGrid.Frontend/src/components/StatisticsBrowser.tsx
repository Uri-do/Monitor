import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  IconButton,
} from '@mui/material';
import { Close as CloseIcon, Analytics as AnalyticsIcon } from '@mui/icons-material';
import StatsExplorer from './StatsExplorer';

interface StatisticsBrowserProps {
  open: boolean;
  onClose: () => void;
  initialCollectorId?: number;
  initialItemName?: string;
}

export const StatisticsBrowser: React.FC<StatisticsBrowserProps> = ({
  open,
  onClose,
  initialCollectorId,
  initialItemName,
}) => {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="xl"
      fullWidth
      PaperProps={{
        sx: { height: '90vh', maxHeight: '90vh' },
      }}
    >
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <AnalyticsIcon color="primary" />
            <Typography variant="h6">Statistics Browser</Typography>
          </Box>
          <IconButton onClick={onClose}>
            <CloseIcon />
          </IconButton>
        </Box>
      </DialogTitle>

      <DialogContent sx={{ p: 2 }}>
        <StatsExplorer initialCollectorId={initialCollectorId} initialItemName={initialItemName} />
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
};

export default StatisticsBrowser;
