import React from 'react';
import {
  Grid,
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
import { TrendingUp, Warning, CheckCircle, PlayArrow } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns';
import { IndicatorDashboardDto } from '@/types/api';
import { Card } from '@/components';

interface IndicatorsDueCardProps {
  indicatorDashboard?: IndicatorDashboardDto;
}

const IndicatorsDueCard: React.FC<IndicatorsDueCardProps> = ({ indicatorDashboard }) => {
  const navigate = useNavigate();
  const theme = useTheme();

  // Hide this card if there are 2 or fewer Indicators to avoid redundancy with NextIndicatorExecutionCard
  if (!indicatorDashboard?.dueIndicators || indicatorDashboard.dueIndicators.length <= 2) {
    return null;
  }

  return (
    <Grid item xs={12} md={6}>
      <Card sx={{ height: '100%' }}>
        <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Box display="flex" alignItems="center" gap={1}>
              <TrendingUp sx={{ color: 'info.main' }} />
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                Indicators Due for Execution
              </Typography>
            </Box>
            <IconButton
              size="small"
              onClick={() => navigate('/indicators')}
              sx={{
                backgroundColor: 'info.main',
                color: 'white',
                '&:hover': {
                  backgroundColor: 'info.dark',
                },
              }}
            >
              <PlayArrow />
            </IconButton>
          </Box>

          <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
            {indicatorDashboard?.dueIndicators && indicatorDashboard.dueIndicators.length > 0 ? (
              <List sx={{ p: 0 }}>
                {indicatorDashboard.dueIndicators.map(indicator => (
                  <ListItem
                    key={indicator.indicatorId}
                    sx={{
                      borderRadius: 1,
                      mb: 1,
                      border: '1px solid',
                      borderColor: 'warning.light',
                      backgroundColor: 'warning.50',
                      '&:hover': {
                        backgroundColor: 'warning.100',
                        transform: 'translateX(4px)',
                        transition: 'all 0.2s ease-in-out',
                      },
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: 'auto', mr: 2 }}>
                      <Box display="flex" alignItems="center" gap={1}>
                        <Chip
                          label={indicator.status === 'never_run' ? 'Never Run' : 'Due'}
                          color={indicator.status === 'never_run' ? 'info' : 'warning'}
                          size="small"
                          icon={<Warning sx={{ fontSize: '14px !important' }} />}
                          sx={{
                            fontWeight: 600,
                            animation:
                              indicator.status !== 'never_run' ? 'pulse 2s infinite' : 'none',
                            '@keyframes pulse': {
                              '0%': { opacity: 1 },
                              '50%': { opacity: 0.7 },
                              '100%': { opacity: 1 },
                            },
                          }}
                        />
                        <Chip
                          label="Active" // isActive not available in IndicatorStatusDto
                          size="small"
                          variant="outlined"
                          color="success"
                          sx={{
                            height: 20,
                            fontSize: '0.7rem',
                            fontWeight: 500,
                          }}
                        />
                      </Box>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
                          {indicator.indicatorName}
                        </Typography>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                            Status: {indicator.status}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Next run:{' '}
                            {indicator.nextDue
                              ? format(new Date(indicator.nextDue), 'MMM dd, HH:mm')
                              : 'Not scheduled'}
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
                  All Indicators are up to date
                </Typography>
                <Typography color="text.secondary" variant="caption">
                  No immediate action required
                </Typography>
              </Box>
            )}
          </Box>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default IndicatorsDueCard;
