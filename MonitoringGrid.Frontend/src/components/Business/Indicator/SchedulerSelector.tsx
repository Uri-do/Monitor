import React, { useState, useEffect } from 'react';
import {
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Box,
  Typography,
  Chip,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Grid,
  Alert,
  Divider,
  Card,
  CardContent,
  IconButton,
  Tooltip,
  SelectChangeEvent
} from '@mui/material';
import {
  Schedule,
  Add,
  Edit,
  Info,
  AccessTime,
  Event,
  Refresh
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

interface SchedulerOption {
  schedulerID: number;
  schedulerName: string;
  schedulerDescription?: string;
  scheduleType: string;
  intervalMinutes?: number;
  cronExpression?: string;
  executionDateTime?: string;
  timezone: string;
  isEnabled: boolean;
  displayText: string;
  nextExecutionTime?: string;
  isCurrentlyActive: boolean;
  indicatorCount: number;
}

interface SchedulerSelectorProps {
  selectedSchedulerId?: number;
  onSchedulerChange: (schedulerId: number | undefined) => void;
  disabled?: boolean;
  required?: boolean;
  showCreateButton?: boolean;
  showScheduleInfo?: boolean;
  variant?: 'standard' | 'detailed';
}

export const SchedulerSelector: React.FC<SchedulerSelectorProps> = ({
  selectedSchedulerId,
  onSchedulerChange,
  disabled = false,
  required = false,
  showCreateButton = true,
  showScheduleInfo = true,
  variant = 'standard',
}) => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const queryClient = useQueryClient();

  // Fetch schedulers
  const {
    data: schedulers = [],
    isLoading: schedulersLoading,
    error: schedulersError,
    refetch: refetchSchedulers,
  } = useQuery({
    queryKey: ['schedulers'],
    queryFn: async () => {
      const response = await fetch('/api/schedulers');
      if (!response.ok) {
        throw new Error('Failed to fetch schedulers');
      }
      return response.json();
    },
  });

  const selectedScheduler = schedulers.find((s: SchedulerOption) => s.schedulerID === selectedSchedulerId);

  const handleSchedulerChange = (event: SelectChangeEvent<string>) => {
    const value = event.target.value;
    if (value === '') {
      onSchedulerChange(undefined);
    } else {
      const schedulerId = parseInt(value, 10);
      if (!isNaN(schedulerId)) {
        onSchedulerChange(schedulerId);
      }
    }
  };

  const handleCreateScheduler = () => {
    setCreateDialogOpen(true);
  };

  const handleRefresh = () => {
    refetchSchedulers();
  };

  const getScheduleTypeIcon = (scheduleType: string) => {
    switch (scheduleType) {
      case 'interval':
        return <AccessTime fontSize="small" />;
      case 'cron':
        return <Schedule fontSize="small" />;
      case 'onetime':
        return <Event fontSize="small" />;
      default:
        return <Schedule fontSize="small" />;
    }
  };

  const getScheduleTypeColor = (scheduleType: string) => {
    switch (scheduleType) {
      case 'interval':
        return 'primary';
      case 'cron':
        return 'secondary';
      case 'onetime':
        return 'warning';
      default:
        return 'default';
    }
  };

  const renderSchedulerOption = (scheduler: SchedulerOption) => {
    if (variant === 'detailed') {
      return (
        <Box sx={{ py: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
            {getScheduleTypeIcon(scheduler.scheduleType)}
            <Typography variant="body2" fontWeight="medium">
              {scheduler.schedulerName}
            </Typography>
            <Chip
              label={scheduler.scheduleType}
              size="small"
              color={getScheduleTypeColor(scheduler.scheduleType) as any}
              variant="outlined"
            />
            {!scheduler.isEnabled && (
              <Chip label="Disabled" size="small" color="error" variant="outlined" />
            )}
          </Box>
          <Typography variant="caption" color="text.secondary" sx={{ ml: 3, display: 'block' }}>
            {scheduler.displayText}
          </Typography>
          {scheduler.schedulerDescription && (
            <Typography variant="caption" color="text.secondary" sx={{ ml: 3, display: 'block', fontStyle: 'italic' }}>
              {scheduler.schedulerDescription}
            </Typography>
          )}
          {scheduler.nextExecutionTime && (
            <Typography variant="caption" color="success.main" sx={{ ml: 3, display: 'block' }}>
              Next: {new Date(scheduler.nextExecutionTime).toLocaleString()}
            </Typography>
          )}
        </Box>
      );
    } else {
      return (
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {getScheduleTypeIcon(scheduler.scheduleType)}
            <Typography variant="body2" fontWeight="medium">
              {scheduler.schedulerName}
            </Typography>
            <Chip
              label={scheduler.scheduleType}
              size="small"
              color={getScheduleTypeColor(scheduler.scheduleType) as any}
              variant="outlined"
            />
          </Box>
          <Typography variant="caption" color="text.secondary">
            {scheduler.displayText}
          </Typography>
        </Box>
      );
    }
  };

  if (schedulersError) {
    return (
      <Alert severity="error">
        Failed to load schedulers. Please try again later.
      </Alert>
    );
  }

  return (
    <Box>
      {/* Scheduler Selection */}
      <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
        <FormControl fullWidth>
          <InputLabel id="scheduler-select-label">
            Scheduler {required && <span style={{ color: 'red' }}>*</span>}
          </InputLabel>
          <Select
            labelId="scheduler-select-label"
            value={selectedSchedulerId ? selectedSchedulerId.toString() : ''}
            onChange={handleSchedulerChange}
            disabled={disabled || schedulersLoading}
            label={`Scheduler ${required ? '*' : ''}`}
            MenuProps={{
              PaperProps: {
                sx: { maxHeight: variant === 'detailed' ? 500 : 400 }
              }
            }}
          >
            {!required && (
              <MenuItem value="">
                <Typography color="text.secondary">No scheduler (manual execution only)</Typography>
              </MenuItem>
            )}
            {schedulers
              .filter((scheduler: SchedulerOption) => scheduler.isEnabled)
              .map((scheduler: SchedulerOption) => (
                <MenuItem key={scheduler.schedulerID} value={scheduler.schedulerID.toString()}>
                  {renderSchedulerOption(scheduler)}
                </MenuItem>
              ))}
          </Select>
        </FormControl>

        {/* Action Buttons */}
        <Box sx={{ display: 'flex', alignItems: 'flex-end', gap: 0.5 }}>
          <Tooltip title="Refresh Schedulers">
            <IconButton
              onClick={handleRefresh}
              disabled={disabled || schedulersLoading}
              size="medium"
              color="primary"
            >
              <Refresh />
            </IconButton>
          </Tooltip>
          
          {showCreateButton && (
            <Tooltip title="Create New Scheduler">
              <IconButton
                onClick={handleCreateScheduler}
                disabled={disabled}
                size="medium"
                color="primary"
              >
                <Add />
              </IconButton>
            </Tooltip>
          )}
        </Box>
      </Box>

      {/* Selected Scheduler Info */}
      {showScheduleInfo && selectedScheduler && (
        <Card variant="outlined" sx={{ mb: 2 }}>
          <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
              {getScheduleTypeIcon(selectedScheduler.scheduleType)}
              <Typography variant="subtitle2" fontWeight="medium">
                {selectedScheduler.schedulerName}
              </Typography>
              <Chip
                label={selectedScheduler.scheduleType}
                size="small"
                color={getScheduleTypeColor(selectedScheduler.scheduleType) as any}
                variant="outlined"
              />
            </Box>
            
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {selectedScheduler.displayText}
            </Typography>
            
            {selectedScheduler.schedulerDescription && (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1, fontStyle: 'italic' }}>
                {selectedScheduler.schedulerDescription}
              </Typography>
            )}
            
            <Grid container spacing={2} sx={{ mt: 1 }}>
              <Grid item xs={6}>
                <Typography variant="caption" color="text.secondary">
                  Timezone: {selectedScheduler.timezone}
                </Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography variant="caption" color="text.secondary">
                  Used by {selectedScheduler.indicatorCount} indicator(s)
                </Typography>
              </Grid>
              {selectedScheduler.nextExecutionTime && (
                <Grid item xs={12}>
                  <Typography variant="caption" color="success.main">
                    Next execution: {new Date(selectedScheduler.nextExecutionTime).toLocaleString()}
                  </Typography>
                </Grid>
              )}
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Create Scheduler Dialog */}
      <CreateSchedulerDialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        onCreated={(newScheduler) => {
          queryClient.invalidateQueries({ queryKey: ['schedulers'] });
          onSchedulerChange(newScheduler.schedulerID);
          setCreateDialogOpen(false);
        }}
      />
    </Box>
  );
};

// Simple create scheduler dialog component
interface CreateSchedulerDialogProps {
  open: boolean;
  onClose: () => void;
  onCreated: (scheduler: SchedulerOption) => void;
}

const CreateSchedulerDialog: React.FC<CreateSchedulerDialogProps> = ({
  open,
  onClose,
  onCreated,
}) => {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [scheduleType, setScheduleType] = useState('interval');
  const [intervalMinutes, setIntervalMinutes] = useState(60);

  const createMutation = useMutation({
    mutationFn: async (data: any) => {
      const response = await fetch('/api/schedulers', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      if (!response.ok) {
        throw new Error('Failed to create scheduler');
      }
      return response.json();
    },
    onSuccess: (newScheduler) => {
      onCreated(newScheduler);
      setName('');
      setDescription('');
      setScheduleType('interval');
      setIntervalMinutes(60);
    },
  });

  const handleSubmit = () => {
    createMutation.mutate({
      schedulerName: name,
      schedulerDescription: description,
      scheduleType,
      intervalMinutes: scheduleType === 'interval' ? intervalMinutes : undefined,
      isEnabled: true,
    });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create New Scheduler</DialogTitle>
      <DialogContent>
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Scheduler Name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              multiline
              rows={2}
            />
          </Grid>
          <Grid item xs={6}>
            <FormControl fullWidth>
              <InputLabel>Schedule Type</InputLabel>
              <Select
                value={scheduleType}
                onChange={(e) => setScheduleType(e.target.value)}
                label="Schedule Type"
              >
                <MenuItem value="interval">Interval</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={6}>
            <TextField
              fullWidth
              label="Interval (minutes)"
              type="number"
              value={intervalMinutes}
              onChange={(e) => setIntervalMinutes(Number(e.target.value))}
              inputProps={{ min: 1 }}
            />
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          disabled={!name || createMutation.isPending}
        >
          {createMutation.isPending ? 'Creating...' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default SchedulerSelector;
