import React, { useState, useMemo } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Button,
  Alert,
  Chip,
  CircularProgress,
  SelectChangeEvent,
  Divider,
} from '@mui/material';
import {
  Analytics as AnalyticsIcon,
  Refresh as RefreshIcon,
  FilterList as FilterIcon,
  Search as SearchIcon,
} from '@mui/icons-material';
import {
  useActiveCollectors,
  useCollectorItemNames,
  MonitorStatisticsCollector,
} from '@/hooks/useMonitorStatistics';
import CollectorStatisticsViewer from './CollectorStatisticsViewer';

interface StatsExplorerProps {
  initialCollectorId?: number;
  initialItemName?: string;
}

export const StatsExplorer: React.FC<StatsExplorerProps> = ({
  initialCollectorId,
  initialItemName,
}) => {
  const [selectedCollectorId, setSelectedCollectorId] = useState<number | undefined>(
    initialCollectorId
  );
  const [selectedItemName, setSelectedItemName] = useState<string>(initialItemName || '');
  const [searchTerm, setSearchTerm] = useState('');

  // Fetch collectors
  const {
    data: collectors = [],
    isLoading: collectorsLoading,
    error: collectorsError,
    refetch: refetchCollectors,
  } = useActiveCollectors();

  // Fetch item names for selected collector
  const {
    data: itemNames = [],
    isLoading: itemNamesLoading,
    error: itemNamesError,
  } = useCollectorItemNames(selectedCollectorId || 0);

  // Find selected collector
  const selectedCollector = useMemo(() => {
    return collectors.find(c => c.collectorID === selectedCollectorId);
  }, [collectors, selectedCollectorId]);

  // Filter collectors based on search term
  const filteredCollectors = useMemo(() => {
    if (!searchTerm.trim()) return collectors;

    const term = searchTerm.toLowerCase();
    return collectors.filter(
      collector =>
        collector.displayName.toLowerCase().includes(term) ||
        (collector.collectorCode && collector.collectorCode.toLowerCase().includes(term)) ||
        (collector.collectorDesc && collector.collectorDesc.toLowerCase().includes(term))
    );
  }, [collectors, searchTerm]);

  // Calculate collector summary stats
  const collectorSummary = useMemo(() => {
    if (!collectors.length) return { total: 0, active: 0, withData: 0 };

    return {
      total: collectors.length,
      active: collectors.filter(c => c.isActiveStatus).length,
      withData: collectors.filter(c => c.statisticsCount > 0).length,
    };
  }, [collectors]);

  const handleCollectorSelect = (event: SelectChangeEvent<number>) => {
    const collectorId = event.target.value as number;
    setSelectedCollectorId(collectorId);
    setSelectedItemName(''); // Reset item selection when collector changes
  };

  const handleItemNameChange = (event: SelectChangeEvent<string>) => {
    setSelectedItemName(event.target.value);
  };

  const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value);
  };

  if (collectorsError) {
    return (
      <Alert severity="error" sx={{ mt: 2 }}>
        Failed to load collectors: {collectorsError.message}
      </Alert>
    );
  }

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <AnalyticsIcon color="primary" />
          <Typography variant="h5">Statistics Explorer</Typography>
        </Box>
        <Button
          startIcon={<RefreshIcon />}
          onClick={() => refetchCollectors()}
          disabled={collectorsLoading}
          size="small"
        >
          Refresh
        </Button>
      </Box>

      {/* Summary Cards */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={4}>
          <Card variant="outlined">
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <AnalyticsIcon color="primary" sx={{ fontSize: 32, mb: 1 }} />
              <Typography variant="h4">{collectorSummary.total}</Typography>
              <Typography variant="body2" color="text.secondary">
                Total Collectors
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={4}>
          <Card variant="outlined">
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <FilterIcon color="success" sx={{ fontSize: 32, mb: 1 }} />
              <Typography variant="h4">{collectorSummary.active}</Typography>
              <Typography variant="body2" color="text.secondary">
                Active Collectors
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={4}>
          <Card variant="outlined">
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <SearchIcon color="info" sx={{ fontSize: 32, mb: 1 }} />
              <Typography variant="h4">{collectorSummary.withData}</Typography>
              <Typography variant="body2" color="text.secondary">
                With Statistics
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Collector Selection */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Select Collector & Item
          </Typography>

          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth size="small">
                <InputLabel>Collector</InputLabel>
                <Select
                  value={selectedCollectorId || ''}
                  onChange={handleCollectorSelect}
                  label="Collector"
                  disabled={collectorsLoading}
                >
                  <MenuItem value="">
                    <em>Select a collector</em>
                  </MenuItem>
                  {filteredCollectors.map(collector => (
                    <MenuItem key={collector.collectorID} value={collector.collectorID}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2">{collector.displayName}</Typography>
                        {collector.statisticsCount > 0 && (
                          <Chip
                            label={`${collector.statisticsCount} records`}
                            size="small"
                            color="primary"
                            variant="outlined"
                          />
                        )}
                        {!collector.isActiveStatus && (
                          <Chip label="Inactive" size="small" color="warning" variant="outlined" />
                        )}
                      </Box>
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>

            <Grid item xs={12} sm={6}>
              <FormControl fullWidth size="small">
                <InputLabel>Item Name (Optional)</InputLabel>
                <Select
                  value={selectedItemName}
                  onChange={handleItemNameChange}
                  label="Item Name (Optional)"
                  disabled={itemNamesLoading || !selectedCollectorId}
                >
                  <MenuItem value="">
                    <em>All Items</em>
                  </MenuItem>
                  {itemNames.map(itemName => (
                    <MenuItem key={itemName} value={itemName}>
                      {itemName}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
          </Grid>

          {itemNamesLoading && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
              <CircularProgress size={16} />
              <Typography variant="caption">Loading items...</Typography>
            </Box>
          )}

          {itemNamesError && (
            <Alert severity="warning" sx={{ mt: 2 }}>
              Failed to load item names
            </Alert>
          )}

          {itemNames.length > 0 && !itemNamesLoading && (
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
              {itemNames.length} items available
            </Typography>
          )}
        </CardContent>
      </Card>

      {/* Statistics Viewer */}
      {selectedCollectorId ? (
        <CollectorStatisticsViewer
          collectorId={selectedCollectorId}
          collectorName={selectedCollector?.displayName}
          selectedItemName={selectedItemName || undefined}
        />
      ) : (
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 8 }}>
            <AnalyticsIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" color="text.secondary" gutterBottom>
              Select a Collector
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Choose a collector above to view its statistics and analytics
            </Typography>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};

export default StatsExplorer;
