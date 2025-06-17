import React from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  IconButton,
  Badge,
  Divider,
} from '@mui/material';
import {
  Warning as WarningIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  CheckCircle as SuccessIcon,
  PlayArrow as ViewAllIcon,
  Notifications as NotificationIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';

/**
 * Alert Summary Card Component
 * Displays alert statistics and recent alerts
 */

export interface AlertSummary {
  critical: number;
  high: number;
  medium: number;
  low: number;
  total: number;
}

export interface RecentAlert {
  id: string;
  severity: 'critical' | 'high' | 'medium' | 'low';
  indicator: string;
  message: string;
  timestamp: Date;
  acknowledged?: boolean;
}

export interface AlertSummaryCardProps {
  summary: AlertSummary;
  recentAlerts: RecentAlert[];
  onViewAll?: () => void;
  onAlertClick?: (alert: RecentAlert) => void;
  maxRecentAlerts?: number;
}

const getSeverityColor = (severity: string) => {
  switch (severity.toLowerCase()) {
    case 'critical': return 'error';
    case 'high': return 'warning';
    case 'medium': return 'info';
    case 'low': return 'success';
    default: return 'default';
  }
};

const getSeverityIcon = (severity: string) => {
  switch (severity.toLowerCase()) {
    case 'critical': return <ErrorIcon color="error" />;
    case 'high': return <WarningIcon color="warning" />;
    case 'medium': return <InfoIcon color="info" />;
    case 'low': return <SuccessIcon color="success" />;
    default: return <InfoIcon />;
  }
};

export const AlertSummaryCard: React.FC<AlertSummaryCardProps> = ({
  summary,
  recentAlerts,
  onViewAll,
  onAlertClick,
  maxRecentAlerts = 5,
}) => {
  const displayAlerts = recentAlerts.slice(0, maxRecentAlerts);
  const hasAlerts = summary.total > 0;

  return (
    <Card sx={{ height: '100%' }}>
      <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        {/* Header */}
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Box display="flex" alignItems="center" gap={1}>
            <Badge
              badgeContent={summary.total}
              color={hasAlerts ? 'error' : 'success'}
              sx={{ '& .MuiBadge-badge': { fontSize: '0.7rem' } }}
            >
              <NotificationIcon sx={{ color: hasAlerts ? 'error.main' : 'success.main' }} />
            </Badge>
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              Alert Summary
            </Typography>
          </Box>
          {onViewAll && (
            <IconButton
              size="small"
              onClick={onViewAll}
              sx={{
                backgroundColor: 'primary.main',
                color: 'white',
                '&:hover': {
                  backgroundColor: 'primary.dark',
                },
              }}
            >
              <ViewAllIcon />
            </IconButton>
          )}
        </Box>

        {/* Alert Counts */}
        <Box display="flex" gap={1} mb={2} flexWrap="wrap">
          <Chip
            label={`Critical: ${summary.critical}`}
            color="error"
            variant={summary.critical > 0 ? 'filled' : 'outlined'}
            size="small"
          />
          <Chip
            label={`High: ${summary.high}`}
            color="warning"
            variant={summary.high > 0 ? 'filled' : 'outlined'}
            size="small"
          />
          <Chip
            label={`Medium: ${summary.medium}`}
            color="info"
            variant={summary.medium > 0 ? 'filled' : 'outlined'}
            size="small"
          />
          <Chip
            label={`Low: ${summary.low}`}
            color="success"
            variant={summary.low > 0 ? 'filled' : 'outlined'}
            size="small"
          />
        </Box>

        <Divider sx={{ mb: 2 }} />

        {/* Recent Alerts */}
        <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
          <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 600 }}>
            Recent Alerts
          </Typography>
          
          {displayAlerts.length > 0 ? (
            <List sx={{ p: 0, maxHeight: 300, overflow: 'auto' }}>
              {displayAlerts.map((alert, index) => (
                <React.Fragment key={alert.id}>
                  <ListItem
                    button={!!onAlertClick}
                    onClick={() => onAlertClick?.(alert)}
                    sx={{
                      borderRadius: 1,
                      mb: 0.5,
                      backgroundColor: alert.acknowledged ? 'grey.50' : 'background.paper',
                      opacity: alert.acknowledged ? 0.7 : 1,
                      '&:hover': {
                        backgroundColor: 'action.hover',
                      },
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: 'auto', mr: 1 }}>
                      {getSeverityIcon(alert.severity)}
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="body2" fontWeight="medium" noWrap>
                            {alert.indicator}
                          </Typography>
                          <Chip
                            label={alert.severity.toUpperCase()}
                            color={getSeverityColor(alert.severity) as any}
                            size="small"
                            sx={{ fontSize: '0.6rem', height: 16 }}
                          />
                          {alert.acknowledged && (
                            <Chip
                              label="ACK"
                              size="small"
                              variant="outlined"
                              sx={{ fontSize: '0.6rem', height: 16 }}
                            />
                          )}
                        </Box>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="text.secondary" noWrap sx={{ mb: 0.5 }}>
                            {alert.message}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {format(alert.timestamp, 'MMM dd, HH:mm')}
                          </Typography>
                        </Box>
                      }
                      primaryTypographyProps={{
                        component: 'div',
                      }}
                      secondaryTypographyProps={{
                        component: 'div',
                      }}
                    />
                  </ListItem>
                  {index < displayAlerts.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          ) : (
            <Box
              display="flex"
              flexDirection="column"
              alignItems="center"
              justifyContent="center"
              py={3}
              sx={{
                backgroundColor: 'success.50',
                borderRadius: 2,
                border: '2px dashed',
                borderColor: 'success.200',
              }}
            >
              <SuccessIcon sx={{ fontSize: 40, color: 'success.main', mb: 1 }} />
              <Typography color="success.main" variant="body2" sx={{ fontWeight: 500 }}>
                No recent alerts
              </Typography>
              <Typography color="text.secondary" variant="caption">
                System is running smoothly
              </Typography>
            </Box>
          )}
        </Box>

        {/* Footer */}
        {summary.total > maxRecentAlerts && (
          <Box sx={{ mt: 2, textAlign: 'center' }}>
            <Typography variant="caption" color="text.secondary">
              Showing {maxRecentAlerts} of {summary.total} alerts
            </Typography>
          </Box>
        )}
      </CardContent>
    </Card>
  );
};

export default AlertSummaryCard;
