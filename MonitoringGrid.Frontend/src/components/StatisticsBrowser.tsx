import React, { useState, useMemo } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Typography,
  Chip,
  Card,
  CardContent,
  Tabs,
  Tab,
  Alert,
  CircularProgress,
  IconButton,
  Tooltip,
  Divider,
  SelectChangeEvent,
  Paper,
  List,
  ListItem,
  ListItemText,
  ListItemButton,
  ListItemIcon,
} from '@mui/material';
import {
  Close as CloseIcon,
  Analytics as AnalyticsIcon,
  BarChart as BarChartIcon,
  Timeline as TimelineIcon,
  PieChart as PieChartIcon,
  TableChart as TableIcon,
  Refresh as RefreshIcon,
  FilterList as FilterIcon,
  TrendingUp,
  Assessment,
  DataUsage,
} from '@mui/icons-material';
import { 
  useActiveCollectors, 
  useCollectorItemNames, 
  useCollectorStatistics,
  MonitorStatisticsCollector 
} from '../hooks/useMonitorStatistics';
import CollectorStatisticsViewer from './CollectorStatisticsViewer';

interface StatisticsBrowserProps {
  open: boolean;
  onClose: () => void;
  initialCollectorId?: number;
  initialItemName?: string;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index, ...other }) => {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`statistics-browser-tabpanel-${index}`}
      aria-labelledby={`statistics-browser-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 2 }}>{children}</Box>}
    </div>
  );
};

export const StatisticsBrowser: React.FC<StatisticsBrowserProps> = ({
  open,
  onClose,
  initialCollectorId,
  initialItemName,
}) => {
  const [selectedCollectorId, setSelectedCollectorId] = useState<number | undefined>(initialCollectorId);
  const [selectedItemName, setSelectedItemName] = useState<string>(initialItemName || '');
  const [activeTab, setActiveTab] = useState(0);
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

  // Filter collectors based on search
  const filteredCollectors = useMemo(() => {
    if (!searchTerm) return collectors;
    return collectors.filter(collector =>
      collector.displayName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      collector.collectorCode?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      collector.collectorDesc?.toLowerCase().includes(searchTerm.toLowerCase())
    );
  }, [collectors, searchTerm]);

  // Get selected collector details
  const selectedCollector = useMemo(() => {
    return collectors.find(c => c.collectorID === selectedCollectorId);
  }, [collectors, selectedCollectorId]);

  // Calculate collector summary stats
  const collectorSummary = useMemo(() => {
    if (!collectors.length) return { total: 0, active: 0, withData: 0 };
    
    return {
      total: collectors.length,
      active: collectors.filter(c => c.isActiveStatus).length,
      withData: collectors.filter(c => c.statisticsCount > 0).length,
    };
  }, [collectors]);

  const handleCollectorSelect = (collector: MonitorStatisticsCollector) => {
    setSelectedCollectorId(collector.collectorID);
    setSelectedItemName(''); // Reset item selection
    setActiveTab(1); // Switch to statistics tab
  };

  const handleItemNameChange = (event: SelectChangeEvent<string>) => {
    setSelectedItemName(event.target.value);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleClose = () => {
    onClose();
  };

  const handleRefresh = () => {
    refetchCollectors();
  };

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="xl"
      fullWidth
      PaperProps={{
        sx: { height: '90vh', maxHeight: '90vh' }
      }}
    >
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <AnalyticsIcon color="primary" />
            <Typography variant="h6">Statistics Browser</Typography>
            {selectedCollector && (
              <Chip 
                label={selectedCollector.displayName} 
                color="primary" 
                size="small" 
              />
            )}
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Tooltip title="Refresh Data">
              <IconButton onClick={handleRefresh} disabled={collectorsLoading}>
                <RefreshIcon />
              </IconButton>
            </Tooltip>
            <IconButton onClick={handleClose}>
              <CloseIcon />
            </IconButton>
          </Box>
        </Box>
      </DialogTitle>

      <DialogContent sx={{ p: 0 }}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={activeTab} onChange={handleTabChange}>
            <Tab 
              icon={<FilterIcon />} 
              label="Browse Collectors" 
              id="statistics-browser-tab-0"
            />
            <Tab 
              icon={<BarChartIcon />} 
              label="Statistics View" 
              id="statistics-browser-tab-1"
              disabled={!selectedCollectorId}
            />
          </Tabs>
        </Box>

        <TabPanel value={activeTab} index={0}>
          <Box sx={{ p: 3 }}>
            {/* Summary Cards */}
            <Grid container spacing={2} sx={{ mb: 3 }}>
              <Grid item xs={12} sm={4}>
                <Card variant="outlined">
                  <CardContent sx={{ textAlign: 'center', py: 2 }}>
                    <Assessment color="primary" sx={{ fontSize: 32, mb: 1 }} />
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
                    <TrendingUp color="success" sx={{ fontSize: 32, mb: 1 }} />
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
                    <DataUsage color="info" sx={{ fontSize: 32, mb: 1 }} />
                    <Typography variant="h4">{collectorSummary.withData}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      With Statistics
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>

            <Divider sx={{ mb: 3 }} />

            {/* Search and Filter */}
            <Box sx={{ mb: 3 }}>
              <TextField
                fullWidth
                size="small"
                label="Search Collectors"
                placeholder="Search by name, code, or description..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                InputProps={{
                  startAdornment: <FilterIcon sx={{ mr: 1, color: 'text.secondary' }} />,
                }}
              />
            </Box>

            {/* Collectors List */}
            {collectorsLoading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                <CircularProgress />
              </Box>
            ) : collectorsError ? (
              <Alert severity="error">
                Failed to load collectors: {collectorsError.message}
              </Alert>
            ) : (
              <Paper variant="outlined" sx={{ maxHeight: 400, overflow: 'auto' }}>
                <List>
                  {filteredCollectors.map((collector) => (
                    <ListItem key={collector.collectorID} disablePadding>
                      <ListItemButton
                        onClick={() => handleCollectorSelect(collector)}
                        selected={selectedCollectorId === collector.collectorID}
                      >
                        <ListItemIcon>
                          <BarChartIcon 
                            color={collector.isActiveStatus ? 'primary' : 'disabled'} 
                          />
                        </ListItemIcon>
                        <ListItemText
                          primary={
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Typography variant="body1" fontWeight="medium">
                                {collector.displayName}
                              </Typography>
                              <Chip
                                label={collector.isActiveStatus ? 'Active' : 'Inactive'}
                                size="small"
                                color={collector.isActiveStatus ? 'success' : 'default'}
                              />
                              {collector.statisticsCount > 0 && (
                                <Chip
                                  label={`${collector.statisticsCount} records`}
                                  size="small"
                                  variant="outlined"
                                />
                              )}
                            </Box>
                          }
                          secondary={
                            <Box>
                              <Typography variant="caption" color="text.secondary">
                                ID: {collector.collectorID} • {collector.frequencyDisplay}
                              </Typography>
                              {collector.collectorDesc && (
                                <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                                  {collector.collectorDesc}
                                </Typography>
                              )}
                              <Typography variant="caption" color="text.secondary">
                                Last Run: {collector.lastRunDisplay}
                              </Typography>
                            </Box>
                          }
                        />
                      </ListItemButton>
                    </ListItem>
                  ))}
                </List>
                
                {filteredCollectors.length === 0 && (
                  <Box sx={{ p: 3, textAlign: 'center' }}>
                    <Typography color="text.secondary">
                      {searchTerm ? 'No collectors match your search.' : 'No collectors found.'}
                    </Typography>
                  </Box>
                )}
              </Paper>
            )}
          </Box>
        </TabPanel>

        <TabPanel value={activeTab} index={1}>
          <Box sx={{ p: 3 }}>
            {selectedCollectorId ? (
              <>
                {/* Item Selection */}
                <Box sx={{ mb: 3 }}>
                  <Grid container spacing={2} alignItems="center">
                    <Grid item xs={12} sm={6}>
                      <FormControl fullWidth size="small">
                        <InputLabel>Item Name (Optional)</InputLabel>
                        <Select
                          value={selectedItemName}
                          onChange={handleItemNameChange}
                          label="Item Name (Optional)"
                          disabled={itemNamesLoading}
                        >
                          <MenuItem value="">
                            <em>All Items</em>
                          </MenuItem>
                          {itemNames.map((itemName) => (
                            <MenuItem key={itemName} value={itemName}>
                              {itemName}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      {itemNamesLoading && (
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <CircularProgress size={16} />
                          <Typography variant="caption">Loading items...</Typography>
                        </Box>
                      )}
                      {itemNamesError && (
                        <Alert severity="warning" sx={{ py: 0.5 }}>
                          Failed to load item names
                        </Alert>
                      )}
                      {itemNames.length > 0 && !itemNamesLoading && (
                        <Typography variant="caption" color="text.secondary">
                          {itemNames.length} items available
                        </Typography>
                      )}
                    </Grid>
                  </Grid>
                </Box>

                {/* Statistics Viewer */}
                <CollectorStatisticsViewer
                  collectorId={selectedCollectorId}
                  collectorName={selectedCollector?.displayName}
                  selectedItemName={selectedItemName || undefined}
                />
              </>
            ) : (
              <Box sx={{ textAlign: 'center', py: 4 }}>
                <AnalyticsIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" color="text.secondary" gutterBottom>
                  Select a Collector
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Choose a collector from the Browse tab to view its statistics
                </Typography>
              </Box>
            )}
          </Box>
        </TabPanel>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={handleClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
};

export default StatisticsBrowser;
