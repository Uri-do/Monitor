import React, { useState } from 'react';
import { Box, Typography, Grid, Paper, Alert, CircularProgress } from '@mui/material';
import { useActiveCollectors, useCollectorItemNames } from '../hooks/useMonitorStatistics';
import { CollectorSelector } from '../components/CollectorSelector';

/**
 * Test page for Monitor Statistics integration
 * This page demonstrates the new collector selection functionality
 */
const MonitorStatisticsTest: React.FC = () => {
  const [selectedCollectorId, setSelectedCollectorId] = useState<number | undefined>();
  const [selectedItemName, setSelectedItemName] = useState<string>('');

  const {
    data: collectors,
    isLoading: collectorsLoading,
    error: collectorsError,
  } = useActiveCollectors();

  const {
    data: itemNames,
    isLoading: itemNamesLoading,
    error: itemNamesError,
  } = useCollectorItemNames(selectedCollectorId || 0);

  const handleCollectorChange = (collectorId: number | undefined) => {
    setSelectedCollectorId(collectorId || undefined);
    setSelectedItemName(''); // Reset item name when collector changes
  };

  const handleItemNameChange = (itemName: string) => {
    setSelectedItemName(itemName);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Monitor Statistics Integration Test
      </Typography>
      
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        This page tests the new Monitor Statistics API integration with the ProgressPlayDBTest database.
      </Typography>

      <Grid container spacing={3}>
        {/* Collector Selection Test */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Collector Selection Component
            </Typography>
            
            <CollectorSelector
              selectedCollectorId={selectedCollectorId}
              selectedItemName={selectedItemName}
              onCollectorChange={handleCollectorChange}
              onItemNameChange={handleItemNameChange}
            />
            
            {/* Selection Summary */}
            <Box sx={{ mt: 3, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
              <Typography variant="subtitle2" gutterBottom>
                Current Selection:
              </Typography>
              <Typography variant="body2">
                <strong>Collector ID:</strong> {selectedCollectorId || 'None'}
              </Typography>
              <Typography variant="body2">
                <strong>Item Name:</strong> {selectedItemName || 'None'}
              </Typography>
            </Box>
          </Paper>
        </Grid>

        {/* API Status */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              API Status
            </Typography>
            
            {/* Collectors Status */}
            <Box sx={{ mb: 2 }}>
              <Typography variant="subtitle2">Collectors API:</Typography>
              {collectorsLoading && (
                <Box sx={{ display: 'flex', alignItems: 'center', mt: 1 }}>
                  <CircularProgress size={16} sx={{ mr: 1 }} />
                  <Typography variant="body2">Loading collectors...</Typography>
                </Box>
              )}
              {collectorsError && (
                <Alert severity="error" sx={{ mt: 1 }}>
                  Error: {collectorsError.message}
                </Alert>
              )}
              {collectors && !collectorsLoading && (
                <Alert severity="success" sx={{ mt: 1 }}>
                  Successfully loaded {collectors.length} collectors
                </Alert>
              )}
            </Box>

            {/* Item Names Status */}
            {selectedCollectorId && (
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2">Item Names API:</Typography>
                {itemNamesLoading && (
                  <Box sx={{ display: 'flex', alignItems: 'center', mt: 1 }}>
                    <CircularProgress size={16} sx={{ mr: 1 }} />
                    <Typography variant="body2">Loading item names...</Typography>
                  </Box>
                )}
                {itemNamesError && (
                  <Alert severity="error" sx={{ mt: 1 }}>
                    Error: {itemNamesError.message}
                  </Alert>
                )}
                {itemNames && !itemNamesLoading && (
                  <Alert severity="success" sx={{ mt: 1 }}>
                    Successfully loaded {itemNames.length} item names
                  </Alert>
                )}
              </Box>
            )}
          </Paper>
        </Grid>

        {/* Raw Data Display */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Raw API Data
            </Typography>
            
            {/* Collectors Data */}
            <Box sx={{ mb: 3 }}>
              <Typography variant="subtitle2" gutterBottom>
                Collectors Data:
              </Typography>
              <Box
                component="pre"
                sx={{
                  bgcolor: 'grey.100',
                  p: 2,
                  borderRadius: 1,
                  overflow: 'auto',
                  maxHeight: 200,
                  fontSize: '0.75rem',
                }}
              >
                {JSON.stringify(collectors, null, 2)}
              </Box>
            </Box>

            {/* Item Names Data */}
            {selectedCollectorId && itemNames && (
              <Box>
                <Typography variant="subtitle2" gutterBottom>
                  Item Names Data for Collector {selectedCollectorId}:
                </Typography>
                <Box
                  component="pre"
                  sx={{
                    bgcolor: 'grey.100',
                    p: 2,
                    borderRadius: 1,
                    overflow: 'auto',
                    maxHeight: 200,
                    fontSize: '0.75rem',
                  }}
                >
                  {JSON.stringify(itemNames, null, 2)}
                </Box>
              </Box>
            )}
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default MonitorStatisticsTest;
