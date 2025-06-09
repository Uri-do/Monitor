import React from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  useTheme,
} from '@mui/material';
import { History, PlayArrow, CheckCircle } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns';
import { AlertDashboardDto } from '../../../types/api';

interface RecentAlertsCardProps {
  alertDashboard?: AlertDashboardDto;
}

const RecentAlertsCard: React.FC<RecentAlertsCardProps> = ({ alertDashboard }) => {
  const navigate = useNavigate();
  const theme = useTheme();

  const getSeverityColor = (severity: string | undefined) => {
    if (!severity) return 'secondary';
    switch (severity.toLowerCase()) {
      case 'critical':
        return 'error';
      case 'high':
        return 'warning';
      case 'medium':
        return 'info';
      case 'low':
        return 'success';
      default:
        return 'secondary';
    }
  };

  return (
    <Grid item xs={12} md={6}>
      <Card sx={{ height: '100%' }}>
        <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Box display="flex" alignItems="center" gap={1}>
              <History sx={{ color: 'warning.main' }} />
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                Recent Alerts
              </Typography>
            </Box>
            <IconButton
              size="small"
              onClick={() => navigate('/alerts')}
              sx={{
                backgroundColor: 'warning.main',
                color: 'white',
                '&:hover': {
                  backgroundColor: 'warning.dark',
                },
              }}
            >
              <PlayArrow />
            </IconButton>
          </Box>

          <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
            {alertDashboard?.recentAlerts && alertDashboard.recentAlerts.length > 0 ? (
              <List sx={{ p: 0 }}>
                {alertDashboard.recentAlerts.slice(0, 5).map(alert => (
                  <ListItem
                    key={`${alert.kpiId}-${alert.triggerTime}`}
                    button
                    onClick={() => navigate(`/alerts/${alert.alertId}`)}
                    sx={{
                      borderRadius: 2,
                      mb: 1,
                      border: '1px solid',
                      borderColor: 'divider',
                      backgroundColor: 'background.paper',
                      '&:hover': {
                        backgroundColor: 'action.hover',
                        transform: 'translateX(4px)',
                        transition: 'all 0.2s ease-in-out',
                      },
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
                      <Chip
                        label={alert.severity}
                        color={getSeverityColor(alert.severity)}
                        size="small"
                        sx={{ fontWeight: 600 }}
                      />
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
                          {alert.kpiIndicator}
                        </Typography>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                            {alert.message}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {format(new Date(alert.triggerTime), 'MMM dd, HH:mm')}
                          </Typography>
                        </Box>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
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
                <CheckCircle sx={{ fontSize: 48, color: 'success.main', mb: 2 }} />
                <Typography color="text.secondary" variant="body2" sx={{ fontWeight: 500 }}>
                  No recent alerts
                </Typography>
                <Typography color="text.secondary" variant="caption">
                  Your system is running smoothly
                </Typography>
              </Box>
            )}
          </Box>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default RecentAlertsCard;
