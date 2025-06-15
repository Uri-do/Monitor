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
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  PlayArrow as ExecuteIcon,
  Schedule as ScheduleIcon,
  Person as PersonIcon,
  Settings as SettingsIcon,
  Assessment as IndicatorIcon,
  ArrowBack as BackIcon,
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
  InfoItem,
  ConfirmationDialog
} from '@/components';
import {
  DataSourceInfo,
  ThresholdConfiguration,
  StatusOverviewCards,
  SchedulerDetails,
} from '@/components/Business';



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
      indicatorID: indicator.indicatorID,
    };

    executeIndicatorMutation.mutate(request, {
      onSuccess: () => {
        setExecuteDialogOpen(false);
        refetch(); // Refresh indicator data to show updated status
      },
    });
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
        subtitle={`${indicator.indicatorCode} • Collector → ${indicator.collectorItemName}`}
        icon={<IndicatorIcon />}
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
          },
          {
            label: 'Delete',
            icon: <DeleteIcon />,
            onClick: () => setDeleteDialogOpen(true),
          },
        ]}
      />

      {/* Data Source Information */}
      <DataSourceInfo indicator={indicator} />

      {/* Threshold Configuration */}
      <ThresholdConfiguration indicator={indicator} />

      {/* Status Overview Cards */}
      <StatusOverviewCards indicator={indicator} />

      <Grid container spacing={3} sx={{ mt: 1 }}>

        {/* Scheduler & Execution Status */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography
                variant="h6"
                gutterBottom
                sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
              >
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
                  <InfoItem label="Execution Context" value={indicator.executionContext} />
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
              <Typography
                variant="h6"
                gutterBottom
                sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
              >
                <SettingsIcon color="primary" />
                Execution Details
              </Typography>
              <Stack spacing={2}>
                <InfoItem label="Data Window" value={`${indicator.lastMinutes} minutes`} />
                {indicator.averageLastDays && (
                  <InfoItem label="Average Last Days" value={indicator.averageLastDays} />
                )}
                <Divider />
                <InfoItem
                  label="Created"
                  value={format(new Date(indicator.createdDate), 'MMM dd, yyyy HH:mm')}
                />
                <InfoItem
                  label="Last Updated"
                  value={format(new Date(indicator.updatedDate), 'MMM dd, yyyy HH:mm')}
                />
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
              <Typography
                variant="h6"
                gutterBottom
                sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
              >
                <PersonIcon color="primary" />
                Notification Contacts
              </Typography>
              <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                <Chip
                  label={`${indicator.ownerContact?.name || 'Unknown'} (Owner)`}
                  color="primary"
                  variant="outlined"
                />
                {indicator.contacts.map(contact => (
                  <Chip key={contact.contactID} label={contact.name} variant="outlined" />
                ))}
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Delete Confirmation Dialog */}
      <ConfirmationDialog
        open={deleteDialogOpen}
        title="Delete Indicator"
        message={`Are you sure you want to delete "${indicator.indicatorName}"? This action cannot be undone.`}
        confirmLabel="Delete"
        confirmColor="error"
        loading={deleteIndicatorMutation.isPending}
        onConfirm={handleDelete}
        onCancel={() => setDeleteDialogOpen(false)}
      />

      {/* Execute Confirmation Dialog */}
      <ConfirmationDialog
        open={executeDialogOpen}
        title="Execute Indicator"
        message={`Execute "${indicator.indicatorName}" now? This will run the indicator outside of its normal schedule.`}
        confirmLabel="Execute"
        loading={executeIndicatorMutation.isPending}
        onConfirm={handleExecute}
        onCancel={() => setExecuteDialogOpen(false)}
      />
    </Box>
  );
};

export default IndicatorDetail;
