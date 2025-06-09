import React from 'react';
import { useNavigate } from 'react-router-dom';
import { KpiDashboardDto } from '../../../types/api';
import { RunningKpisDisplay, RunningKpi } from '../../../components/Common';

interface RealtimeRunningKpi {
  kpiId: number;
  indicator: string;
  owner: string;
  startTime: string;
  progress?: number;
  estimatedCompletion?: string;
  currentStep?: string;
  elapsedTime?: number;
}

interface RunningKpisCardProps {
  kpiDashboard?: KpiDashboardDto;
  realtimeRunningKpis?: RealtimeRunningKpi[];
}

const RunningKpisCard: React.FC<RunningKpisCardProps> = ({
  kpiDashboard,
  realtimeRunningKpis = []
}) => {
  const navigate = useNavigate();

  // Use real-time data if available, otherwise fall back to dashboard data
  const runningKpis: RunningKpi[] = realtimeRunningKpis.length > 0
    ? realtimeRunningKpis
    : kpiDashboard?.runningKpis || [];

  // Debug logging
  console.log('RunningKpisCard - realtimeRunningKpis:', realtimeRunningKpis);
  console.log('RunningKpisCard - kpiDashboard?.runningKpis:', kpiDashboard?.runningKpis);
  console.log('RunningKpisCard - final runningKpis:', runningKpis);

  return (
    <RunningKpisDisplay
      runningKpis={runningKpis}
      variant="card"
      title="KPI Execution Monitor"
      showNavigateButton={true}
      onNavigate={() => navigate('/kpis')}
      showProgress={true}
    />
  );
};

export default RunningKpisCard;
