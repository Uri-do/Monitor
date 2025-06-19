import { useState, useEffect, useRef } from 'react';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';
import { useRealtime } from '@/contexts/RealtimeContext';
import {
  useSignalR,
  IndicatorExecutionStarted,
  IndicatorExecutionCompleted
} from '@/services/signalRService';
import { ExecutionLogEntry } from '../types';

interface UseLiveExecutionLogProps {
  maxEntries: number;
  autoScroll: boolean;
  showOnlyErrors: boolean;
}

export const useLiveExecutionLog = ({
  maxEntries,
  autoScroll,
  showOnlyErrors,
}: UseLiveExecutionLogProps) => {
  const [logEntries, setLogEntries] = useState<ExecutionLogEntry[]>([]);
  const [isPaused, setIsPaused] = useState(false);
  const [isExpanded, setIsExpanded] = useState(true);
  const [filterErrors, setFilterErrors] = useState(showOnlyErrors);
  const logContainerRef = useRef<HTMLDivElement>(null);

  const dashboardState = useRealtimeDashboard();
  const realtimeContext = useRealtime();
  const signalR = useSignalR();

  // Add new log entries from real-time events
  useEffect(() => {
    if (isPaused) return;

    // Handle execution started events
    (dashboardState.runningIndicators || []).forEach(indicator => {
      const existingEntry = logEntries.find(
        entry => entry.indicatorID === indicator.indicatorID && entry.type === 'started'
      );

      if (!existingEntry) {
        const newEntry: ExecutionLogEntry = {
          id: `${indicator.indicatorID}-started-${Date.now()}`,
          timestamp: new Date(indicator.startTime),
          indicatorID: indicator.indicatorID,
          indicator: indicator.indicator,
          type: 'started',
          message: `Started execution`,
          details: {
            collectorID: undefined, // Will be populated from SignalR events
          }
        };

        setLogEntries(prev => [newEntry, ...prev].slice(0, maxEntries));
      }
    });
  }, [dashboardState.runningIndicators, isPaused, maxEntries, logEntries]);

  // Auto-scroll to top when new entries are added
  useEffect(() => {
    if (autoScroll && logContainerRef.current) {
      logContainerRef.current.scrollTop = 0;
    }
  }, [logEntries, autoScroll]);

  // Listen for SignalR execution events
  useEffect(() => {
    if (!realtimeContext.isConnected) return;

    // Handle IndicatorExecutionStarted events
    const handleExecutionStarted = (data: IndicatorExecutionStarted) => {
      console.log('📊 Execution started event received:', data);

      // Only process if not paused
      if (isPaused) {
        console.log('⏸️ Log is paused, ignoring execution started event');
        return;
      }

      const newEntry: ExecutionLogEntry = {
        id: `${data.indicatorID}-started-${Date.now()}`,
        timestamp: new Date(data.startTime),
        indicatorID: data.indicatorID,
        indicator: data.indicatorName,
        type: 'started',
        message: `Started execution`,
        details: {
          executionContext: data.executionContext,
          collectorID: data.collectorID,
          collectorItemName: data.collectorItemName,
          lastMinutes: data.lastMinutes,
        }
      };

      setLogEntries(prev => [newEntry, ...prev].slice(0, maxEntries));
    };

    // Handle IndicatorExecutionCompleted events
    const handleExecutionCompleted = (data: IndicatorExecutionCompleted) => {
      console.log('✅ Execution completed event received:', data);

      // Only process if not paused
      if (isPaused) {
        console.log('⏸️ Log is paused, ignoring execution completed event');
        return;
      }

      const newEntry: ExecutionLogEntry = {
        id: `${data.indicatorId}-completed-${Date.now()}`,
        timestamp: new Date(data.completedAt),
        indicatorID: data.indicatorId,
        indicator: data.indicatorName,
        type: data.success ? 'completed' : 'error',
        message: data.success ? 'Execution completed successfully' : (data.errorMessage || 'Execution failed'),
        duration: data.duration,
        success: data.success,
        errorMessage: data.errorMessage,
        executionHistoryId: data.executionHistoryId,
        details: {
          value: data.value,
          executionContext: data.executionContext,
          thresholdBreached: data.thresholdBreached,
          alertThreshold: data.alertThreshold,
          alertOperator: data.alertOperator,
          alertsGenerated: data.alertsGenerated,
        }
      };

      setLogEntries(prev => [newEntry, ...prev].slice(0, maxEntries));
    };

    // Register event handlers - use the "on" prefix to match the event handler names
    signalR.on('onIndicatorExecutionStarted', handleExecutionStarted);
    signalR.on('onIndicatorExecutionCompleted', handleExecutionCompleted);

    // Cleanup function
    return () => {
      signalR.off('onIndicatorExecutionStarted', handleExecutionStarted);
      signalR.off('onIndicatorExecutionCompleted', handleExecutionCompleted);
    };
  }, [realtimeContext.isConnected, maxEntries, signalR]);

  const clearLog = () => {
    setLogEntries([]);
  };

  const errorCount = logEntries.filter(entry => entry.type === 'error').length;

  return {
    logEntries,
    isPaused,
    isExpanded,
    filterErrors,
    logContainerRef,
    errorCount,
    setIsPaused,
    setIsExpanded,
    setFilterErrors,
    clearLog,
  };
};
