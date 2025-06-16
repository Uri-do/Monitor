import React from 'react';
import { useNavigate } from 'react-router-dom';
import { IndicatorDashboardDto } from '@/types/api';
import { RunningIndicatorsDisplay, RunningIndicator } from '@/components/Business';

interface RealtimeRunningIndicator {
  indicatorId: number;
  indicator: string;
  owner: string;
  startTime: string;
  progress?: number;
  estimatedCompletion?: string;
  currentStep?: string;
  elapsedTime?: number;
}

interface RunningIndicatorsCardProps {
  indicatorDashboard?: IndicatorDashboardDto;
  realtimeRunningIndicators?: RealtimeRunningIndicator[];
}

const RunningIndicatorsCard: React.FC<RunningIndicatorsCardProps> = ({
  indicatorDashboard,
  realtimeRunningIndicators = [],
}) => {
  const navigate = useNavigate();

  // Use real-time data if available, otherwise fall back to dashboard data
  const realtimeArray = Array.isArray(realtimeRunningIndicators) ? realtimeRunningIndicators : [];
  const dashboardArray = Array.isArray(indicatorDashboard?.runningIndicators) ? indicatorDashboard.runningIndicators : [];

  const runningIndicators: RunningIndicator[] =
    realtimeArray.length > 0
      ? realtimeArray.map(indicator => ({
          indicatorID: indicator.indicatorId,
          indicator: indicator.indicator,
          owner: indicator.owner,
          startTime: indicator.startTime,
          progress: indicator.progress,
          estimatedCompletion: indicator.estimatedCompletion,
        }))
      : dashboardArray.map(indicator => ({
          indicatorID: indicator.indicatorId,
          indicator: indicator.indicatorName,
          owner: 'System', // Fallback since IndicatorStatusDto doesn't have owner
          startTime: new Date().toISOString(), // Fallback since we don't have startTime
          progress: undefined,
          estimatedCompletion: undefined,
        }));

  return (
    <RunningIndicatorsDisplay
      runningIndicators={runningIndicators}
      variant="card"
      title="Indicator Execution Monitor"
      showNavigateButton={true}
      onNavigate={() => navigate('/indicators')}
      showProgress={true}
    />
  );
};

export default RunningIndicatorsCard;
