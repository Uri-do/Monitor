import React from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AlertNotification, IndicatorExecutionResult, SystemStatus } from '../types/monitoring';
import { RealtimeStatusDto, LiveDashboardDto } from '../types/api';

// Real-time event interfaces
export interface WorkerStatusUpdate {
  isRunning: boolean;
  mode: string;
  processId?: number;
  services: Array<{ name: string; status: string }>;
  lastHeartbeat: string;
  uptime: string;
}

export interface IndicatorExecutionStarted {
  indicatorID: number;
  indicator: string;
  owner: string;
  startTime: string;
  estimatedDuration?: number;
}

export interface IndicatorExecutionProgress {
  indicatorID: number;
  indicator: string;
  progress: number; // 0-100
  currentStep: string;
  elapsedTime: number;
  estimatedTimeRemaining?: number;
}

export interface IndicatorExecutionCompleted {
  indicatorID: number;
  indicator: string;
  success: boolean;
  value?: number;
  duration: number;
  completedAt: string;
  errorMessage?: string;
  alertsGenerated?: number;
}

export interface CountdownUpdate {
  nextIndicatorID: number;
  indicator: string;
  owner: string;
  secondsUntilDue: number;
  scheduledTime: string;
}

export interface NextIndicatorScheduleUpdate {
  nextIndicators: Array<{
    indicatorID: number;
    indicator: string;
    owner: string;
    scheduledTime: string;
    minutesUntilDue: number;
  }>;
}

export interface RunningIndicatorsUpdate {
  runningIndicators: Array<{
    indicatorID: number;
    indicator: string;
    owner: string;
    startTime: string;
    progress?: number;
    estimatedCompletion?: string;
  }>;
}

export interface SignalREvents {
  onAlertTriggered: (alert: AlertNotification) => void;
  onIndicatorExecuted: (result: IndicatorExecutionResult) => void;
  onSystemStatusChanged: (status: SystemStatus) => void;
  onUserConnected: (userId: string) => void;
  onUserDisconnected: (userId: string) => void;
  onConnectionStateChanged: (state: string) => void;
  // Enhanced real-time events
  onStatusUpdate: (status: RealtimeStatusDto) => void;
  onDashboardUpdate: (dashboard: LiveDashboardDto) => void;
  onSystemHealthUpdate: (health: any) => void;
  onIndicatorExecutionWebhook: (data: any) => void;
  onAlertWebhook: (data: any) => void;
  onSystemStatusWebhook: (data: any) => void;
  // Worker service real-time events
  onWorkerStatusUpdate: (status: WorkerStatusUpdate) => void;
  onIndicatorExecutionStarted: (data: IndicatorExecutionStarted) => void;
  onIndicatorExecutionProgress: (data: IndicatorExecutionProgress) => void;
  onIndicatorExecutionCompleted: (data: IndicatorExecutionCompleted) => void;
  onCountdownUpdate: (data: CountdownUpdate) => void;
  onNextIndicatorScheduleUpdate: (data: NextIndicatorScheduleUpdate) => void;
  onRunningIndicatorsUpdate: (data: RunningIndicatorsUpdate) => void;
}

class SignalRService {
  private connection: HubConnection | null = null;
  private eventHandlers: Partial<SignalREvents> = {};
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 5000; // 5 seconds

  constructor() {
    // Don't initialize connection immediately - wait for authentication
    // this.initializeConnection();
  }

