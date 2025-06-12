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
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  PlayArrow as ExecuteIcon,
  Schedule as ScheduleIcon,
  Person as PersonIcon,
  Settings as SettingsIcon,
  TrendingUp as TrendingUpIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { format } from 'date-fns';
import { useIndicator } from '@/hooks/useIndicators';
import { useDeleteIndicator, useExecuteIndicator } from '@/hooks/useIndicatorMutations';
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
import toast from 'react-hot-toast';

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
      indicatorId: indicator.indicatorId,
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
        subtitle={`${indicator.indicatorCode} • ${indicator.collectorItemName}`}
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

      <Grid container spacing={3}>
        {/* Status Overview */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Status Overview
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6} md={3}>
                  <InfoItem
                    label="Status"
                    value={<StatusChip status={getIndicatorStatus()} />}
                    icon={<SettingsIcon />}
                  />
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <InfoItem
                    label="Priority"
                    value={getPriorityLabel(indicator.priority)}
                    icon={<TrendingUpIcon />}
                  />
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <InfoItem
                    label="Last Executed"
                    value={indicator.lastExecuted ? format(new Date(indicator.lastExecuted), 'MMM dd, yyyy HH:mm') : 'Never'}
                    icon={<ScheduleIcon />}
                  />
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <InfoItem
                    label="Owner"
                    value={indicator.ownerContact?.name || 'Unknown'}
                    icon={<PersonIcon />}
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Configuration Details */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Configuration
              </Typography>
              <Stack spacing={2}>
                <InfoItem label="Indicator Name" value={indicator.indicatorName} />
                <InfoItem label="Indicator Code" value={indicator.indicatorCode} />
                {indicator.indicatorDesc && (
                  <InfoItem label="Description" value={indicator.indicatorDesc} />
                )}
                <Divider />
                <InfoItem label="Collector Item" value={indicator.collectorItemName} />
                <InfoItem label="Time Range" value={`${indicator.lastMinutes} minutes`} />
                <InfoItem label="Priority" value={getPriorityLabel(indicator.priority)} />
                {indicator.averageLastDays && (
                  <InfoItem label="Average Last Days" value={indicator.averageLastDays} />
                )}
                <InfoItem
                  label="Average of Current Hour"
                  value={indicator.averageOfCurrHour ? 'Yes' : 'No'}
                />
                {indicator.averageHour && (
                  <InfoItem label="Current Hour Average" value={indicator.averageHour} />
                )}
                <Divider />
                <InfoItem label="Created" value={format(new Date(indicator.createdDate), 'MMM dd, yyyy HH:mm')} />
                <InfoItem label="Last Updated" value={format(new Date(indicator.updatedDate), 'MMM dd, yyyy HH:mm')} />
                {indicator.lastRunResult && (
                  <InfoItem label="Last Run Result" value={indicator.lastRunResult} />
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Threshold Configuration */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Threshold Configuration
              </Typography>
              <Stack spacing={2}>
                {indicator.thresholdType ? (
                  <>
                    <InfoItem label="Threshold Type" value={indicator.thresholdType} />
                    {indicator.thresholdField && (
                      <InfoItem label="Threshold Field" value={indicator.thresholdField} />
                    )}
                    {indicator.thresholdComparison && (
                      <InfoItem label="Comparison" value={indicator.thresholdComparison} />
                    )}
                    {indicator.thresholdValue !== undefined && (
                      <InfoItem label="Threshold Value" value={indicator.thresholdValue} />
                    )}
                  </>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    No threshold configuration set
                  </Typography>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Execution Status */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Execution Status
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6} md={3}>
                  <InfoItem
                    label="Currently Running"
                    value={indicator.isCurrentlyRunning ? 'Yes' : 'No'}
                  />
                </Grid>
                {indicator.executionStartTime && (
                  <Grid item xs={12} sm={6} md={3}>
                    <InfoItem
                      label="Execution Started"
                      value={format(new Date(indicator.executionStartTime), 'MMM dd, yyyy HH:mm:ss')}
                    />
                  </Grid>
                )}
                {indicator.executionContext && (
                  <Grid item xs={12} sm={6} md={3}>
                    <InfoItem
                      label="Execution Context"
                      value={indicator.executionContext}
                    />
                  </Grid>
                )}
                {indicator.isCurrentlyRunning && indicator.executionStartTime && (
                  <Grid item xs={12} sm={6} md={3}>
                    <InfoItem
                      label="Running Duration"
                      value={`${Math.floor((new Date().getTime() - new Date(indicator.executionStartTime).getTime()) / 1000)} seconds`}
                    />
                  </Grid>
                )}
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Contacts */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
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
                    key={contact.contactId}
                    label={contact.name}
                    variant="outlined"
                  />
                ))}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Schedule Configuration */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Schedule Configuration
              </Typography>
              <Box
                component="pre"
                sx={{
                  backgroundColor: 'grey.100',
                  p: 2,
                  borderRadius: 1,
                  overflow: 'auto',
                  fontSize: '0.875rem',
                  fontFamily: 'monospace',
                }}
              >
                {JSON.stringify(JSON.parse(indicator.scheduleConfiguration), null, 2)}
              </Box>
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
