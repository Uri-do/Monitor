import React from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AlertNotification, KpiExecutionResult, SystemStatus } from '../types/monitoring';
import { RealtimeStatusDto, LiveDashboardDto } from '../types/api';

// Real-time event interfaces
export interface WorkerStatusUpdate {
  isRunning: boolean;
  mode: string;
  processId?: number;
  services: Array<{ name: string; status: string; }>;
  lastHeartbeat: string;
  uptime: string;
}

export interface KpiExecutionStarted {
  kpiId: number;
  indicator: string;
  owner: string;
  startTime: string;
  estimatedDuration?: number;
}

export interface KpiExecutionProgress {
  kpiId: number;
  indicator: string;
  progress: number; // 0-100
  currentStep: string;
  elapsedTime: number;
  estimatedTimeRemaining?: number;
}

export interface KpiExecutionCompleted {
  kpiId: number;
  indicator: string;
  success: boolean;
  value?: number;
  duration: number;
  completedAt: string;
  errorMessage?: string;
}

export interface CountdownUpdate {
  nextKpiId: number;
  indicator: string;
  owner: string;
  secondsUntilDue: number;
  scheduledTime: string;
}

export interface NextKpiScheduleUpdate {
  nextKpis: Array<{
    kpiId: number;
    indicator: string;
    owner: string;
    scheduledTime: string;
    minutesUntilDue: number;
  }>;
}

export interface RunningKpisUpdate {
  runningKpis: Array<{
    kpiId: number;
    indicator: string;
    owner: string;
    startTime: string;
    progress?: number;
    estimatedCompletion?: string;
  }>;
}

export interface SignalREvents {
  onAlertTriggered: (alert: AlertNotification) => void;
  onKpiExecuted: (result: KpiExecutionResult) => void;
  onSystemStatusChanged: (status: SystemStatus) => void;
  onUserConnected: (userId: string) => void;
  onUserDisconnected: (userId: string) => void;
  onConnectionStateChanged: (state: string) => void;
  // Enhanced real-time events
  onStatusUpdate: (status: RealtimeStatusDto) => void;
  onDashboardUpdate: (dashboard: LiveDashboardDto) => void;
  onSystemHealthUpdate: (health: any) => void;
  onKpiExecutionWebhook: (data: any) => void;
  onAlertWebhook: (data: any) => void;
  onSystemStatusWebhook: (data: any) => void;
  // Worker service real-time events
  onWorkerStatusUpdate: (status: WorkerStatusUpdate) => void;
  onKpiExecutionStarted: (data: KpiExecutionStarted) => void;
  onKpiExecutionProgress: (data: KpiExecutionProgress) => void;
  onKpiExecutionCompleted: (data: KpiExecutionCompleted) => void;
  onCountdownUpdate: (data: CountdownUpdate) => void;
  onNextKpiScheduleUpdate: (data: NextKpiScheduleUpdate) => void;
  onRunningKpisUpdate: (data: RunningKpisUpdate) => void;
}

class SignalRService {
  private connection: HubConnection | null = null;
  private eventHandlers: Partial<SignalREvents> = {};
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 5000; // 5 seconds

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    const baseUrl = (import.meta as any).env.VITE_API_BASE_URL || 'https://localhost:7001';

    this.connection = new HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/monitoring`, {
        withCredentials: true,
        headers: {
          Authorization: `Bearer ${localStorage.getItem('accessToken')}`,
        },
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
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

    // Alert events
    this.connection.on('AlertTriggered', (alert: AlertNotification) => {
      console.log('Alert triggered:', alert);
      this.eventHandlers.onAlertTriggered?.(alert);
    });

    this.connection.on('AlertResolved', (alertId: number) => {
      console.log('Alert resolved:', alertId);
      // Handle alert resolution
    });

    // KPI execution events
    this.connection.on('KpiExecuted', (result: KpiExecutionResult) => {
      console.log('KPI executed:', result);
      this.eventHandlers.onKpiExecuted?.(result);
    });

    this.connection.on('KpiExecutionStarted', (kpiId: number) => {
      console.log('KPI execution started:', kpiId);
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
    this.connection.on('KpiExecutionWebhook', (data: any) => {
      console.log('KPI execution webhook:', data);
      this.eventHandlers.onKpiExecutionWebhook?.(data);
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
      console.log('Worker status update:', status);
      this.eventHandlers.onWorkerStatusUpdate?.(status);
    });

    this.connection.on('KpiExecutionStarted', (data: KpiExecutionStarted) => {
      console.log('KPI execution started (enhanced):', data);
      this.eventHandlers.onKpiExecutionStarted?.(data);
    });

    this.connection.on('KpiExecutionProgress', (data: KpiExecutionProgress) => {
      console.log('KPI execution progress:', data);
      this.eventHandlers.onKpiExecutionProgress?.(data);
    });

    this.connection.on('KpiExecutionCompleted', (data: KpiExecutionCompleted) => {
      console.log('KPI execution completed:', data);
      this.eventHandlers.onKpiExecutionCompleted?.(data);
    });

    this.connection.on('CountdownUpdate', (data: CountdownUpdate) => {
      console.log('Countdown update:', data);
      this.eventHandlers.onCountdownUpdate?.(data);
    });

    this.connection.on('NextKpiScheduleUpdate', (data: NextKpiScheduleUpdate) => {
      console.log('Next KPI schedule update:', data);
      this.eventHandlers.onNextKpiScheduleUpdate?.(data);
    });

    this.connection.on('RunningKpisUpdate', (data: RunningKpisUpdate) => {
      console.log('Running KPIs update:', data);
      this.eventHandlers.onRunningKpisUpdate?.(data);
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
    if (!this.connection) {
      this.initializeConnection();
    }

    try {
      await this.connection!.start();
      console.log('SignalR connection started');
      this.eventHandlers.onConnectionStateChanged?.('Connected');
      this.reconnectAttempts = 0;
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      this.eventHandlers.onConnectionStateChanged?.('Failed');

      // Retry connection
      if (this.reconnectAttempts < this.maxReconnectAttempts) {
        this.reconnectAttempts++;
        setTimeout(() => this.start(), this.reconnectDelay);
      }
    }
  }

  public async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      console.log('SignalR connection stopped');
      this.eventHandlers.onConnectionStateChanged?.('Disconnected');
    }
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
        console.error(`Error joining group ${groupName}:`, error);
      }
    }
  }

  public async leaveGroup(groupName: string): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      try {
        await this.connection.invoke('LeaveGroup', groupName);
        console.log(`Left group: ${groupName}`);
      } catch (error) {
        console.error(`Error leaving group ${groupName}:`, error);
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
    // Store new token
    localStorage.setItem('accessToken', token);

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

    // Start connection
    signalRService.start();

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
