import { useState, useEffect } from 'react';
import { useRealtimeDashboard } from '@/hooks/useRealtimeDashboard';
import { IndicatorExecutionCompleted } from '@/services/signalRService';
import { ExecutionError } from './ErrorItem';

interface UseExecutionErrorsProps {
  maxErrors: number;
}

export const useExecutionErrors = ({ maxErrors }: UseExecutionErrorsProps) => {
  const [executionErrors, setExecutionErrors] = useState<ExecutionError[]>([]);
  const [isExpanded, setIsExpanded] = useState(true);
  
  const dashboardState = useRealtimeDashboard();

  // Listen for execution completed events and capture errors
  useEffect(() => {
    const handleExecutionCompleted = (data: IndicatorExecutionCompleted) => {
      if (!data.success && data.errorMessage) {
        const newError: ExecutionError = {
          id: `${data.indicatorID}-${Date.now()}`,
          indicatorID: data.indicatorID,
          indicator: data.indicator,
          owner: data.owner || 'Unknown',
          errorMessage: data.errorMessage,
          duration: data.duration,
          completedAt: data.completedAt,
          collectorID: data.collectorID,
          collectorItemName: data.collectorItemName,
          lastMinutes: data.lastMinutes,
          executionContext: data.executionContext,
          timestamp: new Date(),
        };

        setExecutionErrors(prev => [newError, ...prev].slice(0, maxErrors));
      }
    };

    // Mock error generation for demonstration
    const interval = setInterval(() => {
      if (Math.random() > 0.8) { // 20% chance of error
        const mockError: ExecutionError = {
          id: `mock-${Date.now()}`,
          indicatorID: Math.floor(Math.random() * 3) + 1,
          indicator: `Test Indicator ${Math.floor(Math.random() * 3) + 1}`,
          owner: 'System',
          errorMessage: `Item 'TestItem${Math.floor(Math.random() * 3) + 1}' not found in collector results. Available items: ['item1', 'item2']`,
          duration: Math.floor(Math.random() * 5000) + 500,
          completedAt: new Date().toISOString(),
          collectorID: Math.floor(Math.random() * 3) + 1,
          collectorItemName: `TestItem${Math.floor(Math.random() * 3) + 1}`,
          lastMinutes: 10,
          executionContext: 'Scheduled',
          timestamp: new Date(),
        };

        setExecutionErrors(prev => [mockError, ...prev].slice(0, maxErrors));
      }
    }, 5000);

    return () => clearInterval(interval);
  }, [maxErrors]);

  const handleClearErrors = () => {
    setExecutionErrors([]);
  };

  const toggleExpanded = () => {
    setIsExpanded(!isExpanded);
  };

  return {
    executionErrors,
    isExpanded,
    handleClearErrors,
    toggleExpanded,
  };
};
