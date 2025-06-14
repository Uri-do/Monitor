import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Divider,
} from '@mui/material';
import { BarChart as StatisticsIcon } from '@mui/icons-material';
import { PageHeader } from '@/components/UI';
import { StatsExplorer } from '@/components/StatsExplorer';
import { CollectorSelector } from '@/components/CollectorSelector';

const StatisticsPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [selectedCollectorId, setSelectedCollectorId] = useState<number | undefined>();
  const [selectedItemName, setSelectedItemName] = useState<string>('');

  // Initialize from URL parameters
  useEffect(() => {
    const collectorIdParam = searchParams.get('collectorId');
    const itemParam = searchParams.get('item');

    if (collectorIdParam) {
      const collectorId = parseInt(collectorIdParam, 10);
      if (!isNaN(collectorId)) {
        setSelectedCollectorId(collectorId);
      }
    }

    if (itemParam) {
      setSelectedItemName(decodeURIComponent(itemParam));
    }
  }, [searchParams]);

  return (
    <Box>
      <PageHeader
        title="Statistics & Analytics"
        subtitle="Browse and analyze collector statistics and performance data"
        icon={<StatisticsIcon />}
      />

      <Grid container spacing={3}>
        {/* Enhanced Collector Selector */}
        <Grid item xs={12} lg={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Data Source Selection
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Select a data collector to view detailed statistics and analytics.
              </Typography>

              <CollectorSelector
                selectedCollectorId={selectedCollectorId}
                selectedItemName={selectedItemName}
                onCollectorChange={setSelectedCollectorId}
                onItemNameChange={setSelectedItemName}
                variant="detailed"
                showRefreshButton
                showCollectorInfo
                showStatisticsButton={false}
              />
            </CardContent>
          </Card>
        </Grid>

        {/* Statistics Explorer */}
        <Grid item xs={12} lg={8}>
          <StatsExplorer
            initialCollectorId={selectedCollectorId}
            initialItemName={selectedItemName}
          />
        </Grid>
      </Grid>
    </Box>
  );
};

export default StatisticsPage;
