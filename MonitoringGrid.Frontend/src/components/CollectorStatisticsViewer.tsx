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
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Tabs,
  Tab,
  CircularProgress,
  Stack,
  Divider,
  SelectChangeEvent
} from '@mui/material';
import {
  BarChart,
  Bar,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import {
  BarChart as BarChartIcon,
  Timeline as TimelineIcon,
  PieChart as PieChartIcon,
  TableChart as TableIcon,
  Refresh as RefreshIcon,
  TrendingUp,
  TrendingDown,
  Analytics
} from '@mui/icons-material';
import { useCollectorStatistics, MonitorStatistics } from '../hooks/useMonitorStatistics';
import { MetricCard } from './UI';

interface CollectorStatisticsViewerProps {
  collectorId: number;
  collectorName?: string;
  selectedItemName?: string;
}

type ViewMode = 'total' | 'average' | 'hourly';
type ChartType = 'bar' | 'line' | 'pie' | 'table';
type ItemFilter = 'all' | 'selected';

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
      id={`statistics-tabpanel-${index}`}
      aria-labelledby={`statistics-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
};

export const CollectorStatisticsViewer: React.FC<CollectorStatisticsViewerProps> = ({
  collectorId,
  collectorName,
  selectedItemName,
}) => {
  const [viewMode, setViewMode] = useState<ViewMode>('total');
  const [chartType, setChartType] = useState<ChartType>('bar');
  const [daysBack, setDaysBack] = useState<number>(7);
  const [activeTab, setActiveTab] = useState(0);
  const [itemFilter, setItemFilter] = useState<ItemFilter>('all');

  // Calculate date range
  const fromDate = useMemo(() => {
    const date = new Date();
    date.setDate(date.getDate() - daysBack);
    return date.toISOString().split('T')[0];
  }, [daysBack]);

  const toDate = useMemo(() => {
    return new Date().toISOString().split('T')[0];
  }, []);

  // Fetch statistics
  const {
    data: rawStatistics = [],
    isLoading,
    error,
    refetch,
  } = useCollectorStatistics(collectorId, {
    fromDate,
    toDate,
  });

  // Filter by selected item based on itemFilter setting
  const filteredStatistics = useMemo(() => {
    if (itemFilter === 'all') {
      // Show all items for the collector
      return rawStatistics;
    }
    if (itemFilter === 'selected' && selectedItemName) {
      // Filter by specific item
      return rawStatistics.filter(stat => stat.itemName === selectedItemName);
    }
    // If no item selected but filter is 'selected', show all
    return rawStatistics;
  }, [rawStatistics, selectedItemName, itemFilter]);

  // Process data based on view mode
  const processedData = useMemo(() => {
    if (!filteredStatistics.length) return [];

    switch (viewMode) {
      case 'total':
        // Group by day and sum totals
        const dailyTotals = filteredStatistics.reduce((acc, stat) => {
          const day = stat.day;
          if (!acc[day]) {
            acc[day] = {
              day,
              total: 0,
              marked: 0,
              markedPercent: 0,
              count: 0,
            };
          }
          acc[day].total += stat.total || 0;
          acc[day].marked += stat.marked || 0;
          acc[day].count += 1;
          return acc;
        }, {} as Record<string, any>);

        return Object.values(dailyTotals).map((item: any) => ({
          ...item,
          markedPercent: item.total > 0 ? (item.marked / item.total) * 100 : 0,
          displayTime: new Date(item.day).toLocaleDateString(),
        }));

      case 'average':
        // Group by day and calculate averages
        const dailyAverages = filteredStatistics.reduce((acc, stat) => {
          const day = stat.day;
          if (!acc[day]) {
            acc[day] = {
              day,
              totalSum: 0,
              markedSum: 0,
              count: 0,
            };
          }
          acc[day].totalSum += stat.total || 0;
          acc[day].markedSum += stat.marked || 0;
          acc[day].count += 1;
          return acc;
        }, {} as Record<string, any>);

        return Object.values(dailyAverages).map((item: any) => ({
          day: item.day,
          total: item.count > 0 ? item.totalSum / item.count : 0,
          marked: item.count > 0 ? item.markedSum / item.count : 0,
          markedPercent: item.totalSum > 0 ? (item.markedSum / item.totalSum) * 100 : 0,
          displayTime: new Date(item.day).toLocaleDateString(),
        }));

      case 'hourly':
        // Show hourly data for the most recent day
        const latestDay = filteredStatistics.reduce((latest, stat) => {
          return stat.day > latest ? stat.day : latest;
        }, '');

        return filteredStatistics
          .filter(stat => stat.day === latestDay)
          .map(stat => ({
            ...stat,
            displayTime: `${stat.hour.toString().padStart(2, '0')}:00`,
          }))
          .sort((a, b) => a.hour - b.hour);

      default:
        return filteredStatistics;
    }
  }, [filteredStatistics, viewMode]);

  // Calculate summary metrics
  const summaryMetrics = useMemo(() => {
    if (!processedData.length) {
      return {
        totalRecords: 0,
        avgTotal: 0,
        avgMarked: 0,
        avgMarkedPercent: 0,
        maxTotal: 0,
        minTotal: 0,
        uniqueItems: 0,
      };
    }

    const totals = processedData.map(d => d.total || 0);
    const marked = processedData.map(d => d.marked || 0);
    const markedPercents = processedData.map(d => d.markedPercent || 0);

    // Count unique items in the filtered data
    const uniqueItems = new Set(
      filteredStatistics
        .map(stat => stat.itemName)
        .filter(name => name && name.trim() !== '')
    ).size;

    return {
      totalRecords: processedData.length,
      avgTotal: totals.reduce((a, b) => a + b, 0) / totals.length,
      avgMarked: marked.reduce((a, b) => a + b, 0) / marked.length,
      avgMarkedPercent: markedPercents.reduce((a, b) => a + b, 0) / markedPercents.length,
      maxTotal: Math.max(...totals),
      minTotal: Math.min(...totals),
      uniqueItems,
    };
  }, [processedData, filteredStatistics]);

  // Calculate items breakdown when showing all items
  const itemsBreakdown = useMemo(() => {
    if (itemFilter !== 'all' || !filteredStatistics.length) return [];

    const itemStats = filteredStatistics.reduce((acc, stat) => {
      const itemName = stat.itemName || 'Unknown';
      if (!acc[itemName]) {
        acc[itemName] = {
          itemName,
          totalSum: 0,
          markedSum: 0,
          recordCount: 0,
        };
      }
      acc[itemName].totalSum += stat.total || 0;
      acc[itemName].markedSum += stat.marked || 0;
      acc[itemName].recordCount += 1;
      return acc;
    }, {} as Record<string, any>);

    return Object.values(itemStats)
      .map((item: any) => ({
        ...item,
        avgTotal: item.recordCount > 0 ? item.totalSum / item.recordCount : 0,
        avgMarked: item.recordCount > 0 ? item.markedSum / item.recordCount : 0,
        markedPercent: item.totalSum > 0 ? (item.markedSum / item.totalSum) * 100 : 0,
      }))
      .sort((a, b) => b.totalSum - a.totalSum); // Sort by total sum descending
  }, [filteredStatistics, itemFilter]);

  const handleViewModeChange = (event: SelectChangeEvent<ViewMode>) => {
    setViewMode(event.target.value as ViewMode);
  };

  const handleChartTypeChange = (event: SelectChangeEvent<ChartType>) => {
    setChartType(event.target.value as ChartType);
  };

  const handleDaysBackChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setDaysBack(parseInt(event.target.value, 10));
  };

  const handleItemFilterChange = (event: SelectChangeEvent<ItemFilter>) => {
    setItemFilter(event.target.value as ItemFilter);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  if (error) {
    return (
      <Alert severity="error" sx={{ mt: 2 }}>
        Failed to load statistics: {error.message}
      </Alert>
    );
  }

  return (
    <Card sx={{ mt: 2 }}>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Analytics color="primary" />
            <Typography variant="h6">
              Statistics: {collectorName}
              {itemFilter === 'all' ? (
                <Chip
                  label="All Items"
                  size="small"
                  color="success"
                  sx={{ ml: 1 }}
                />
              ) : selectedItemName ? (
                <Chip
                  label={selectedItemName}
                  size="small"
                  color="primary"
                  sx={{ ml: 1 }}
                />
              ) : (
                <Chip
                  label="No Item Selected"
                  size="small"
                  color="default"
                  sx={{ ml: 1 }}
                />
              )}
            </Typography>
          </Box>
          <Button
            startIcon={<RefreshIcon />}
            onClick={() => refetch()}
            disabled={isLoading}
            size="small"
          >
            Refresh
          </Button>
        </Box>

        {/* Controls */}
        <Grid container spacing={2} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={2.4}>
            <FormControl fullWidth size="small">
              <InputLabel>View Mode</InputLabel>
              <Select
                value={viewMode}
                onChange={handleViewModeChange}
                label="View Mode"
              >
                <MenuItem value="total">Daily Totals</MenuItem>
                <MenuItem value="average">Daily Averages</MenuItem>
                <MenuItem value="hourly">Hourly (Latest Day)</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6} md={2.4}>
            <FormControl fullWidth size="small">
              <InputLabel>Chart Type</InputLabel>
              <Select
                value={chartType}
                onChange={handleChartTypeChange}
                label="Chart Type"
              >
                <MenuItem value="bar">Bar Chart</MenuItem>
                <MenuItem value="line">Line Chart</MenuItem>
                <MenuItem value="pie">Pie Chart</MenuItem>
                <MenuItem value="table">Data Table</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6} md={2.4}>
            <FormControl fullWidth size="small">
              <InputLabel>Items</InputLabel>
              <Select
                value={itemFilter}
                onChange={handleItemFilterChange}
                label="Items"
              >
                <MenuItem value="all">All Items</MenuItem>
                <MenuItem value="selected" disabled={!selectedItemName}>
                  Selected Item Only
                  {!selectedItemName && " (None Selected)"}
                </MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6} md={2.4}>
            <TextField
              fullWidth
              size="small"
              label="Days Back"
              type="number"
              value={daysBack}
              onChange={handleDaysBackChange}
              inputProps={{ min: 1, max: 90 }}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={2.4}>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
              From: {fromDate} to {toDate}
            </Typography>
          </Grid>
        </Grid>

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <>
            {/* Summary Metrics */}
            <Grid container spacing={2} sx={{ mb: 3 }}>
              <Grid item xs={6} sm={2.4}>
                <MetricCard
                  title="Records"
                  value={summaryMetrics.totalRecords.toString()}
                  size="small"
                />
              </Grid>
              <Grid item xs={6} sm={2.4}>
                <MetricCard
                  title="Unique Items"
                  value={summaryMetrics.uniqueItems.toString()}
                  size="small"
                  chip={itemFilter === 'all' ? { label: 'All', color: 'success' } : undefined}
                />
              </Grid>
              <Grid item xs={6} sm={2.4}>
                <MetricCard
                  title="Avg Total"
                  value={summaryMetrics.avgTotal.toFixed(1)}
                  size="small"
                />
              </Grid>
              <Grid item xs={6} sm={2.4}>
                <MetricCard
                  title="Avg Marked"
                  value={summaryMetrics.avgMarked.toFixed(1)}
                  size="small"
                />
              </Grid>
              <Grid item xs={6} sm={2.4}>
                <MetricCard
                  title="Avg Marked %"
                  value={`${summaryMetrics.avgMarkedPercent.toFixed(1)}%`}
                  size="small"
                />
              </Grid>
            </Grid>

            <Divider sx={{ mb: 3 }} />

            {/* Tabs for different views */}
            <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
              <Tabs value={activeTab} onChange={handleTabChange}>
                <Tab icon={<BarChartIcon />} label="Chart" />
                <Tab icon={<TableIcon />} label="Data Table" />
                {itemFilter === 'all' && (
                  <Tab icon={<PieChartIcon />} label="Items Breakdown" />
                )}
              </Tabs>
            </Box>

            <TabPanel value={activeTab} index={0}>
              {/* Chart View */}
              <Box sx={{ height: 400 }}>
                {chartType === 'bar' && (
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={processedData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="displayTime" />
                      <YAxis />
                      <Tooltip />
                      <Legend />
                      <Bar dataKey="total" fill="#2196f3" name="Total" />
                      <Bar dataKey="marked" fill="#f44336" name="Marked" />
                    </BarChart>
                  </ResponsiveContainer>
                )}
                {chartType === 'line' && (
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={processedData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="displayTime" />
                      <YAxis />
                      <Tooltip />
                      <Legend />
                      <Line type="monotone" dataKey="total" stroke="#2196f3" name="Total" />
                      <Line type="monotone" dataKey="marked" stroke="#f44336" name="Marked" />
                      <Line type="monotone" dataKey="markedPercent" stroke="#4caf50" name="Marked %" />
                    </LineChart>
                  </ResponsiveContainer>
                )}
                {chartType === 'pie' && processedData.length > 0 && (
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={processedData.map(d => ({
                          name: d.displayTime,
                          value: d.total || 0,
                        }))}
                        cx="50%"
                        cy="50%"
                        outerRadius={120}
                        fill="#2196f3"
                        dataKey="value"
                        label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                      >
                        {processedData.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={`hsl(${index * 45}, 70%, 50%)`} />
                        ))}
                      </Pie>
                      <Tooltip />
                    </PieChart>
                  </ResponsiveContainer>
                )}
              </Box>
            </TabPanel>

            <TabPanel value={activeTab} index={1}>
              {/* Data Table */}
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Time</TableCell>
                      <TableCell align="right">Total</TableCell>
                      <TableCell align="right">Marked</TableCell>
                      <TableCell align="right">Marked %</TableCell>
                      {viewMode === 'hourly' && <TableCell align="right">Hour</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {processedData.map((row, index) => (
                      <TableRow key={index}>
                        <TableCell>{row.displayTime}</TableCell>
                        <TableCell align="right">{(row.total || 0).toFixed(1)}</TableCell>
                        <TableCell align="right">{(row.marked || 0).toFixed(1)}</TableCell>
                        <TableCell align="right">{(row.markedPercent || 0).toFixed(1)}%</TableCell>
                        {viewMode === 'hourly' && <TableCell align="right">{row.hour}</TableCell>}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </TabPanel>

            {/* Items Breakdown Tab - Only shown when viewing all items */}
            {itemFilter === 'all' && (
              <TabPanel value={activeTab} index={2}>
                <Typography variant="h6" gutterBottom>
                  Items Breakdown
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  Statistics aggregated by item name across the selected time period
                </Typography>

                <TableContainer component={Paper} variant="outlined">
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Item Name</TableCell>
                        <TableCell align="right">Records</TableCell>
                        <TableCell align="right">Total Sum</TableCell>
                        <TableCell align="right">Marked Sum</TableCell>
                        <TableCell align="right">Avg Total</TableCell>
                        <TableCell align="right">Avg Marked</TableCell>
                        <TableCell align="right">Marked %</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {itemsBreakdown.map((item, index) => (
                        <TableRow key={index}>
                          <TableCell>
                            <Typography variant="body2" fontWeight="medium">
                              {item.itemName}
                            </Typography>
                          </TableCell>
                          <TableCell align="right">{item.recordCount}</TableCell>
                          <TableCell align="right">{item.totalSum.toFixed(1)}</TableCell>
                          <TableCell align="right">{item.markedSum.toFixed(1)}</TableCell>
                          <TableCell align="right">{item.avgTotal.toFixed(1)}</TableCell>
                          <TableCell align="right">{item.avgMarked.toFixed(1)}</TableCell>
                          <TableCell align="right">
                            <Chip
                              label={`${item.markedPercent.toFixed(1)}%`}
                              size="small"
                              color={item.markedPercent > 50 ? 'warning' : 'default'}
                            />
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>

                {itemsBreakdown.length === 0 && (
                  <Alert severity="info" sx={{ mt: 2 }}>
                    No items found in the selected data.
                  </Alert>
                )}
              </TabPanel>
            )}

            {processedData.length === 0 && (
              <Alert severity="info" sx={{ mt: 2 }}>
                No statistics data found for the selected criteria.
              </Alert>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
};

export default CollectorStatisticsViewer;
