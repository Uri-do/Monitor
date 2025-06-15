import { useState, useEffect, useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useSignalR } from '@/services/signalRService';
import {
  WorkerStatusUpdate,
  IndicatorExecutionStarted,
  IndicatorExecutionProgress,
  IndicatorExecutionCompleted,
  CountdownUpdate,
  NextIndicatorScheduleUpdate,
  RunningIndicatorsUpdate,
} from '@/services/signalRService';
import { IndicatorDashboardDto } from '@/types/api';
import { useRealtime } from '@/contexts/RealtimeContext';
import { queryKeys } from '@/utils/queryKeys';

export interface RealtimeDashboardState {
  // Worker status
  workerStatus: WorkerStatusUpdate | null;

  // Indicator execution state
  runningIndicators: Array<{
    indicatorID: number;
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
  nextIndicatorDue: {
    indicatorID: number;
    indicator: string;
    owner: string;
    scheduledTime: string;
    minutesUntilDue: number;
  } | null;

  // Dashboard data
  dashboardData: IndicatorDashboardDto | null;

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
  const { isEnabled: realtimeEnabled, isConnected: realtimeConnected } = useRealtime();
  const queryClient = useQueryClient();

  const [state, setState] = useState<RealtimeDashboardState>({
    workerStatus: null,
    runningIndicators: [],
    countdown: null,
    nextIndicatorDue: null,
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

  // Indicator execution started handler
  const handleIndicatorExecutionStarted = useCallback((data: IndicatorExecutionStarted) => {
    setState(prev => ({
      ...prev,
      runningIndicators: [
        ...prev.runningIndicators.filter(indicator => indicator.indicatorID !== data.indicatorID),
        {
          indicatorID: data.indicatorID,
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

  // Indicator execution progress handler
  const handleIndicatorExecutionProgress = useCallback((data: IndicatorExecutionProgress) => {
    setState(prev => ({
      ...prev,
      runningIndicators: prev.runningIndicators.map(indicator =>
        indicator.indicatorID === data.indicatorID
          ? {
              ...indicator,
              progress: data.progress,
              currentStep: data.currentStep,
              elapsedTime: data.elapsedTime,
              estimatedCompletion: data.estimatedTimeRemaining
                ? new Date(Date.now() + data.estimatedTimeRemaining * 1000).toISOString()
                : undefined,
            }
          : indicator
      ),
      lastUpdate: new Date(),
    }));
  }, []);

  // Indicator execution completed handler
  const handleIndicatorExecutionCompleted = useCallback(
    (data: IndicatorExecutionCompleted) => {
      setState(prev => ({
        ...prev,
        runningIndicators: prev.runningIndicators.filter(
          indicator => indicator.indicatorID !== data.indicatorID
        ),
        lastUpdate: new Date(),
      }));

      // Update TanStack Query cache with fresh data
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.detail(data.indicatorID) });
      queryClient.invalidateQueries({
        queryKey: queryKeys.executionHistory.byIndicator(data.indicatorID),
      });
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.lists() });
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });

      // If execution generated alerts, refresh alert data
      if (data.alertsGenerated && data.alertsGenerated > 0) {
        queryClient.invalidateQueries({ queryKey: queryKeys.alerts.lists() });
        queryClient.invalidateQueries({ queryKey: queryKeys.alerts.statistics() });
      }
    },
    [queryClient]
  );

  // Countdown update handler
  const handleCountdownUpdate = useCallback((data: CountdownUpdate) => {
    setState(prev => ({
      ...prev,
      countdown: data.secondsUntilDue,
      nextIndicatorDue: {
        indicatorID: data.nextIndicatorID,
        indicator: data.indicator,
        owner: data.owner,
        scheduledTime: data.scheduledTime,
        minutesUntilDue: Math.ceil(data.secondsUntilDue / 60),
      },
      lastUpdate: new Date(),
    }));
  }, []);

  // Next Indicator schedule update handler
  const handleNextIndicatorScheduleUpdate = useCallback((data: NextIndicatorScheduleUpdate) => {
    if (data.nextIndicators.length > 0) {
      const nextIndicator = data.nextIndicators[0];
      setState(prev => ({
        ...prev,
        nextIndicatorDue: nextIndicator,
        countdown: nextIndicator.minutesUntilDue * 60,
        lastUpdate: new Date(),
      }));
    }
  }, []);

  // Running Indicators update handler
  const handleRunningIndicatorsUpdate = useCallback((data: RunningIndicatorsUpdate) => {
    setState(prev => ({
      ...prev,
      runningIndicators: data.runningIndicators,
      lastUpdate: new Date(),
    }));
  }, []);

  // Subscribe to real-time updates
  const subscribeToUpdates = useCallback(() => {
    if (realtimeEnabled && realtimeConnected) {
      // Join dashboard group
      joinGroup('Dashboard');

      // Set up event handlers
      on('onWorkerStatusUpdate', handleWorkerStatusUpdate);
      on('onIndicatorExecutionStarted', handleIndicatorExecutionStarted);
      on('onIndicatorExecutionProgress', handleIndicatorExecutionProgress);
      on('onIndicatorExecutionCompleted', handleIndicatorExecutionCompleted);
      on('onCountdownUpdate', handleCountdownUpdate);
      on('onNextIndicatorScheduleUpdate', handleNextIndicatorScheduleUpdate);
      on('onRunningIndicatorsUpdate', handleRunningIndicatorsUpdate);
    }
  }, [
    realtimeEnabled,
    realtimeConnected,
    joinGroup,
    on,
    handleWorkerStatusUpdate,
    handleIndicatorExecutionStarted,
    handleIndicatorExecutionProgress,
    handleIndicatorExecutionCompleted,
    handleCountdownUpdate,
    handleNextIndicatorScheduleUpdate,
    handleRunningIndicatorsUpdate,
  ]);

  // Unsubscribe from real-time updates
  const unsubscribeFromUpdates = useCallback(() => {
    leaveGroup('Dashboard');

    off('onWorkerStatusUpdate');
    off('onIndicatorExecutionStarted');
    off('onIndicatorExecutionProgress');
    off('onIndicatorExecutionCompleted');
    off('onCountdownUpdate');
    off('onNextIndicatorScheduleUpdate');
    off('onRunningIndicatorsUpdate');
  }, [leaveGroup, off]);

  // Auto-subscribe when connected and real-time is enabled
  useEffect(() => {
    if (realtimeEnabled && realtimeConnected) {
      subscribeToUpdates();
    }

    return () => {
      unsubscribeFromUpdates();
    };
  }, [realtimeEnabled, realtimeConnected, subscribeToUpdates, unsubscribeFromUpdates]);

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

    // Invalidate dashboard-related queries to trigger fresh data fetch
    queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.all });
    queryClient.invalidateQueries({ queryKey: queryKeys.indicators.lists() });
    queryClient.invalidateQueries({ queryKey: queryKeys.alerts.lists() });
  }, [queryClient]);

  return {
    ...state,
    isConnected: realtimeEnabled && realtimeConnected,
    refreshDashboard,
    subscribeToUpdates,
    unsubscribeFromUpdates,
  };
};
