import React, { useState } from 'react';
import {
  Box,
  Grid,
  Alert,
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  PlayArrow as ExecuteIcon,
  Assessment as IndicatorIcon,
  ArrowBack as BackIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useIndicator } from '@/hooks/useIndicators';
import { useDeleteIndicator, useExecuteIndicator } from '@/hooks/useIndicatorMutations';
import { TestIndicatorRequest } from '@/types/api';
import {
  PageHeader,
  LoadingSpinner,
} from '@/components';
import {
  DataSourceInfo,
  ThresholdConfiguration,
  StatusOverviewCards,
} from '@/components/Business';

// Import detail components
import {
  ExecutionStatusSection,
  ExecutionDetailsSection,
  NotificationContactsSection,
  IndicatorActionDialogs,
} from './components';



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
        <ExecutionStatusSection indicator={indicator} />

        {/* Execution History & Metadata */}
        <ExecutionDetailsSection indicator={indicator} />

        {/* Notification Contacts */}
        <NotificationContactsSection indicator={indicator} />
      </Grid>

      {/* Action Dialogs */}
      <IndicatorActionDialogs
        indicator={indicator}
        deleteDialogOpen={deleteDialogOpen}
        executeDialogOpen={executeDialogOpen}
        deleteLoading={deleteIndicatorMutation.isPending}
        executeLoading={executeIndicatorMutation.isPending}
        onDeleteConfirm={handleDelete}
        onDeleteCancel={() => setDeleteDialogOpen(false)}
        onExecuteConfirm={handleExecute}
        onExecuteCancel={() => setExecuteDialogOpen(false)}
      />
    </Box>
  );
};

export default IndicatorDetail;
