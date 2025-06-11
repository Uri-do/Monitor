import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { signalRService } from '../services/signalRService';

interface RealtimeContextType {
  isEnabled: boolean;
  isConnected: boolean;
  enableRealtime: () => Promise<void>;
  disableRealtime: () => Promise<void>;
  toggleRealtime: () => Promise<void>;
}

const RealtimeContext = createContext<RealtimeContextType | undefined>(undefined);

interface RealtimeProviderProps {
  children: ReactNode;
}

export const RealtimeProvider: React.FC<RealtimeProviderProps> = ({ children }) => {
  const [isEnabled, setIsEnabled] = useState(false);
  const [isConnected, setIsConnected] = useState(false);

  const enableRealtime = async () => {
    try {
      await signalRService.connect();
      setIsConnected(true);
      setIsEnabled(true);
    } catch (error) {
      console.error('Failed to enable real-time features:', error);
      throw error;
    }
  };

  const disableRealtime = async () => {
    try {
      await signalRService.disconnect();
      setIsConnected(false);
      setIsEnabled(false);
    } catch (error) {
      console.error('Failed to disable real-time features:', error);
      throw error;
    }
  };

  const toggleRealtime = async () => {
    if (isEnabled) {
      await disableRealtime();
    } else {
      await enableRealtime();
    }
  };

  // Monitor SignalR connection status
  useEffect(() => {
    const checkConnection = () => {
      const connected = signalRService.isConnected();
      setIsConnected(connected);
      if (!connected && isEnabled) {
        setIsEnabled(false);
      }
    };

    // Check connection status periodically
    const interval = setInterval(checkConnection, 5000);

    return () => clearInterval(interval);
  }, [isEnabled]);

  const value: RealtimeContextType = {
    isEnabled,
    isConnected,
    enableRealtime,
    disableRealtime,
    toggleRealtime,
  };

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
};

export const useRealtime = (): RealtimeContextType => {
  const context = useContext(RealtimeContext);
  if (context === undefined) {
    console.error('useRealtime must be used within a RealtimeProvider. Providing fallback values.');

    // Provide fallback values instead of throwing error
    return {
      isEnabled: false,
      isConnected: false,
      enableRealtime: async () => {
        console.warn('RealtimeProvider not available - enableRealtime called');
      },
      disableRealtime: async () => {
        console.warn('RealtimeProvider not available - disableRealtime called');
      },
      toggleRealtime: async () => {
        console.warn('RealtimeProvider not available - toggleRealtime called');
      },
    };
  }
  return context;
};
