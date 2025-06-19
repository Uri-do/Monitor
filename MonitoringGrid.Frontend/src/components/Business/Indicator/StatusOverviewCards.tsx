import React from 'react';
import {
  Grid,
  Typography,
  CardContent,
} from '@mui/material';
import {
  Settings as SettingsIcon,
  TrendingUp as TrendingUpIcon,
  Schedule as ScheduleIcon,
  Person as PersonIcon,
} from '@mui/icons-material';
import { Card, StatusChip } from '@/components/UI';
import { IndicatorDto } from '@/types/api';
import { safeFormatDate } from '@/utils/dateUtils';

interface StatusOverviewCardsProps {
  indicator: IndicatorDto;
}

/**
 * Component to display status overview cards
 */
export const StatusOverviewCards: React.FC<StatusOverviewCardsProps> = ({ indicator }) => {
  const getIndicatorStatus = () => {
    if (!indicator.isActive) return 'inactive';
    if (indicator.isCurrentlyRunning) return 'running';
    return 'active';
  };

  const getPriorityLabel = (priority: string | number) => {
    if (typeof priority === 'string') {
      return priority.charAt(0).toUpperCase() + priority.slice(1);
    }
    switch (priority) {
      case 1:
        return 'High';
      case 2:
        return 'Medium';
      case 3:
        return 'Normal';
      case 4:
        return 'Low';
      case 5:
        return 'Very Low';
      default:
        return 'Unknown';
    }
  };

  return (
    <Grid container spacing={3}>
      {/* Status Card */}
      <Grid item xs={12} sm={6} md={3}>
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 2 }}>
            <SettingsIcon color="primary" sx={{ fontSize: '2rem', mb: 1 }} />
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Status
            </Typography>
            <StatusChip status={getIndicatorStatus()} />
          </CardContent>
        </Card>
      </Grid>

      {/* Priority Card */}
      <Grid item xs={12} sm={6} md={3}>
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 2 }}>
            <TrendingUpIcon color="primary" sx={{ fontSize: '2rem', mb: 1 }} />
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Priority
            </Typography>
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              {indicator.priority ? getPriorityLabel(indicator.priority) : 'Not Set'}
            </Typography>
          </CardContent>
        </Card>
      </Grid>

      {/* Last Executed Card */}
      <Grid item xs={12} sm={6} md={3}>
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 2 }}>
            <ScheduleIcon color="primary" sx={{ fontSize: '2rem', mb: 1 }} />
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Last Executed
            </Typography>
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              {safeFormatDate(indicator.lastRun, 'MMM dd, yyyy HH:mm', 'Never')}
            </Typography>
          </CardContent>
        </Card>
      </Grid>

      {/* Owner Card */}
      <Grid item xs={12} sm={6} md={3}>
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 2 }}>
            <PersonIcon color="primary" sx={{ fontSize: '2rem', mb: 1 }} />
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Owner
            </Typography>
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              {indicator.ownerContact?.name || indicator.ownerName || 'Unknown'}
            </Typography>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
};

export default StatusOverviewCards;
