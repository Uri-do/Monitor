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
    : (kpiDashboard?.runningKpis || []).map(kpi => ({
        kpiId: kpi.kpiId,
        indicator: kpi.indicator,
        owner: kpi.owner,
        startTime: new Date().toISOString(), // Fallback since KpiStatusDto doesn't have startTime
        progress: undefined,
        estimatedCompletion: undefined,
      }));



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
