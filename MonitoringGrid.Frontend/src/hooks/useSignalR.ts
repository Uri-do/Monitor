import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

export interface SignalRConnection {
  connection: signalR.HubConnection | null;
  isConnected: boolean;
  isConnecting: boolean;
  error: string | null;
  retryCount: number;
  maxRetriesReached: boolean;
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
  joinGroup: (groupName: string) => Promise<void>;
  leaveGroup: (groupName: string) => Promise<void>;
  on: (methodName: string, callback: (...args: any[]) => void) => void;
  off: (methodName: string, callback?: (...args: any[]) => void) => void;
  invoke: (methodName: string, ...args: any[]) => Promise<any>;
}

export interface UseSignalROptions {
  url: string;
  automaticReconnect?: boolean;
  maxRetryAttempts?: number;
  accessTokenFactory?: () => string | Promise<string>;
  onConnected?: () => void;
  onDisconnected?: (error?: Error) => void;
  onReconnecting?: (error?: Error) => void;
  onReconnected?: (connectionId?: string) => void;
  onMaxRetriesReached?: () => void;
}

export const useSignalR = (options: UseSignalROptions): SignalRConnection => {
  const {
    url,
    automaticReconnect = true,
    maxRetryAttempts = 3,
    accessTokenFactory,
    onConnected,
    onDisconnected,
    onReconnecting,
    onReconnected,
    onMaxRetriesReached
  } = options;

  const [isConnected, setIsConnected] = useState(false);
  const [isConnecting, setIsConnecting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);
  const [maxRetriesReached, setMaxRetriesReached] = useState(false);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  // Create connection
  const createConnection = useCallback(() => {
    // If we have an existing connection, check its state
    if (connectionRef.current) {
      const state = connectionRef.current.state;
      // If it's disconnected, we can reuse it
      if (state === signalR.HubConnectionState.Disconnected) {
        return connectionRef.current;
      }
      // If it's in any other state (connecting, connected, reconnecting), return it
      if (state !== signalR.HubConnectionState.Disconnected) {
        return connectionRef.current;
      }
    }

    const connectionBuilder = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: accessTokenFactory,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      });

    if (automaticReconnect) {
      connectionBuilder.withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Check if we've exceeded max retry attempts
          if (retryContext.previousRetryCount >= maxRetryAttempts) {
            console.log(`SignalR max retry attempts (${maxRetryAttempts}) reached. Stopping reconnection attempts.`);
            setMaxRetriesReached(true);
            onMaxRetriesReached?.();
            return null; // Stop retrying
          }

          // Exponential backoff: 0, 2, 10, 30 seconds
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        }
      });
    }

    const connection = connectionBuilder.build();

    // Set up event handlers
    connection.onclose((error) => {
      setIsConnected(false);
      setIsConnecting(false);
      if (error) {
        setError(error.message);
        console.error('SignalR connection closed with error:', error);
      }
      onDisconnected?.(error);
    });

    connection.onreconnecting((error) => {
      setIsConnected(false);
      setIsConnecting(true);
      setRetryCount(prev => prev + 1);
      if (error) {
        setError(error.message);
        console.warn(`SignalR reconnecting (attempt ${retryCount + 1}/${maxRetryAttempts}):`, error);
      }
      onReconnecting?.(error);
    });

    connection.onreconnected((connectionId) => {
      setIsConnected(true);
      setIsConnecting(false);
      setError(null);
      setRetryCount(0); // Reset retry count on successful reconnection
      setMaxRetriesReached(false);
      console.log('SignalR reconnected:', connectionId);
      onReconnected?.(connectionId);
    });

    connectionRef.current = connection;
    return connection;
  }, [url, automaticReconnect, accessTokenFactory, onConnected, onDisconnected, onReconnecting, onReconnected]);

  // Connect function
  const connect = useCallback(async () => {
    // Check if we're already connecting or connected
    if (isConnecting || maxRetriesReached) {
      return;
    }

    // Check the actual connection state
    if (connectionRef.current) {
      const state = connectionRef.current.state;
      if (state === signalR.HubConnectionState.Connected ||
          state === signalR.HubConnectionState.Connecting ||
          state === signalR.HubConnectionState.Reconnecting) {
        return;
      }
    }

    setIsConnecting(true);
    setError(null);

    try {
      const connection = createConnection();

      // Only start if the connection is in Disconnected state
      if (connection.state === signalR.HubConnectionState.Disconnected) {
        await connection.start();
      }

      setIsConnected(true);
      setIsConnecting(false);
      setRetryCount(0); // Reset retry count on successful connection
      setMaxRetriesReached(false);
      console.log('SignalR connected successfully');
      onConnected?.();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error';
      setError(errorMessage);
      setIsConnecting(false);
      setIsConnected(false);
      console.error('SignalR connection failed:', err);
    }
  }, [isConnecting, maxRetriesReached, createConnection, onConnected]);

  // Disconnect function
  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      try {
        const state = connectionRef.current.state;
        if (state !== signalR.HubConnectionState.Disconnected) {
          await connectionRef.current.stop();
        }
        setIsConnected(false);
        setIsConnecting(false);
        setError(null);
        console.log('SignalR disconnected');
      } catch (err) {
        console.error('Error disconnecting SignalR:', err);
      }
    }
  }, []);

  // Join group function
  const joinGroup = useCallback(async (groupName: string) => {
    if (connectionRef.current && isConnected) {
      try {
        await connectionRef.current.invoke('JoinGroup', groupName);
        console.log(`Joined SignalR group: ${groupName}`);
      } catch (err) {
        console.error(`Error joining group ${groupName}:`, err);
      }
    }
  }, [isConnected]);

  // Leave group function
  const leaveGroup = useCallback(async (groupName: string) => {
    if (connectionRef.current && isConnected) {
      try {
        await connectionRef.current.invoke('LeaveGroup', groupName);
        console.log(`Left SignalR group: ${groupName}`);
      } catch (err) {
        console.error(`Error leaving group ${groupName}:`, err);
      }
    }
  }, [isConnected]);

  // Event listener functions
  const on = useCallback((methodName: string, callback: (...args: any[]) => void) => {
    if (connectionRef.current) {
      connectionRef.current.on(methodName, callback);
    }
  }, []);

  const off = useCallback((methodName: string, callback?: (...args: any[]) => void) => {
    if (connectionRef.current) {
      if (callback) {
        connectionRef.current.off(methodName, callback);
      } else {
        connectionRef.current.off(methodName);
      }
    }
  }, []);

  // Invoke function
  const invoke = useCallback(async (methodName: string, ...args: any[]) => {
    if (connectionRef.current && isConnected) {
      try {
        return await connectionRef.current.invoke(methodName, ...args);
      } catch (err) {
        console.error(`Error invoking ${methodName}:`, err);
        throw err;
      }
    } else {
      throw new Error('SignalR connection is not established');
    }
  }, [isConnected]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop().catch(console.error);
      }
    };
  }, []);

  return {
    connection: connectionRef.current,
    isConnected,
    isConnecting,
    error,
    retryCount,
    maxRetriesReached,
    connect,
    disconnect,
    joinGroup,
    leaveGroup,
    on,
    off,
    invoke
  };
};

export default useSignalR;
