import React from 'react';
import { Stack, Typography, CircularProgress } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { schedulerApi } from '@/services/api';
import { InfoItem } from '@/components/UI';

interface SchedulerDetailsProps {
  schedulerId: number;
  variant?: 'default' | 'compact' | 'prominent';
}

/**
 * Component to show scheduler details
 */
export const SchedulerDetails: React.FC<SchedulerDetailsProps> = ({ 
  schedulerId, 
  variant = 'default' 
}) => {
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
    <Stack spacing={variant === 'compact' ? 0.5 : 1}>
      <InfoItem 
        label="Scheduler Name" 
        value={scheduler.schedulerName} 
        variant={variant}
      />
      <InfoItem 
        label="Schedule Type" 
        value={scheduler.scheduleType} 
        variant={variant}
      />
      <InfoItem 
        label="Schedule" 
        value={scheduler.displayText} 
        variant={variant}
      />
      {scheduler.nextExecutionTime && (
        <InfoItem
          label="Next Execution"
          value={format(new Date(scheduler.nextExecutionTime), 'MMM dd, yyyy HH:mm')}
          variant={variant}
        />
      )}
      <InfoItem 
        label="Status" 
        value={scheduler.isEnabled ? 'Enabled' : 'Disabled'} 
        variant={variant}
      />
    </Stack>
  );
};

export default SchedulerDetails;
