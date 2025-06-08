import { useState, useEffect, useCallback } from 'react';
import { useSignalR } from '../services/signalRService';
import {
  WorkerStatusUpdate,
  KpiExecutionStarted,
  KpiExecutionProgress,
  KpiExecutionCompleted,
  CountdownUpdate,
  NextKpiScheduleUpdate,
  RunningKpisUpdate,
} from '../services/signalRService';
import { KpiDashboardDto } from '../types/api';

export interface RealtimeDashboardState {
  // Worker status
  workerStatus: WorkerStatusUpdate | null;
  
  // KPI execution state
  runningKpis: Array<{
    kpiId: number;
    indicator: string;
    owner: string;
    startTime: string;
    progress?: number;
    estimatedCompletion?: string;
    currentStep?: string;
    elapsedTime?: number;
  }>;
  
  // Countdown and scheduling
  countdown: number | null;
  nextKpiDue: {
    kpiId: number;
    indicator: string;
    owner: string;
    scheduledTime: string;
    minutesUntilDue: number;
  } | null;
  
  // Dashboard data
  dashboardData: KpiDashboardDto | null;
  
  // Connection state
  isConnected: boolean;
  lastUpdate: Date;
}

export interface RealtimeDashboardActions {
  refreshDashboard: () => void;
  subscribeToUpdates: () => void;
  unsubscribeFromUpdates: () => void;
}

export const useRealtimeDashboard = (): RealtimeDashboardState & RealtimeDashboardActions => {
  const [state, setState] = useState<RealtimeDashboardState>({
    workerStatus: null,
    runningKpis: [],
    countdown: null,
    nextKpiDue: null,
    dashboardData: null,
    isConnected: false,
    lastUpdate: new Date(),
  });

  const { isConnected, joinGroup, leaveGroup, on, off } = useSignalR();

  // Update connection state
  useEffect(() => {
    setState(prev => ({ ...prev, isConnected }));
  }, [isConnected]);

  // Worker status update handler
  const handleWorkerStatusUpdate = useCallback((status: WorkerStatusUpdate) => {
    setState(prev => ({
      ...prev,
      workerStatus: status,
      lastUpdate: new Date(),
    }));
  }, []);

  // KPI execution started handler
  const handleKpiExecutionStarted = useCallback((data: KpiExecutionStarted) => {
    setState(prev => ({
      ...prev,
      runningKpis: [
        ...prev.runningKpis.filter(kpi => kpi.kpiId !== data.kpiId),
        {
          kpiId: data.kpiId,
          indicator: data.indicator,
          owner: data.owner,
          startTime: data.startTime,
          progress: 0,
          currentStep: 'Starting...',
          elapsedTime: 0,
        },
      ],
      lastUpdate: new Date(),
    }));
  }, []);

  // KPI execution progress handler
  const handleKpiExecutionProgress = useCallback((data: KpiExecutionProgress) => {
    setState(prev => ({
      ...prev,
      runningKpis: prev.runningKpis.map(kpi =>
        kpi.kpiId === data.kpiId
          ? {
              ...kpi,
              progress: data.progress,
              currentStep: data.currentStep,
              elapsedTime: data.elapsedTime,
              estimatedCompletion: data.estimatedTimeRemaining
                ? new Date(Date.now() + data.estimatedTimeRemaining * 1000).toISOString()
                : undefined,
            }
          : kpi
      ),
      lastUpdate: new Date(),
    }));
  }, []);

  // KPI execution completed handler
  const handleKpiExecutionCompleted = useCallback((data: KpiExecutionCompleted) => {
    setState(prev => ({
      ...prev,
      runningKpis: prev.runningKpis.filter(kpi => kpi.kpiId !== data.kpiId),
      lastUpdate: new Date(),
    }));
  }, []);

  // Countdown update handler
  const handleCountdownUpdate = useCallback((data: CountdownUpdate) => {
    setState(prev => ({
      ...prev,
      countdown: data.secondsUntilDue,
      nextKpiDue: {
        kpiId: data.nextKpiId,
        indicator: data.indicator,
        owner: data.owner,
        scheduledTime: data.scheduledTime,
        minutesUntilDue: Math.ceil(data.secondsUntilDue / 60),
      },
      lastUpdate: new Date(),
    }));
  }, []);

  // Next KPI schedule update handler
  const handleNextKpiScheduleUpdate = useCallback((data: NextKpiScheduleUpdate) => {
    if (data.nextKpis.length > 0) {
      const nextKpi = data.nextKpis[0];
      setState(prev => ({
        ...prev,
        nextKpiDue: nextKpi,
        countdown: nextKpi.minutesUntilDue * 60,
        lastUpdate: new Date(),
      }));
    }
  }, []);

  // Running KPIs update handler
  const handleRunningKpisUpdate = useCallback((data: RunningKpisUpdate) => {
    setState(prev => ({
      ...prev,
      runningKpis: data.runningKpis,
      lastUpdate: new Date(),
    }));
  }, []);

  // Subscribe to real-time updates
  const subscribeToUpdates = useCallback(() => {
    if (isConnected) {
      // Join dashboard group
      joinGroup('Dashboard');
      
      // Set up event handlers
      on('onWorkerStatusUpdate', handleWorkerStatusUpdate);
      on('onKpiExecutionStarted', handleKpiExecutionStarted);
      on('onKpiExecutionProgress', handleKpiExecutionProgress);
      on('onKpiExecutionCompleted', handleKpiExecutionCompleted);
      on('onCountdownUpdate', handleCountdownUpdate);
      on('onNextKpiScheduleUpdate', handleNextKpiScheduleUpdate);
      on('onRunningKpisUpdate', handleRunningKpisUpdate);
    }
  }, [
    isConnected,
    joinGroup,
    on,
    handleWorkerStatusUpdate,
    handleKpiExecutionStarted,
    handleKpiExecutionProgress,
    handleKpiExecutionCompleted,
    handleCountdownUpdate,
    handleNextKpiScheduleUpdate,
    handleRunningKpisUpdate,
  ]);

  // Unsubscribe from real-time updates
  const unsubscribeFromUpdates = useCallback(() => {
    leaveGroup('Dashboard');
    
    off('onWorkerStatusUpdate');
    off('onKpiExecutionStarted');
    off('onKpiExecutionProgress');
    off('onKpiExecutionCompleted');
    off('onCountdownUpdate');
    off('onNextKpiScheduleUpdate');
    off('onRunningKpisUpdate');
  }, [leaveGroup, off]);

  // Auto-subscribe when connected
  useEffect(() => {
    if (isConnected) {
      subscribeToUpdates();
    }
    
    return () => {
      unsubscribeFromUpdates();
    };
  }, [isConnected, subscribeToUpdates, unsubscribeFromUpdates]);

  // Countdown timer - update every second
  useEffect(() => {
    if (state.countdown === null || state.countdown <= 0) return;

    const timer = setInterval(() => {
      setState(prev => {
        if (prev.countdown === null || prev.countdown <= 1) {
          return { ...prev, countdown: null };
        }
        return { ...prev, countdown: prev.countdown - 1 };
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [state.countdown]);

  const refreshDashboard = useCallback(() => {
    setState(prev => ({ ...prev, lastUpdate: new Date() }));
  }, []);

  return {
    ...state,
    refreshDashboard,
    subscribeToUpdates,
    unsubscribeFromUpdates,
  };
};
