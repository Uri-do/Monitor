import React from 'react';
import { Box, Typography, Chip, IconButton, Tooltip } from '@mui/material';
import { Refresh, Timer, Dashboard as DashboardIcon } from '@mui/icons-material';
import { formatDistanceToNow } from 'date-fns';
import {
  formatCountdownWithContext,
  getCountdownSeverity,
  shouldCountdownPulse,
} from '@/utils/countdown';
import { Card } from '@/components';

interface DashboardHeaderProps {
  lastUpdate?: string;
  countdown?: number | null;
  onRefresh: () => void;
}

const DashboardHeader: React.FC<DashboardHeaderProps> = ({ lastUpdate, countdown, onRefresh }) => {
  return (
    <Card gradient="primary" glowEffect={true} sx={{ mb: 4 }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ p: 3 }}>
        <Box>
          <Box display="flex" alignItems="center" gap={2} mb={1}>
            <DashboardIcon sx={{ fontSize: '2rem', color: 'white' }} />
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
                icon={
                  <Timer
                    sx={{
                      fontSize: '16px !important',
                      animation: shouldCountdownPulse(countdown) ? 'pulse 1s infinite' : 'none',
                    }}
                  />
                }
                label={formatCountdownWithContext(countdown).display}
                size="small"
                aria-label={formatCountdownWithContext(countdown).ariaLabel}
                sx={{
                  backgroundColor:
                    getCountdownSeverity(countdown) === 'warning'
                      ? 'rgba(255, 152, 0, 0.3)'
                      : getCountdownSeverity(countdown) === 'error'
                        ? 'rgba(244, 67, 54, 0.3)'
                        : 'rgba(255, 255, 255, 0.2)',
                  color: 'white',
                  fontWeight: 600,
                  animation: shouldCountdownPulse(countdown) ? 'pulse 1s infinite' : 'none',
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
    </Card>
  );
};

export default DashboardHeader;
