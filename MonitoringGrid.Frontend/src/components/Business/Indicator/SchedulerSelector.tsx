import React, { useState } from 'react';
import {
  Box,
  Typography,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Grid,
} from '@mui/material';
import { Schedule, Add } from '@mui/icons-material';
import { GenericSelector, GenericSelectorOption } from '../../UI/GenericSelector';
import { useStableQuery } from '../../../hooks/useGenericQuery';
import { schedulerApi } from '../../../services/api';

// Transform scheduler data to match GenericSelectorOption interface
interface SchedulerOption extends GenericSelectorOption {
  schedulerID: number;
  schedulerName: string;
  schedulerDescription?: string;
  cronExpression?: string;
  timezone?: string;
  isEnabled: boolean;
  nextRunTime?: string;
  lastRunTime?: string;
}

interface SchedulerSelectorProps {
  selectedSchedulerId?: number;
  onSchedulerChange: (schedulerId: number | undefined) => void;
  disabled?: boolean;
  className?: string;
  required?: boolean;
  variant?: 'standard' | 'detailed' | 'compact';
  showCreateButton?: boolean;
  showRefreshButton?: boolean;
  showSchedulerInfo?: boolean;
  title?: string;
  subtitle?: string;
}

export const SchedulerSelector: React.FC<SchedulerSelectorProps> = ({
  selectedSchedulerId,
  onSchedulerChange,
  disabled = false,
  className = '',
  required = false,
  variant = 'detailed',
  showCreateButton = true,
  showRefreshButton = true,
  showSchedulerInfo = true,
  title,
  subtitle,
}) => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [newSchedulerName, setNewSchedulerName] = useState('');
  const [newSchedulerCron, setNewSchedulerCron] = useState('');

  // Fetch schedulers using the new generic query hook
  const {
    data: schedulers,
    isLoading: schedulersLoading,
    error: schedulersError,
    refetch: refetchSchedulers,
  } = useStableQuery(['schedulers'], () => schedulerApi.getSchedulers(), {
    graceful404: true,
    fallbackValue: [],
    errorContext: 'Loading schedulers',
  });

  // Transform schedulers to match GenericSelectorOption interface
  const schedulerOptions: SchedulerOption[] = (Array.isArray(schedulers) ? schedulers : []).map(scheduler => ({
    id: scheduler.schedulerID,
    name: scheduler.schedulerName || `Scheduler ${scheduler.schedulerID}`,
    description: scheduler.schedulerDescription,
    isEnabled: scheduler.isEnabled ?? true,
    isActive: scheduler.isEnabled ?? true,
    displayText: `${scheduler.schedulerName} - ${scheduler.cronExpression || 'No schedule'}`,
    metadata: {
      type: 'scheduler',
      cronExpression: scheduler.cronExpression,
      timezone: scheduler.timezone,
      nextRunTime: scheduler.nextRunTime,
      lastRunTime: scheduler.lastRunTime,
    },
    // Original properties for backward compatibility
    schedulerID: scheduler.schedulerID,
    schedulerName: scheduler.schedulerName,
    schedulerDescription: scheduler.schedulerDescription,
    cronExpression: scheduler.cronExpression,
    timezone: scheduler.timezone,
    nextRunTime: scheduler.nextRunTime,
    lastRunTime: scheduler.lastRunTime,
  }));

  // Handle scheduler selection
  const handleSchedulerChange = (schedulerId: string | number | undefined) => {
    const numericId = schedulerId ? Number(schedulerId) : undefined;
    onSchedulerChange(numericId);
  };

  // Handle create new scheduler
  const handleCreateScheduler = async () => {
    try {
      const newScheduler = await schedulerApi.createScheduler({
        schedulerName: newSchedulerName,
        cronExpression: newSchedulerCron,
        isEnabled: true,
      });

      // Refresh the list and select the new scheduler
      await refetchSchedulers();
      onSchedulerChange(newScheduler.schedulerID);

      // Close dialog and reset form
      setCreateDialogOpen(false);
      setNewSchedulerName('');
      setNewSchedulerCron('');
    } catch (error) {
      // Handle error through error handling system
      // Error will be handled by the error handling system
    }
  };

  // Custom render for scheduler info card
  const renderSchedulerInfo = (scheduler: SchedulerOption) => (
    <Box>
      <Typography variant="subtitle2" fontWeight="medium" gutterBottom>
        {scheduler.name}
      </Typography>
      {scheduler.description && (
        <Typography variant="body2" color="text.secondary" gutterBottom>
          {scheduler.description}
        </Typography>
      )}
      <Box sx={{ display: 'flex', gap: 2, mt: 1, flexWrap: 'wrap' }}>
        <Typography variant="caption" color="text.secondary">
          <strong>Schedule:</strong> {scheduler.metadata?.cronExpression || 'Not set'}
        </Typography>
        {scheduler.metadata?.timezone && (
          <Typography variant="caption" color="text.secondary">
            <strong>Timezone:</strong> {scheduler.metadata.timezone}
          </Typography>
        )}
      </Box>
      {scheduler.metadata?.nextRunTime && (
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
          <strong>Next Run:</strong> {scheduler.metadata.nextRunTime}
        </Typography>
      )}
      {scheduler.metadata?.lastRunTime && (
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
          <strong>Last Run:</strong> {scheduler.metadata.lastRunTime}
        </Typography>
      )}
    </Box>
  );

  return (
    <Box className={className}>
      {/* Title and Subtitle */}
      {(title || subtitle) && (
        <Box sx={{ mb: 2 }}>
          {title && (
            <Typography
              variant="h6"
              gutterBottom
              sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
            >
              <Schedule color="primary" />
              {title}
            </Typography>
          )}
          {subtitle && (
            <Typography variant="body2" color="text.secondary">
              {subtitle}
            </Typography>
          )}
        </Box>
      )}

      {/* Scheduler Selection using GenericSelector */}
      <GenericSelector
        data={schedulerOptions}
        loading={schedulersLoading}
        error={schedulersError}
        selectedId={selectedSchedulerId}
        onSelectionChange={handleSchedulerChange}
        label="Scheduler"
        required={required}
        disabled={disabled}
        variant={variant}
        showRefreshButton={showRefreshButton}
        showCreateButton={showCreateButton}
        showInfoCard={showSchedulerInfo}
        onCreateClick={() => setCreateDialogOpen(true)}
        onRefresh={() => refetchSchedulers()}
        queryKey={['schedulers']}
        renderInfoCard={renderSchedulerInfo}
        emptyMessage="No schedulers found. Create one to get started."
        placeholder="No scheduler selected"
      />

      {/* Create Scheduler Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Add color="primary" />
            Create New Scheduler
          </Box>
        </DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Scheduler Name"
                value={newSchedulerName}
                onChange={e => setNewSchedulerName(e.target.value)}
                placeholder="Enter scheduler name"
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Cron Expression"
                value={newSchedulerCron}
                onChange={e => setNewSchedulerCron(e.target.value)}
                placeholder="0 0 * * * (every hour)"
                helperText="Use cron format: minute hour day month weekday"
                required
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateScheduler}
            variant="contained"
            disabled={!newSchedulerName.trim() || !newSchedulerCron.trim()}
            startIcon={<Add />}
          >
            Create Scheduler
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default SchedulerSelector;
