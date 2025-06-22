import React from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  IconButton,
  Tooltip,
  Switch,
  FormControlLabel,
} from '@mui/material';
import {
  Schedule as ScheduleIcon,
  PlayArrow as PlayIcon,
  Pause as PauseIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { safeFormatDate } from '@/utils/dateUtils';
import { SchedulerDto } from '@/types/api';

/**
 * Scheduler Card Component
 * Displays scheduler information with controls
 */

export interface SchedulerCardProps {
  scheduler: SchedulerDto;
  onEdit?: (scheduler: SchedulerDto) => void;
  onDelete?: (scheduler: SchedulerDto) => void;
  onToggleActive?: (scheduler: SchedulerDto, active: boolean) => void;
  onExecuteNow?: (scheduler: SchedulerDto) => void;
  showControls?: boolean;
  compact?: boolean;
}

export const SchedulerCard: React.FC<SchedulerCardProps> = ({
  scheduler,
  onEdit,
  onDelete,
  onToggleActive,
  onExecuteNow,
  showControls = true,
  compact = false,
}) => {
  const getStatusColor = (isActive: boolean) => {
    return isActive ? 'success' : 'default';
  };

  const getStatusLabel = (isActive: boolean) => {
    return isActive ? 'Active' : 'Inactive';
  };

  const formatCronExpression = (cron: string) => {
    // Simple cron expression formatting
    const parts = cron.split(' ');
    if (parts.length >= 5) {
      const [minute, hour, day, month, dayOfWeek] = parts;
      
      // Common patterns
      if (cron === '0 * * * *') return 'Every hour';
      if (cron === '0 0 * * *') return 'Daily at midnight';
      if (cron === '0 0 * * 0') return 'Weekly on Sunday';
      if (cron === '0 0 1 * *') return 'Monthly on 1st';
      
      // Custom formatting
      let description = '';
      if (minute === '0' && hour !== '*') {
        description = `At ${hour}:00`;
      } else if (minute !== '*' && hour !== '*') {
        description = `At ${hour}:${minute.padStart(2, '0')}`;
      } else if (minute !== '*') {
        description = `Every hour at minute ${minute}`;
      } else {
        description = cron;
      }
      
      return description;
    }
    
    return cron;
  };

  return (
    <Card
      sx={{
        height: compact ? 'auto' : '100%',
        transition: 'all 0.2s ease-in-out',
        '&:hover': {
          transform: 'translateY(-2px)',
          boxShadow: 4,
        },
      }}
    >
      <CardContent sx={{ 
        height: '100%', 
        display: 'flex', 
        flexDirection: 'column',
        p: compact ? 2 : 3,
      }}>
        {/* Header */}
        <Box display="flex" justifyContent="space-between" alignItems="flex-start" mb={2}>
          <Box display="flex" alignItems="center" gap={1}>
            <ScheduleIcon sx={{ color: 'primary.main' }} />
            <Typography 
              variant={compact ? "subtitle2" : "h6"} 
              sx={{ fontWeight: 600 }}
            >
              {scheduler.schedulerName}
            </Typography>
          </Box>
          
          <Box display="flex" alignItems="center" gap={0.5}>
            <Chip
              label={getStatusLabel(scheduler.isActive)}
              color={getStatusColor(scheduler.isActive) as any}
              size="small"
              sx={{ fontWeight: 600 }}
            />
            {showControls && (
              <Tooltip title="More info">
                <IconButton size="small">
                  <InfoIcon />
                </IconButton>
              </Tooltip>
            )}
          </Box>
        </Box>

        {/* Schedule Information */}
        <Box sx={{ flexGrow: 1, mb: 2 }}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            <strong>Schedule:</strong> {formatCronExpression(scheduler.cronExpression)}
          </Typography>
          
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            <strong>Cron:</strong> <code>{scheduler.cronExpression}</code>
          </Typography>

          {scheduler.lastRunTime && (
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              <strong>Last Run:</strong> {safeFormatDate(scheduler.lastRunTime, 'MMM dd, yyyy HH:mm', 'Never')}
            </Typography>
          )}

          {scheduler.nextRunTime && (
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              <strong>Next Run:</strong> {format(parseISO(scheduler.nextRunTime), 'MMM dd, yyyy HH:mm')}
            </Typography>
          )}

          {scheduler.description && (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              {scheduler.description}
            </Typography>
          )}
        </Box>

        {/* Controls */}
        {showControls && (
          <Box>
            {/* Active Toggle */}
            <Box sx={{ mb: 2 }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={scheduler.isActive}
                    onChange={(e) => onToggleActive?.(scheduler, e.target.checked)}
                    size="small"
                  />
                }
                label="Active"
                sx={{ m: 0 }}
              />
            </Box>

            {/* Action Buttons */}
            <Box display="flex" justifyContent="space-between" alignItems="center">
              <Box display="flex" gap={1}>
                {onEdit && (
                  <Tooltip title="Edit scheduler">
                    <IconButton
                      size="small"
                      onClick={() => onEdit(scheduler)}
                      sx={{
                        backgroundColor: 'primary.main',
                        color: 'white',
                        '&:hover': {
                          backgroundColor: 'primary.dark',
                        },
                      }}
                    >
                      <EditIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                )}
                
                {onDelete && (
                  <Tooltip title="Delete scheduler">
                    <IconButton
                      size="small"
                      onClick={() => onDelete(scheduler)}
                      sx={{
                        backgroundColor: 'error.main',
                        color: 'white',
                        '&:hover': {
                          backgroundColor: 'error.dark',
                        },
                      }}
                    >
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                )}
              </Box>

              {onExecuteNow && scheduler.isActive && (
                <Tooltip title="Execute now">
                  <IconButton
                    size="small"
                    onClick={() => onExecuteNow(scheduler)}
                    sx={{
                      backgroundColor: 'success.main',
                      color: 'white',
                      '&:hover': {
                        backgroundColor: 'success.dark',
                      },
                    }}
                  >
                    <PlayIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
            </Box>
          </Box>
        )}

        {/* Status Indicator */}
        <Box
          sx={{
            position: 'absolute',
            top: 8,
            right: 8,
            width: 8,
            height: 8,
            borderRadius: '50%',
            backgroundColor: scheduler.isActive ? 'success.main' : 'grey.400',
            animation: scheduler.isActive ? 'pulse 2s infinite' : 'none',
            '@keyframes pulse': {
              '0%': { opacity: 1, transform: 'scale(1)' },
              '50%': { opacity: 0.6, transform: 'scale(1.2)' },
              '100%': { opacity: 1, transform: 'scale(1)' },
            },
          }}
        />
      </CardContent>
    </Card>
  );
};

export default SchedulerCard;
