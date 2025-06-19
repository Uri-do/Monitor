import React from 'react';
import {
  Box,
  Grid,
  Typography,
  CardContent,
} from '@mui/material';
import { Storage as DatabaseIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { Card, Button } from '@/components/UI';
import { IndicatorDto } from '@/types/api';
import { useCollector } from '@/hooks/useMonitorStatistics';
import { CollectorItemsExpander } from './CollectorItemsExpander';

interface DataSourceInfoProps {
  indicator: IndicatorDto;
}

/**
 * Component to display data source information prominently
 */
export const DataSourceInfo: React.FC<DataSourceInfoProps> = ({ indicator }) => {
  const navigate = useNavigate();
  const { data: collector } = useCollector(indicator?.collectorID || 0);

  return (
    <Card sx={{ mb: 3, border: '2px solid', borderColor: 'primary.main' }}>
      <CardContent sx={{ py: 3 }}>
        <Grid container spacing={3} alignItems="center">
          <Grid item xs={12} md={8}>
            <Box display="flex" alignItems="center" gap={2} mb={2}>
              <DatabaseIcon sx={{ fontSize: '2.5rem', color: 'primary.main' }} />
              <Box>
                <Typography variant="h4" sx={{ fontWeight: 600, mb: 0.5, color: 'primary.main' }}>
                  {collector?.displayName ||
                    collector?.collectorDesc ||
                    collector?.collectorCode ||
                    indicator.collectorName ||
                    'Unknown Collector'}
                </Typography>
                <Typography variant="h5" sx={{ fontWeight: 500, mb: 1, color: 'text.primary' }}>
                  Item: {indicator.collectorItemName || 'Not specified'}
                </Typography>
                <Typography variant="body1" color="text.secondary">
                  {indicator.lastMinutes || 0} minute intervals • Collector ID:{' '}
                  {indicator.collectorID || 'N/A'}
                </Typography>
              </Box>
            </Box>

            <Typography variant="body1" color="text.secondary">
              This indicator monitors <strong>{indicator.collectorItemName}</strong> from the{' '}
              <strong>
                {collector?.displayName ||
                  collector?.collectorDesc ||
                  collector?.collectorCode ||
                  indicator.collectorName ||
                  'collector'}
              </strong>{' '}
              data source
            </Typography>
          </Grid>

          <Grid item xs={12} md={4}>
            <Box>
              {/* Collector Items Panel */}
              {indicator.collectorID ? (
                <CollectorItemsExpander
                  collectorId={indicator.collectorID}
                  selectedItemName={indicator.collectorItemName}
                  showStats={true}
                />
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No collector configured
                </Typography>
              )}

              {/* Small link to general statistics */}
              <Box sx={{ mt: 2, textAlign: { xs: 'left', md: 'right' } }}>
                <Button
                  variant="text"
                  size="small"
                  onClick={() => {
                    if (indicator.collectorID) {
                      navigate(`/statistics?collectorId=${indicator.collectorID}`);
                    }
                  }}
                >
                  View All Statistics →
                </Button>
              </Box>
            </Box>
          </Grid>
        </Grid>
      </CardContent>
    </Card>
  );
};

export default DataSourceInfo;
