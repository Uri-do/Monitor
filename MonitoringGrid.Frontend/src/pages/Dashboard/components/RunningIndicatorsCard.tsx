import React from 'react';
import { useNavigate } from 'react-router-dom';
import { IndicatorDashboardDto } from '../../../types/api';
import { RunningKpisDisplay, RunningKpi } from '@/components/Business';

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
  const runningIndicators: RunningKpi[] =
    realtimeRunningIndicators.length > 0
      ? realtimeRunningIndicators.map(indicator => ({
          kpiId: indicator.indicatorId,
          indicator: indicator.indicator,
          owner: indicator.owner,
          startTime: indicator.startTime,
          progress: indicator.progress,
          estimatedCompletion: indicator.estimatedCompletion,
        }))
      : (indicatorDashboard?.indicatorStatuses?.filter(status => status.isCurrentlyRunning) || []).map(indicator => ({
          kpiId: indicator.indicatorID,
          indicator: indicator.indicatorName,
          owner: 'System', // Fallback since IndicatorStatusDto doesn't have owner
          startTime: new Date().toISOString(), // Fallback since we don't have startTime
          progress: undefined,
          estimatedCompletion: undefined,
        }));

  return (
    <RunningKpisDisplay
      runningKpis={runningIndicators}
      variant="card"
      title="Indicator Execution Monitor"
      showNavigateButton={true}
      onNavigate={() => navigate('/indicators')}
      showProgress={true}
    />
  );
};

export default RunningIndicatorsCard;
