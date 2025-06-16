import React, { useState, useMemo, useEffect } from 'react';
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
  SelectChangeEvent,
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
  Analytics,
} from '@mui/icons-material';
import { useCollectorStatistics, MonitorStatistics } from '@/hooks/useMonitorStatistics';
import { MetricCard } from './UI';

interface CollectorStatisticsViewerProps {
  collectorId: number;
  collectorName?: string;
  selectedItemName?: string;
}

type ViewMode = 'total' | 'average' | 'hourly';
type ChartType = 'bar' | 'line' | 'pie' | 'table';
type TimeMode = 'daily' | 'hourly' | 'timeseries';

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
  const [timeMode, setTimeMode] = useState<TimeMode>('daily');
  // Default to 30 days (last month) when an item is selected, 7 days when viewing all items
  const [daysBack, setDaysBack] = useState<number>(selectedItemName ? 30 : 7);
  const [activeTab, setActiveTab] = useState(0);
  // Track hidden items for the multi-line chart
  const [hiddenItems, setHiddenItems] = useState<Set<string>>(new Set());

  // Update daysBack and auto-show chart when selectedItemName changes
  useEffect(() => {
    setDaysBack(selectedItemName ? 30 : 7);
    // Auto-show chart when clearing item selection (going to "All Items" view)
    if (!selectedItemName) {
      setActiveTab(0); // Switch to Chart tab
      setChartType('bar'); // Default to bar chart for better item comparison
      setHiddenItems(new Set()); // Reset hidden items when switching views
    }
  }, [selectedItemName]);

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

  // Filter by selected item - if selectedItemName is provided, filter to that item only
  const filteredStatistics = useMemo(() => {
    if (selectedItemName) {
      // Filter by specific item
      return rawStatistics.filter(stat => stat.itemName === selectedItemName);
    }
    // Show all items for the collector
    return rawStatistics;
  }, [rawStatistics, selectedItemName]);

  // Process data based on view mode and whether an item is selected
  const processedData = useMemo(() => {
    if (!filteredStatistics.length) return [];

    // If no specific item is selected, show items breakdown as main data
    if (!selectedItemName) {
      // For "All Items" view, we can show either daily aggregates or hourly for latest day
      if (timeMode === 'hourly') {
        // Show hourly breakdown for the latest day across all items
        const latestDay = filteredStatistics.reduce((latest, stat) => {
          return stat.day > latest ? stat.day : latest;
        }, '');

        const hourlyData = filteredStatistics
          .filter(stat => stat.day === latestDay)
          .reduce(
            (acc, stat) => {
              const hour = stat.hour;
              if (!acc[hour]) {
                acc[hour] = {
                  hour,
                  total: 0,
                  marked: 0,
                  count: 0,
                };
              }
              acc[hour].total += stat.total || 0;
              acc[hour].marked += stat.marked || 0;
              acc[hour].count += 1;
              return acc;
            },
            {} as Record<number, any>
          );

        return Object.values(hourlyData)
          .map((item: any) => ({
            ...item,
            markedPercent: item.total > 0 ? (item.marked / item.total) * 100 : 0,
            displayTime: `${item.hour.toString().padStart(2, '0')}:00`,
          }))
          .sort((a, b) => a.hour - b.hour);
      } else {
        // Show items breakdown (default for "All Items")
        const itemStats = filteredStatistics.reduce(
          (acc, stat) => {
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
          },
          {} as Record<string, any>
        );

        return Object.values(itemStats)
          .map((item: any) => ({
            ...item,
            total: item.totalSum,
            marked: item.markedSum,
            avgTotal: item.recordCount > 0 ? item.totalSum / item.recordCount : 0,
            avgMarked: item.recordCount > 0 ? item.markedSum / item.recordCount : 0,
            markedPercent: item.totalSum > 0 ? (item.markedSum / item.totalSum) * 100 : 0,
            displayTime: item.itemName, // Use item name as display identifier
          }))
          .sort((a, b) => b.totalSum - a.totalSum); // Sort by total sum descending
      }
    }

    // If specific item is selected, show time-based data for that item
    if (timeMode === 'hourly') {
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
    } else {
      // Daily view modes
      switch (viewMode) {
        case 'total':
          // Group by day and sum totals
          const dailyTotals = filteredStatistics.reduce(
            (acc, stat) => {
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
            },
            {} as Record<string, any>
          );

          return Object.values(dailyTotals).map((item: any) => ({
            ...item,
            markedPercent: item.total > 0 ? (item.marked / item.total) * 100 : 0,
            displayTime: new Date(item.day).toLocaleDateString(),
          }));

        case 'average':
          // Group by day and calculate averages
          const dailyAverages = filteredStatistics.reduce(
            (acc, stat) => {
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
            },
            {} as Record<string, any>
          );

          return Object.values(dailyAverages).map((item: any) => ({
            day: item.day,
            total: item.count > 0 ? item.totalSum / item.count : 0,
            marked: item.count > 0 ? item.markedSum / item.count : 0,
            markedPercent: item.totalSum > 0 ? (item.markedSum / item.totalSum) * 100 : 0,
            displayTime: new Date(item.day).toLocaleDateString(),
          }));

        default:
          return filteredStatistics;
      }
    }
  }, [filteredStatistics, viewMode, selectedItemName, timeMode]);

  // Process data for multi-line time series chart (when viewing all items over time)
  const multiLineData = useMemo(() => {
    if (selectedItemName || !filteredStatistics.length) return [];

    // Group data by day and item
    const timeSeriesData = filteredStatistics.reduce(
      (acc, stat) => {
        const day = stat.day;
        const itemName = stat.itemName || 'Unknown';

        if (!acc[day]) {
          acc[day] = {
            day,
            displayTime: new Date(day).toLocaleDateString(),
          };
        }

        if (!acc[day][itemName]) {
          acc[day][itemName] = 0;
        }

        acc[day][itemName] += stat.total || 0;
        return acc;
      },
      {} as Record<string, any>
    );

    return Object.values(timeSeriesData).sort((a: any, b: any) => a.day.localeCompare(b.day));
  }, [filteredStatistics, selectedItemName]);

  // Get unique item names for the multi-line chart
  const uniqueItemNames = useMemo(() => {
    if (selectedItemName || !filteredStatistics.length) return [];

    const items = new Set(
      filteredStatistics
        .map(stat => stat.itemName || 'Unknown')
        .filter(name => name && name.trim() !== '')
    );

    return Array.from(items).sort();
  }, [filteredStatistics, selectedItemName]);

  // Toggle item visibility in multi-line chart
  const toggleItemVisibility = (itemName: string) => {
    setHiddenItems(prev => {
      const newSet = new Set(prev);
      if (newSet.has(itemName)) {
        newSet.delete(itemName);
      } else {
        newSet.add(itemName);
      }
      return newSet;
    });
  };

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
      filteredStatistics.map(stat => stat.itemName).filter(name => name && name.trim() !== '')
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

  const handleViewModeChange = (event: SelectChangeEvent<ViewMode>) => {
    setViewMode(event.target.value as ViewMode);
  };

  const handleChartTypeChange = (event: SelectChangeEvent<ChartType>) => {
    setChartType(event.target.value as ChartType);
  };

  const handleTimeModeChange = (event: SelectChangeEvent<TimeMode>) => {
    setTimeMode(event.target.value as TimeMode);
  };

  const handleDaysBackChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setDaysBack(parseInt(event.target.value, 10));
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
              {selectedItemName ? (
                <Chip label={selectedItemName} size="small" color="primary" sx={{ ml: 1 }} />
              ) : (
                <Chip label="All Items" size="small" color="success" sx={{ ml: 1 }} />
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
          {/* Time Mode Selector */}
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Time Mode</InputLabel>
              <Select value={timeMode} onChange={handleTimeModeChange} label="Time Mode">
                <MenuItem value="daily">
                  {selectedItemName ? 'Daily View' : 'Items Breakdown'}
                </MenuItem>
                <MenuItem value="hourly">Hourly (24h)</MenuItem>
                {!selectedItemName && (
                  <MenuItem value="timeseries">Time Series (All Items)</MenuItem>
                )}
              </Select>
            </FormControl>
          </Grid>

          {/* View Mode - Only for selected item in daily mode */}
          {selectedItemName && timeMode === 'daily' && (
            <Grid item xs={12} sm={6} md={3}>
              <FormControl fullWidth size="small">
                <InputLabel>View Mode</InputLabel>
                <Select value={viewMode} onChange={handleViewModeChange} label="View Mode">
                  <MenuItem value="total">Daily Totals</MenuItem>
                  <MenuItem value="average">Daily Averages</MenuItem>
                </Select>
              </FormControl>
            </Grid>
          )}

          {/* Chart Type */}
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Chart Type</InputLabel>
              <Select value={chartType} onChange={handleChartTypeChange} label="Chart Type">
                <MenuItem value="bar">Bar Chart</MenuItem>
                <MenuItem value="line">Line Chart</MenuItem>
                <MenuItem value="pie">Pie Chart</MenuItem>
                <MenuItem value="table">Data Table</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Days Back - Now available for both selected item and all items */}
          <Grid item xs={12} sm={6} md={3}>
            <TextField
              fullWidth
              size="small"
              label="Days Back"
              type="number"
              value={daysBack}
              onChange={handleDaysBackChange}
              inputProps={{ min: 1, max: 90 }}
              helperText={timeMode === 'hourly' ? 'Latest day only' : `${fromDate} to ${toDate}`}
            />
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
                  chip={!selectedItemName ? { label: 'All', color: 'success' } : undefined}
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
              </Tabs>
            </Box>

            <TabPanel value={activeTab} index={0}>
              {/* Chart View */}
              {timeMode === 'timeseries' && !selectedItemName ? (
                <>
                  {/* Item Visibility Controls */}
                  <Box sx={{ mb: 2 }}>
                    <Typography variant="subtitle2" gutterBottom>
                      Item Visibility (click to toggle):
                    </Typography>
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                      {(Array.isArray(uniqueItemNames) ? uniqueItemNames : []).map((itemName, index) => (
                        <Chip
                          key={itemName}
                          label={itemName}
                          onClick={() => toggleItemVisibility(itemName)}
                          color={hiddenItems.has(itemName) ? 'default' : 'primary'}
                          variant={hiddenItems.has(itemName) ? 'outlined' : 'filled'}
                          size="small"
                          sx={{
                            opacity: hiddenItems.has(itemName) ? 0.5 : 1,
                            '&:hover': { opacity: 0.8 },
                          }}
                        />
                      ))}
                    </Box>
                    <Typography
                      variant="caption"
                      color="text.secondary"
                      sx={{ mt: 1, display: 'block' }}
                    >
                      {hiddenItems.size > 0
                        ? `${hiddenItems.size} item(s) hidden`
                        : 'All items visible'}
                    </Typography>
                  </Box>

                  {/* Multi-line Time Series Chart */}
                  <Box sx={{ height: 400 }}>
                    <ResponsiveContainer width="100%" height="100%">
                      <LineChart data={multiLineData}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="displayTime" angle={-45} textAnchor="end" height={100} />
                        <YAxis />
                        <Tooltip />
                        <Legend />
                        {uniqueItemNames
                          .filter(itemName => !hiddenItems.has(itemName))
                          .map((itemName, index) => (
                            <Line
                              key={itemName}
                              type="monotone"
                              dataKey={itemName}
                              stroke={`hsl(${(index * 360) / uniqueItemNames.length}, 70%, 50%)`}
                              strokeWidth={2}
                              name={itemName}
                              connectNulls={false}
                            />
                          ))}
                      </LineChart>
                    </ResponsiveContainer>
                  </Box>
                </>
              ) : (
                <Box sx={{ height: 400 }}>
                  {chartType === 'bar' && (
                    <ResponsiveContainer width="100%" height="100%">
                      <BarChart data={processedData}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis
                          dataKey="displayTime"
                          angle={selectedItemName ? 0 : -45}
                          textAnchor={selectedItemName ? 'middle' : 'end'}
                          height={selectedItemName ? 60 : 100}
                        />
                        <YAxis />
                        <Tooltip />
                        <Legend />
                        <Bar dataKey="total" fill="#2196f3" name="Total" />
                        <Bar dataKey="marked" fill="#f44336" name="Marked" />
                      </BarChart>
                    </ResponsiveContainer>
                  )}
                  {chartType === 'line' &&
                    (selectedItemName || timeMode === 'hourly' ? (
                      <ResponsiveContainer width="100%" height="100%">
                        <LineChart data={processedData}>
                          <CartesianGrid strokeDasharray="3 3" />
                          <XAxis
                            dataKey="displayTime"
                            angle={timeMode === 'hourly' ? 0 : -45}
                            textAnchor={timeMode === 'hourly' ? 'middle' : 'end'}
                            height={timeMode === 'hourly' ? 60 : 100}
                          />
                          <YAxis />
                          <Tooltip />
                          <Legend />
                          <Line type="monotone" dataKey="total" stroke="#2196f3" name="Total" />
                          <Line type="monotone" dataKey="marked" stroke="#f44336" name="Marked" />
                          <Line
                            type="monotone"
                            dataKey="markedPercent"
                            stroke="#4caf50"
                            name="Marked %"
                          />
                        </LineChart>
                      </ResponsiveContainer>
                    ) : (
                      <Box
                        sx={{
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          height: '100%',
                        }}
                      >
                        <Typography variant="body2" color="text.secondary">
                          Line chart is available for specific items or hourly view
                        </Typography>
                      </Box>
                    ))}
                  {chartType === 'pie' && processedData.length > 0 && (
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={processedData.map(d => ({
                            name: selectedItemName ? d.displayTime : d.itemName,
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
              )}
            </TabPanel>

            <TabPanel value={activeTab} index={1}>
              {/* Data Table - For both item-specific and all-items views */}
              <Typography variant="h6" gutterBottom>
                {selectedItemName
                  ? timeMode === 'hourly'
                    ? 'Hourly Breakdown'
                    : 'Time Series Data'
                  : timeMode === 'hourly'
                    ? 'Hourly Aggregated Data'
                    : timeMode === 'timeseries'
                      ? 'Time Series Data (All Items)'
                      : 'Items Breakdown'}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                {selectedItemName
                  ? timeMode === 'hourly'
                    ? `Hourly statistics for ${selectedItemName} on the latest day`
                    : `${viewMode === 'total' ? 'Daily totals' : 'Daily averages'} for ${selectedItemName}`
                  : timeMode === 'hourly'
                    ? 'Statistics aggregated by hour across all items for the latest day'
                    : timeMode === 'timeseries'
                      ? 'Daily time series data showing each item as a separate column'
                      : 'Statistics aggregated by item name across the selected time period'}
              </Typography>

              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      {timeMode === 'timeseries' && !selectedItemName ? (
                        <>
                          <TableCell>Date</TableCell>
                          {uniqueItemNames
                            .filter(itemName => !hiddenItems.has(itemName))
                            .map(itemName => (
                              <TableCell key={itemName} align="right">
                                {itemName}
                              </TableCell>
                            ))}
                        </>
                      ) : (
                        <>
                          <TableCell>
                            {selectedItemName
                              ? 'Time'
                              : timeMode === 'hourly'
                                ? 'Hour'
                                : 'Item Name'}
                          </TableCell>
                          <TableCell align="right">Total</TableCell>
                          <TableCell align="right">Marked</TableCell>
                          <TableCell align="right">Marked %</TableCell>
                          {!selectedItemName && timeMode !== 'hourly' && (
                            <TableCell align="right">Records</TableCell>
                          )}
                          {!selectedItemName && timeMode !== 'hourly' && (
                            <TableCell align="right">Avg Total</TableCell>
                          )}
                          {!selectedItemName && timeMode !== 'hourly' && (
                            <TableCell align="right">Avg Marked</TableCell>
                          )}
                          {timeMode === 'hourly' && <TableCell align="right">Hour</TableCell>}
                        </>
                      )}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {timeMode === 'timeseries' && !selectedItemName
                      ? multiLineData.map((row, index) => (
                          <TableRow key={index}>
                            <TableCell>
                              <Typography variant="body2" fontWeight="medium">
                                {row.displayTime}
                              </Typography>
                            </TableCell>
                            {uniqueItemNames
                              .filter(itemName => !hiddenItems.has(itemName))
                              .map(itemName => (
                                <TableCell key={itemName} align="right">
                                  {(row[itemName] || 0).toFixed(1)}
                                </TableCell>
                              ))}
                          </TableRow>
                        ))
                      : processedData.map((row, index) => (
                          <TableRow key={index}>
                            <TableCell>
                              {selectedItemName ? (
                                row.displayTime
                              ) : timeMode === 'hourly' ? (
                                row.displayTime
                              ) : (
                                <Typography variant="body2" fontWeight="medium">
                                  {row.itemName}
                                </Typography>
                              )}
                            </TableCell>
                            <TableCell align="right">{(row.total || 0).toFixed(1)}</TableCell>
                            <TableCell align="right">{(row.marked || 0).toFixed(1)}</TableCell>
                            <TableCell align="right">
                              <Chip
                                label={`${(row.markedPercent || 0).toFixed(1)}%`}
                                size="small"
                                color={(row.markedPercent || 0) > 50 ? 'warning' : 'default'}
                              />
                            </TableCell>
                            {!selectedItemName && timeMode !== 'hourly' && (
                              <TableCell align="right">{row.recordCount}</TableCell>
                            )}
                            {!selectedItemName && timeMode !== 'hourly' && (
                              <TableCell align="right">{(row.avgTotal || 0).toFixed(1)}</TableCell>
                            )}
                            {!selectedItemName && timeMode !== 'hourly' && (
                              <TableCell align="right">{(row.avgMarked || 0).toFixed(1)}</TableCell>
                            )}
                            {timeMode === 'hourly' && (
                              <TableCell align="right">{row.hour || 'N/A'}</TableCell>
                            )}
                          </TableRow>
                        ))}
                  </TableBody>
                </Table>
              </TableContainer>

              {processedData.length === 0 && (
                <Alert severity="info" sx={{ mt: 2 }}>
                  No data found for the selected criteria.
                </Alert>
              )}
            </TabPanel>

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