  private initializeConnection(): void {
    // Use the proxied URL for SignalR connection in development
    const hubUrl = '/monitoring-hub';

    console.log('Initializing SignalR connection to:', hubUrl);

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        withCredentials: true,
        // Temporarily disable authentication for testing
        // accessTokenFactory: () => {
        //   const token = localStorage.getItem('auth_token');
        //   console.log('SignalR requesting token, found:', token ? 'valid token' : 'no token');

        //   // Return null if no token to prevent malformed token errors
        //   if (!token || token.trim() === '') {
        //     console.log('No valid token available for SignalR connection');
        //     return null;
        //   }

        //   return token;
        // },
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          // Temporarily allow reconnection without token for testing
          // const token = localStorage.getItem('auth_token');
          // if (!token || token.trim() === '') {
          //   console.log('No token available, stopping SignalR reconnection attempts');
          //   return null; // Stop retrying if no token
          // }

          if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
            return this.reconnectDelay * Math.pow(2, retryContext.previousRetryCount);
          }
          return null; // Stop retrying
        },
      })
      .configureLogging(LogLevel.Information)
      .build();

    this.setupEventHandlers();
    this.setupConnectionEvents();
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Test events
    this.connection.on('TestMessage', (data: any) => {
      console.log('✅ SignalR Test Message received:', data);
    });

    this.connection.on('GroupTestMessage', (data: any) => {
      console.log('✅ SignalR Group Test Message received:', data);
    });

    // Alert events
    this.connection.on('AlertTriggered', (alert: AlertNotification) => {
      console.log('Alert triggered:', alert);
      this.eventHandlers.onAlertTriggered?.(alert);
    });

    this.connection.on('AlertResolved', (alertId: number) => {
      console.log('Alert resolved:', alertId);
      // Handle alert resolution
    });

    // Indicator execution events
    this.connection.on('IndicatorExecuted', (result: IndicatorExecutionResult) => {
      console.log('Indicator executed:', result);
      this.eventHandlers.onIndicatorExecuted?.(result);
    });

    this.connection.on('IndicatorExecutionStarted', (indicatorID: number) => {
      console.log('Indicator execution started:', indicatorID);
      // Handle execution start
    });

    // System status events
    this.connection.on('SystemStatusChanged', (status: SystemStatus) => {
      console.log('System status changed:', status);
      this.eventHandlers.onSystemStatusChanged?.(status);
    });

    // User presence events
    this.connection.on('UserConnected', (userId: string) => {
      console.log('User connected:', userId);
      this.eventHandlers.onUserConnected?.(userId);
    });

    this.connection.on('UserDisconnected', (userId: string) => {
      console.log('User disconnected:', userId);
      this.eventHandlers.onUserDisconnected?.(userId);
    });

    // Notification events
    this.connection.on('NotificationSent', (notification: any) => {
      console.log('Notification sent:', notification);
      // Handle notification
    });

    // Enhanced real-time events
    this.connection.on('StatusUpdate', (status: RealtimeStatusDto) => {
      console.log('Status update:', status);
      this.eventHandlers.onStatusUpdate?.(status);
    });

    this.connection.on('DashboardUpdate', (dashboard: LiveDashboardDto) => {
      console.log('Dashboard update:', dashboard);
      this.eventHandlers.onDashboardUpdate?.(dashboard);
    });

    this.connection.on('SystemHealthUpdate', (health: any) => {
      console.log('System health update:', health);
      this.eventHandlers.onSystemHealthUpdate?.(health);
    });

    // Webhook events
    this.connection.on('IndicatorExecutionWebhook', (data: any) => {
      console.log('Indicator execution webhook:', data);
      this.eventHandlers.onIndicatorExecutionWebhook?.(data);
    });

    this.connection.on('AlertWebhook', (data: any) => {
      console.log('Alert webhook:', data);
      this.eventHandlers.onAlertWebhook?.(data);
    });

    this.connection.on('SystemStatusWebhook', (data: any) => {
      console.log('System status webhook:', data);
      this.eventHandlers.onSystemStatusWebhook?.(data);
    });

    // Worker service real-time events
    this.connection.on('WorkerStatusUpdate', (status: WorkerStatusUpdate) => {
      console.log('🔧 Worker status update received:', status);
      this.eventHandlers.onWorkerStatusUpdate?.(status);
    });

    this.connection.on('IndicatorExecutionStarted', (data: IndicatorExecutionStarted) => {
      console.log('Indicator execution started (enhanced):', data);
      this.eventHandlers.onIndicatorExecutionStarted?.(data);
    });

    this.connection.on('IndicatorExecutionProgress', (data: IndicatorExecutionProgress) => {
      console.log('Indicator execution progress:', data);
      this.eventHandlers.onIndicatorExecutionProgress?.(data);
    });

    this.connection.on('IndicatorExecutionCompleted', (data: IndicatorExecutionCompleted) => {
      console.log('Indicator execution completed:', data);
      this.eventHandlers.onIndicatorExecutionCompleted?.(data);
    });

    this.connection.on('CountdownUpdate', (data: CountdownUpdate) => {
      console.log('🕒 Countdown update received:', data);
      this.eventHandlers.onCountdownUpdate?.(data);
    });

    this.connection.on('NextIndicatorScheduleUpdate', (data: NextIndicatorScheduleUpdate) => {
      console.log('Next Indicator schedule update:', data);
      this.eventHandlers.onNextIndicatorScheduleUpdate?.(data);
    });

    this.connection.on('RunningIndicatorsUpdate', (data: RunningIndicatorsUpdate) => {
      console.log('Running Indicators update:', data);
      this.eventHandlers.onRunningIndicatorsUpdate?.(data);
    });

    // Handle system status updates (if server sends them)
    this.connection.on('systemstatus', (data: any) => {
      console.log('System status update:', data);
      // Handle system status if needed
    });

    // Group join/leave events
    this.connection.on('JoinedGroup', (data: any) => {
      console.log('Joined group:', data);
    });

    this.connection.on('LeftGroup', (data: any) => {
      console.log('Left group:', data);
    });
  }

  private setupConnectionEvents(): void {
    if (!this.connection) return;

    this.connection.onclose(error => {
      console.log('SignalR connection closed:', error);
      this.eventHandlers.onConnectionStateChanged?.('Disconnected');
    });

    this.connection.onreconnecting(error => {
      console.log('SignalR reconnecting:', error);
      this.eventHandlers.onConnectionStateChanged?.('Reconnecting');
    });

    this.connection.onreconnected(connectionId => {
      console.log('SignalR reconnected:', connectionId);
      this.eventHandlers.onConnectionStateChanged?.('Connected');
      this.reconnectAttempts = 0;
    });
  }

  public async start(): Promise<void> {
    // Temporarily disable token check for testing
    // const token = localStorage.getItem('auth_token');
    // if (!token || token.trim() === '') {
    //   console.log('Cannot start SignalR connection: No authentication token available');
    //   this.eventHandlers.onConnectionStateChanged?.('Disconnected');
    //   return;
    // }

    if (!this.connection) {
      this.initializeConnection();
    }

    // Check if already connected or connecting
    if (this.connection!.state === 'Connected') {
      console.log('SignalR connection already connected');
      this.eventHandlers.onConnectionStateChanged?.('Connected');
      return;
    }

    if (this.connection!.state === 'Connecting') {
      console.log('SignalR connection already connecting');
      return;
    }

    try {
      await this.connection!.start();
      console.log('SignalR connection started successfully');
      this.eventHandlers.onConnectionStateChanged?.('Connected');
      this.reconnectAttempts = 0;
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      this.eventHandlers.onConnectionStateChanged?.('Failed');

      // Temporarily allow retry without token for testing
      // const currentToken = localStorage.getItem('auth_token');
      // if (currentToken && currentToken.trim() !== '' && this.reconnectAttempts < this.maxReconnectAttempts) {
      if (this.reconnectAttempts < this.maxReconnectAttempts) {
        this.reconnectAttempts++;
        setTimeout(() => this.start(), this.reconnectDelay);
      } else {
        console.log('Not retrying SignalR connection: Max attempts reached');
      }
    }
  }

  public async stop(): Promise<void> {
    if (this.connection && this.connection.state !== 'Disconnected') {
      await this.connection.stop();
      console.log('SignalR connection stopped');
      this.eventHandlers.onConnectionStateChanged?.('Disconnected');
    }
  }

  // Alias methods for RealtimeContext compatibility
  public async connect(): Promise<void> {
    return this.start();
  }

  public async disconnect(): Promise<void> {
    return this.stop();
  }

  public isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }

  public on<K extends keyof SignalREvents>(event: K, handler: SignalREvents[K]): void {
    this.eventHandlers[event] = handler;
  }

  public off<K extends keyof SignalREvents>(event: K): void {
    delete this.eventHandlers[event];
  }

  public async joinGroup(groupName: string): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      try {
        await this.connection.invoke('JoinGroup', groupName);
        console.log(`Joined group: ${groupName}`);
      } catch (error) {
        // Gracefully handle missing server method
        console.warn(`JoinGroup method not available on server for group ${groupName}:`, error);
      }
    }
  }

  public async leaveGroup(groupName: string): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      try {
        await this.connection.invoke('LeaveGroup', groupName);
        console.log(`Left group: ${groupName}`);
      } catch (error) {
        // Gracefully handle missing server method
        console.warn(`LeaveGroup method not available on server for group ${groupName}:`, error);
      }
    }
  }

  public async sendMessage(method: string, ...args: any[]): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      try {
        await this.connection.invoke(method, ...args);
      } catch (error) {
        console.error(`Error sending message ${method}:`, error);
      }
    }
  }

  public getConnectionState(): string {
    return this.connection?.state || 'Disconnected';
  }

  public getConnectionId(): string | null {
    return this.connection?.connectionId || null;
  }

  public updateAuthToken(token: string): void {
    // Store new token using the same key as authService
    localStorage.setItem('auth_token', token);

    // Restart connection with new token
    if (this.connection) {
      this.stop().then(() => {
        this.initializeConnection();
        this.start();
      });
    }
  }
}

// Create singleton instance
export const signalRService = new SignalRService();

// React hook for SignalR
export const useSignalR = () => {
  const [connectionState, setConnectionState] = React.useState<string>('Disconnected');
  const [isConnected, setIsConnected] = React.useState(false);

  React.useEffect(() => {
    const handleConnectionStateChanged = (state: string) => {
      setConnectionState(state);
      setIsConnected(state === 'Connected');
    };

    signalRService.on('onConnectionStateChanged', handleConnectionStateChanged);

    // Don't auto-start connection - let RealtimeContext manage it
    // signalRService.start();

    return () => {
      signalRService.off('onConnectionStateChanged');
    };
  }, []);

  return {
    connectionState,
    isConnected,
    joinGroup: signalRService.joinGroup.bind(signalRService),
    leaveGroup: signalRService.leaveGroup.bind(signalRService),
    sendMessage: signalRService.sendMessage.bind(signalRService),
    on: signalRService.on.bind(signalRService),
    off: signalRService.off.bind(signalRService),
  };
};

export default signalRService;
