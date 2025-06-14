import React, { useState } from 'react';
import {
  Box,
  Grid,
  Typography,
  CardContent,
  Chip,
  Stack,
  Divider,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemText,
  CircularProgress,
  Popover,
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  PlayArrow as ExecuteIcon,
  Schedule as ScheduleIcon,
  Person as PersonIcon,
  Settings as SettingsIcon,
  TrendingUp as TrendingUpIcon,
  Assessment as KpiIcon,
  ArrowBack as BackIcon,
  Warning as ThresholdIcon,
  CheckCircle as CheckIcon,
  Error as ErrorIcon,
  Storage as DatabaseIcon,
  ExpandMore as ExpandMoreIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { format } from 'date-fns';
import { useIndicator, useCollectorItemNames } from '@/hooks/useIndicators';
import { useDeleteIndicator, useExecuteIndicator } from '@/hooks/useIndicatorMutations';
import { useCollector, useCollectorStatistics } from '@/hooks/useMonitorStatistics';
import { useQuery } from '@tanstack/react-query';
import { schedulerApi } from '@/services/api';
import { TestIndicatorRequest } from '@/types/api';
import {
  PageHeader,
  LoadingSpinner,
  Card,
  Button,
  StatusChip,
} from '@/components';

// Temporary InfoItem component until it's added to the component library
const InfoItem: React.FC<{ label: string; value: React.ReactNode; icon?: React.ReactNode }> = ({ label, value, icon }) => (
  <Box>
    <Typography variant="body2" color="text.secondary" gutterBottom>
      {label}
    </Typography>
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      {icon}
      <Typography variant="body1">{value}</Typography>
    </Box>
  </Box>
);

// Component to show collector items expansion with optional statistics
const CollectorItemsExpander: React.FC<{
  collectorId: number;
  selectedItemName?: string;
  showStats?: boolean;
}> = ({ collectorId, selectedItemName, showStats = false }) => {
  const [anchorEl, setAnchorEl] = React.useState<HTMLElement | null>(null);
  const { data: itemNames, isLoading } = useCollectorItemNames(collectorId);
  const { data: collector } = useCollector(collectorId);

  // Fetch last 30 days statistics if showStats is enabled
  const fromDate = React.useMemo(() => {
    const date = new Date();
    date.setDate(date.getDate() - 30);
    return date.toISOString().split('T')[0];
  }, []);

  const toDate = React.useMemo(() => {
    return new Date().toISOString().split('T')[0];
  }, []);

  const { data: statistics, isLoading: statsLoading } = useCollectorStatistics(
    collectorId,
    showStats ? { fromDate, toDate } : undefined
  );

  // Calculate summary stats for ALL items
  const allItemsStats = React.useMemo(() => {
    if (!statistics || !showStats) return null;

    // Group by item name and calculate totals
    const itemGroups = statistics.reduce((acc, stat) => {
      const itemName = stat.itemName || 'Unknown';
      if (!acc[itemName]) {
        acc[itemName] = { totalSum: 0, markedSum: 0, recordCount: 0 };
      }
      acc[itemName].totalSum += stat.total || 0;
      acc[itemName].markedSum += stat.marked || 0;
      acc[itemName].recordCount += 1;
      return acc;
    }, {} as Record<string, { totalSum: number; markedSum: number; recordCount: number }>);

    // Convert to array and calculate percentages
    return Object.entries(itemGroups).map(([itemName, stats]) => ({
      itemName,
      totalSum: stats.totalSum,
      markedSum: stats.markedSum,
      avgTotal: stats.recordCount > 0 ? stats.totalSum / stats.recordCount : 0,
      avgMarked: stats.recordCount > 0 ? stats.markedSum / stats.recordCount : 0,
      markedPercent: stats.totalSum > 0 ? (stats.markedSum / stats.totalSum) * 100 : 0,
      recordCount: stats.recordCount
    })).sort((a, b) => b.totalSum - a.totalSum); // Sort by total descending
  }, [statistics, showStats]);

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const open = Boolean(anchorEl);

  if (isLoading) {
    return <CircularProgress size={16} />;
  }

  if (!itemNames || itemNames.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        No items available for this collector
      </Typography>
    );
  }

  const collectorName = collector?.displayName || collector?.collectorDesc || collector?.collectorCode || 'Collector';

  return (
    <>
      <Box
        sx={{
          cursor: 'pointer',
          p: 2,
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 1,
          '&:hover': {
            bgcolor: 'action.hover'
          }
        }}
        onClick={handleClick}
      >
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography variant="body2" sx={{ fontWeight: 500 }}>
              {selectedItemName} • See all collector items ({itemNames.length} available)
            </Typography>
          </Box>
          <ExpandMoreIcon color="action" />
        </Box>
      </Box>

      <Popover
        open={open}
        anchorEl={anchorEl}
        onClose={handleClose}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'left',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'left',
        }}
        slotProps={{
          paper: {
            sx: {
              width: anchorEl?.offsetWidth || 'auto',
              maxHeight: 500,
              overflow: 'auto'
            }
          }
        }}
      >
        <Box sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            {collectorName} - All Items
          </Typography>

          {showStats && allItemsStats && (
            <Box sx={{ mb: 3 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Last 30 Days Statistics (All Items)
              </Typography>
              {statsLoading ? (
                <CircularProgress size={16} />
              ) : (
                <Box sx={{ maxHeight: 200, overflow: 'auto' }}>
                  {allItemsStats.map((itemStat) => (
                    <Box
                      key={itemStat.itemName}
                      sx={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        py: 0.5,
                        px: 1,
                        borderBottom: '1px solid',
                        borderColor: 'divider',
                        '&:last-child': { borderBottom: 'none' }
                      }}
                    >
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" sx={{ fontWeight: itemStat.itemName === selectedItemName ? 600 : 400 }}>
                          {itemStat.itemName}
                        </Typography>
                        {itemStat.itemName === selectedItemName && (
                          <Chip label="Selected" size="small" color="primary" />
                        )}
                      </Box>
                      <Typography variant="caption" color="text.secondary">
                        {itemStat.totalSum.toLocaleString()} • {itemStat.markedSum.toLocaleString()} ({itemStat.markedPercent.toFixed(1)}%)
                      </Typography>
                    </Box>
                  ))}
                </Box>
              )}
            </Box>
          )}


        </Box>
      </Popover>
    </>
  );
};

