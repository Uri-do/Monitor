import React from 'react';
import { Box, Typography, Chip, IconButton, Tooltip } from '@mui/material';
import { Refresh, Timer } from '@mui/icons-material';
import { formatDistanceToNow } from 'date-fns';

interface DashboardHeaderProps {
  lastUpdate?: string;
  countdown?: number | null;
  onRefresh: () => void;
}

const DashboardHeader: React.FC<DashboardHeaderProps> = ({
  lastUpdate,
  countdown,
  onRefresh,
}) => {
  const formatCountdown = (seconds: number): string => {
    if (seconds <= 0) return 'Due Now!';

    const days = Math.floor(seconds / (24 * 60 * 60));
    const hours = Math.floor((seconds % (24 * 60 * 60)) / (60 * 60));
    const mins = Math.floor((seconds % (60 * 60)) / 60);
    const secs = seconds % 60;

    if (days > 0) {
      return `${days}d ${hours}h ${mins}m`;
    } else if (hours > 0) {
      return `${hours}h ${mins}m`;
    } else if (mins > 0) {
      return `${mins}m ${secs}s`;
    } else {
      return `${secs}s`;
    }
  };

  return (
    <Box
      display="flex"
      justifyContent="space-between"
      alignItems="center"
      mb={4}
      sx={{
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        borderRadius: 3,
        p: 3,
        color: 'white',
        boxShadow: '0px 4px 20px rgba(102, 126, 234, 0.3)',
      }}
    >
      <Box>
        <Box display="flex" alignItems="center" gap={2} mb={1}>
          <Typography variant="h4" sx={{ fontWeight: 700 }}>
            Dashboard
          </Typography>
          <Chip
            label="LIVE"
            size="small"
            sx={{
              backgroundColor: 'rgba(76, 175, 80, 0.2)',
              color: 'white',
              fontWeight: 600,
              animation: 'pulse 2s infinite',
              '@keyframes pulse': {
                '0%': { opacity: 1 },
                '50%': { opacity: 0.7 },
                '100%': { opacity: 1 },
              },
            }}
          />
        </Box>
        <Typography variant="subtitle1" sx={{ opacity: 0.9 }}>
          Monitor your KPIs and system health in real-time
        </Typography>
        {lastUpdate && (
          <Typography variant="caption" sx={{ opacity: 0.8, mt: 0.5, display: 'block' }}>
            Last updated: {formatDistanceToNow(new Date(lastUpdate), { addSuffix: true })}
          </Typography>
        )}
      </Box>
      <Box display="flex" alignItems="center" gap={1}>
        {countdown !== null && countdown !== undefined && countdown > 0 && (
          <Tooltip title="Next KPI execution countdown">
            <Chip
              icon={<Timer sx={{
                fontSize: '16px !important',
                animation: countdown <= 60 ? 'pulse 1s infinite' : 'none',
              }} />}
              label={formatCountdown(countdown)}
              size="small"
              sx={{
                backgroundColor: countdown <= 300 ? 'rgba(255, 152, 0, 0.3)' : 'rgba(255, 255, 255, 0.2)',
                color: 'white',
                fontWeight: 600,
                animation: countdown <= 60 ? 'pulse 1s infinite' : 'none',
                '@keyframes pulse': {
                  '0%': { opacity: 1 },
                  '50%': { opacity: 0.7 },
                  '100%': { opacity: 1 },
                },
                mr: 1,
              }}
            />
          </Tooltip>
        )}
        <IconButton
          onClick={onRefresh}
          sx={{
            color: 'white',
            backgroundColor: 'rgba(255, 255, 255, 0.1)',
            '&:hover': {
              backgroundColor: 'rgba(255, 255, 255, 0.2)',
            },
          }}
        >
          <Refresh />
        </IconButton>
      </Box>
    </Box>
  );
};

export default DashboardHeader;
