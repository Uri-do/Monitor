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
  useTheme,
} from '@mui/material';
import { Error } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { AlertDashboardDto } from '../../../types/api';
import { UltimateCard } from '@/components/UltimateEnterprise';

interface TopAlertingKpisCardProps {
  alertDashboard?: AlertDashboardDto;
}

const TopAlertingKpisCard: React.FC<TopAlertingKpisCardProps> = ({ alertDashboard }) => {
  const navigate = useNavigate();
  const theme = useTheme();

  if (!alertDashboard?.topAlertingKpis || alertDashboard.topAlertingKpis.length === 0) {
    return null;
  }

  return (
    <Grid item xs={12}>
      <UltimateCard gradient="error" glowEffect={true}>
        <CardContent>
          <Box display="flex" alignItems="center" gap={1} mb={3}>
            <Error sx={{ fontSize: 28 }} />
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              Top Alerting KPIs (Last 7 Days)
            </Typography>
          </Box>
          <List sx={{ p: 0 }}>
            {alertDashboard.topAlertingKpis.slice(0, 5).map(kpi => (
              <ListItem
                key={kpi.kpiId}
                button
                onClick={() => navigate(`/kpis/${kpi.kpiId}`)}
                sx={{
                  borderRadius: 2,
                  mb: 1,
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                  border: '1px solid rgba(255, 255, 255, 0.2)',
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.2)',
                    transform: 'translateX(4px)',
                    transition: 'all 0.2s ease-in-out',
                  },
                }}
              >
                <ListItemText
                  primary={
                    <Box display="flex" alignItems="center" gap={1} mb={1}>
                      <Typography variant="subtitle1" sx={{ fontWeight: 600, color: 'white' }}>
                        {kpi.indicator}
                      </Typography>
                      <Chip
                        label="Active"
                        size="small"
                        sx={{
                          backgroundColor: 'rgba(76, 175, 80, 0.3)',
                          color: 'white',
                          fontWeight: 600,
                          height: 20,
                          fontSize: '0.7rem',
                        }}
                      />
                    </Box>
                  }
                  secondary={
                    <Box display="flex" alignItems="center" gap={1} flexWrap="wrap">
                      <Typography variant="body2" sx={{ color: 'rgba(255, 255, 255, 0.8)' }}>
                        Owner: {kpi.owner}
                      </Typography>
                      <Chip
                        label={`${kpi.alertCount} alerts`}
                        size="small"
                        sx={{
                          backgroundColor: 'rgba(244, 67, 54, 0.3)',
                          color: 'white',
                          fontWeight: 600,
                        }}
                      />
                      {kpi.unresolvedCount > 0 && (
                        <Chip
                          label={`${kpi.unresolvedCount} unresolved`}
                          size="small"
                          sx={{
                            backgroundColor: 'rgba(255, 152, 0, 0.3)',
                            color: 'white',
                            fontWeight: 600,
                          }}
                        />
                      )}
                    </Box>
                  }
                />
              </ListItem>
            ))}
          </List>
        </CardContent>
      </UltimateCard>
    </Grid>
  );
};

export default TopAlertingKpisCard;