// Component to show scheduler details
const SchedulerDetails: React.FC<{ schedulerId: number }> = ({ schedulerId }) => {
  const { data: scheduler, isLoading } = useQuery({
    queryKey: ['scheduler', schedulerId],
    queryFn: () => schedulerApi.getScheduler(schedulerId),
    enabled: !!schedulerId,
  });

  if (isLoading) {
    return <CircularProgress size={16} />;
  }

  if (!scheduler) {
    return (
      <Typography variant="body2" color="text.secondary">
        Scheduler not found
      </Typography>
    );
  }

  return (
    <Stack spacing={1}>
      <InfoItem label="Scheduler Name" value={scheduler.schedulerName} />
      <InfoItem label="Schedule Type" value={scheduler.scheduleType} />
      <InfoItem label="Schedule" value={scheduler.displayText} />
      {scheduler.nextExecutionTime && (
        <InfoItem
          label="Next Execution"
          value={format(new Date(scheduler.nextExecutionTime), 'MMM dd, yyyy HH:mm')}
        />
      )}
      <InfoItem
        label="Status"
        value={scheduler.isEnabled ? 'Enabled' : 'Disabled'}
      />
    </Stack>
  );
};

const IndicatorDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const indicatorId = parseInt(id || '0', 10);

  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [executeDialogOpen, setExecuteDialogOpen] = useState(false);

  // Use our enhanced hooks
  const { data: indicator, isLoading, refetch } = useIndicator(indicatorId);
  const deleteIndicatorMutation = useDeleteIndicator();
  const executeIndicatorMutation = useExecuteIndicator();

  // Fetch collector details if collectorID is available
  const { data: collector } = useCollector(indicator?.collectorID || 0);

  const handleDelete = () => {
    deleteIndicatorMutation.mutate(indicatorId, {
      onSuccess: () => {
        setDeleteDialogOpen(false);
        navigate('/indicators');
      },
    });
  };

  const handleExecute = () => {
    if (!indicator) return;

    const request: TestIndicatorRequest = {
      indicatorID: indicator.indicatorID,
    };

    executeIndicatorMutation.mutate(request, {
      onSuccess: () => {
        setExecuteDialogOpen(false);
        refetch(); // Refresh indicator data to show updated status
      },
    });
  };

  const getIndicatorStatus = () => {
    if (!indicator) return 'unknown';
    if (!indicator.isActive) return 'inactive';
    if (indicator.isCurrentlyRunning) return 'running';
    return 'active';
  };

  const getPriorityLabel = (priority: string | number) => {
    if (typeof priority === 'string') {
      return priority.charAt(0).toUpperCase() + priority.slice(1);
    }
    switch (priority) {
      case 1: return 'High';
      case 2: return 'Medium';
      case 3: return 'Normal';
      case 4: return 'Low';
      case 5: return 'Very Low';
      default: return 'Unknown';
    }
  };

  // Threshold helper functions
  const getThresholdStatusIcon = () => {
    if (!indicator?.thresholdType || !indicator?.thresholdField || !indicator?.thresholdComparison || indicator?.thresholdValue === undefined) {
      return <ErrorIcon sx={{ color: 'warning.main' }} />;
    }
    return <CheckIcon sx={{ color: 'success.main' }} />;
  };

  const getThresholdDescription = () => {
    if (!indicator?.thresholdType || !indicator?.thresholdField || !indicator?.thresholdComparison || indicator?.thresholdValue === undefined) {
      return 'Threshold configuration is incomplete. Please configure all threshold settings.';
    }

    const field = indicator.thresholdField;
    const comparison = getComparisonText(indicator.thresholdComparison);
    const value = indicator.thresholdValue;
    const type = formatThresholdType(indicator.thresholdType);

    return `Alert when ${field} is ${comparison} ${value} (${type})`;
  };

  const getComparisonSymbol = (comparison?: string) => {
    switch (comparison) {
      case 'gt': return '>';
      case 'gte': return '≥';
      case 'lt': return '<';
      case 'lte': return '≤';
      case 'eq': return '=';
      default: return comparison || 'Not Set';
    }
  };

  const getComparisonText = (comparison?: string) => {
    switch (comparison) {
      case 'gt': return 'greater than';
      case 'gte': return 'greater than or equal to';
      case 'lt': return 'less than';
      case 'lte': return 'less than or equal to';
      case 'eq': return 'equal to';
      default: return 'compared to';
    }
  };

  const formatThresholdType = (type?: string) => {
    switch (type) {
      case 'volume_average': return 'Volume Average';
      case 'threshold_value': return 'Threshold Value';
      case 'percentage': return 'Percentage';
      default: return type || 'Not Set';
    }
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (!indicator) {
    return (
      <Box>
        <Alert severity="error">
          Indicator not found. It may have been deleted or you don't have permission to view it.
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader
        title={indicator.indicatorName}
        subtitle={`${indicator.indicatorCode} • ${collector?.displayName || 'Collector'} → ${indicator.collectorItemName}`}
        icon={<KpiIcon />}
        backAction={{
          label: 'Back to Indicators',
          icon: <BackIcon />,
          onClick: () => navigate('/indicators'),
        }}
        primaryAction={{
          label: 'Edit Indicator',
          icon: <EditIcon />,
          onClick: () => navigate(`/indicators/${indicator.indicatorID}/edit`),
        }}
        actions={[
          {
            label: 'Execute Now',
            icon: <ExecuteIcon />,
            onClick: () => setExecuteDialogOpen(true),
            // disabled: !indicator.isActive || indicator.isCurrentlyRunning, // TODO: Add disabled support
          },
          {
            label: 'Delete',
            icon: <DeleteIcon />,
            onClick: () => setDeleteDialogOpen(true),
            // color: 'error', // TODO: Add color support
          },
        ]}
      />

      {/* Prominent Data Source Information */}
      <Card sx={{ mb: 3, border: '2px solid', borderColor: 'primary.main' }}>
        <CardContent sx={{ py: 3 }}>
          <Grid container spacing={3} alignItems="center">
            <Grid item xs={12} md={8}>
              <Box display="flex" alignItems="center" gap={2} mb={2}>
                <DatabaseIcon sx={{ fontSize: '2.5rem', color: 'primary.main' }} />
                <Box>
                  <Typography variant="h4" sx={{ fontWeight: 600, mb: 0.5, color: 'primary.main' }}>
                    {collector?.displayName || collector?.collectorDesc || collector?.collectorCode || 'Unknown Collector'}
                  </Typography>
                  <Typography variant="h5" sx={{ fontWeight: 500, mb: 1, color: 'text.primary' }}>
                    Item: {indicator.collectorItemName || 'Not specified'}
                  </Typography>
                  <Typography variant="body1" color="text.secondary">
                    {indicator.lastMinutes} minute intervals • Collector ID: {indicator.collectorID || 'N/A'}
                  </Typography>
                </Box>
              </Box>

              <Typography variant="body1" color="text.secondary">
                This indicator monitors <strong>{indicator.collectorItemName}</strong> from the{' '}
                <strong>{collector?.displayName || collector?.collectorDesc || collector?.collectorCode || 'collector'}</strong> data source
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

      {/* Threshold Configuration */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box display="flex" alignItems="center" gap={2} mb={3}>
            <ThresholdIcon color="primary" />
            <Box flex={1}>
              <Typography variant="h6" sx={{ fontWeight: 600, mb: 0.5 }}>
                Alert Threshold Configuration
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {getThresholdDescription()}
              </Typography>
            </Box>
            {getThresholdStatusIcon()}
          </Box>

          <Grid container spacing={2}>
            <Grid item xs={6} md={3}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Field
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 500 }}>
                {indicator.thresholdField || 'Not Set'}
              </Typography>
            </Grid>
            <Grid item xs={6} md={3}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Comparison
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 500 }}>
                {getComparisonSymbol(indicator.thresholdComparison)}
              </Typography>
            </Grid>
            <Grid item xs={6} md={3}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Threshold Value
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 500 }}>
                {indicator.thresholdValue !== undefined ? indicator.thresholdValue : 'Not Set'}
              </Typography>
            </Grid>
            <Grid item xs={6} md={3}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Type
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 500 }}>
                {formatThresholdType(indicator.thresholdType)}
              </Typography>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Grid container spacing={3}>
        {/* Status Overview - Compact Cards */}
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <SettingsIcon color="primary" sx={{ fontSize: '2rem', mb: 1 }} />
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Status
              </Typography>
              <StatusChip status={getIndicatorStatus()} />
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <TrendingUpIcon color="primary" sx={{ fontSize: '2rem', mb: 1 }} />
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Priority
              </Typography>
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                {getPriorityLabel(indicator.priority)}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <ScheduleIcon color="primary" sx={{ fontSize: '2rem', mb: 1 }} />
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Last Executed
              </Typography>
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                {indicator.lastRun ? format(new Date(indicator.lastRun), 'MMM dd, yyyy HH:mm') : 'Never'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <PersonIcon color="primary" sx={{ fontSize: '2rem', mb: 1 }} />
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Owner
              </Typography>
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                {indicator.ownerContact?.name || 'Unknown'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        {/* Scheduler & Execution Status */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <ScheduleIcon color="primary" />
                Scheduler & Execution
              </Typography>
              <Stack spacing={2}>
                {indicator.schedulerID ? (
                  <SchedulerDetails schedulerId={indicator.schedulerID} />
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    No scheduler assigned - manual execution only
                  </Typography>
                )}
                <Divider />
                <InfoItem
                  label="Currently Running"
                  value={indicator.isCurrentlyRunning ? 'Yes' : 'No'}
                />
                {indicator.executionStartTime && (
                  <InfoItem
                    label="Execution Started"
                    value={format(new Date(indicator.executionStartTime), 'MMM dd, yyyy HH:mm:ss')}
                  />
                )}
                {indicator.executionContext && (
                  <InfoItem
                    label="Execution Context"
                    value={indicator.executionContext}
                  />
                )}
                {indicator.isCurrentlyRunning && indicator.executionStartTime && (
                  <InfoItem
                    label="Running Duration"
                    value={`${Math.floor((new Date().getTime() - new Date(indicator.executionStartTime).getTime()) / 1000)} seconds`}
                  />
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Execution History & Metadata */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <SettingsIcon color="primary" />
                Execution Details
              </Typography>
              <Stack spacing={2}>
                <InfoItem label="Data Window" value={`${indicator.lastMinutes} minutes`} />
                {indicator.averageLastDays && (
                  <InfoItem label="Average Last Days" value={indicator.averageLastDays} />
                )}
                <Divider />
                <InfoItem label="Created" value={format(new Date(indicator.createdDate), 'MMM dd, yyyy HH:mm')} />
                <InfoItem label="Last Updated" value={format(new Date(indicator.updatedDate), 'MMM dd, yyyy HH:mm')} />
                {indicator.lastRunResult && (
                  <InfoItem label="Last Run Result" value={indicator.lastRunResult} />
                )}
                {indicator.indicatorDesc && (
                  <>
                    <Divider />
                    <InfoItem label="Description" value={indicator.indicatorDesc} />
                  </>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Notification Contacts */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <PersonIcon color="primary" />
                Notification Contacts
              </Typography>
              <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                <Chip
                  label={`${indicator.ownerContact?.name || 'Unknown'} (Owner)`}
                  color="primary"
                  variant="outlined"
                />
                {indicator.contacts.map((contact) => (
                  <Chip
                    key={contact.contactID}
                    label={contact.name}
                    variant="outlined"
                  />
                ))}
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Indicator</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{indicator.indicatorName}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDelete}
            disabled={deleteIndicatorMutation.isPending}
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>

      {/* Execute Confirmation Dialog */}
      <Dialog open={executeDialogOpen} onClose={() => setExecuteDialogOpen(false)}>
        <DialogTitle>Execute Indicator</DialogTitle>
        <DialogContent>
          <Typography>
            Execute "{indicator.indicatorName}" now? This will run the indicator outside of its normal schedule.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setExecuteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleExecute}
            disabled={executeIndicatorMutation.isPending}
          >
            Execute
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default IndicatorDetail;
