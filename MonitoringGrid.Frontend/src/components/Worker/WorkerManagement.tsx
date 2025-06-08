import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Separator } from '@/components/ui/separator';
import {
  Play,
  Square,
  RotateCcw,
  Activity,
  Clock,
  Cpu,
  AlertCircle,
  CheckCircle,
  RefreshCw,
} from 'lucide-react';
import { toast } from 'sonner';

interface WorkerService {
  name: string;
  status: string;
  lastActivity?: string;
  errorMessage?: string;
}

interface WorkerStatus {
  isRunning: boolean;
  mode: string;
  processId?: number;
  startTime?: string;
  uptime?: string;
  services: WorkerService[];
  timestamp: string;
}

interface WorkerActionResult {
  success: boolean;
  message: string;
  processId?: number;
  timestamp: string;
}

const WorkerManagement: React.FC = () => {
  const [status, setStatus] = useState<WorkerStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const fetchStatus = async () => {
    try {
      const response = await fetch('/api/worker/status');
      if (response.ok) {
        const data = await response.json();
        setStatus(data);
      } else {
        console.error('Failed to fetch worker status');
      }
    } catch (error) {
      console.error('Error fetching worker status:', error);
    }
  };

  const performAction = async (action: string) => {
    setActionLoading(action);
    try {
      const response = await fetch(`/api/worker/${action}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const result: WorkerActionResult = await response.json();
      
      if (result.success) {
        toast.success(result.message);
        await fetchStatus(); // Refresh status
      } else {
        toast.error(result.message);
      }
    } catch (error) {
      toast.error(`Failed to ${action} worker`);
      console.error(`Error ${action}ing worker:`, error);
    } finally {
      setActionLoading(null);
    }
  };

  const formatUptime = (startTime?: string) => {
    if (!startTime) return 'N/A';
    
    const start = new Date(startTime);
    const now = new Date();
    const diff = now.getTime() - start.getTime();
    
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    
    return `${hours}h ${minutes}m`;
  };

  const formatDateTime = (dateTime?: string) => {
    if (!dateTime) return 'N/A';
    return new Date(dateTime).toLocaleString();
  };

  useEffect(() => {
    fetchStatus();
    
    if (autoRefresh) {
      const interval = setInterval(fetchStatus, 5000); // Refresh every 5 seconds
      return () => clearInterval(interval);
    }
  }, [autoRefresh]);

  if (!status) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="h-5 w-5" />
            Worker Management
          </CardTitle>
          <CardDescription>
            Loading worker status...
          </CardDescription>
        </CardHeader>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Activity className="h-5 w-5" />
                Worker Management
              </CardTitle>
              <CardDescription>
                Control and monitor the MonitoringGrid Worker service
              </CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setAutoRefresh(!autoRefresh)}
                className={autoRefresh ? 'bg-green-50' : ''}
              >
                <RefreshCw className={`h-4 w-4 ${autoRefresh ? 'animate-spin' : ''}`} />
                Auto Refresh
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={fetchStatus}
                disabled={loading}
              >
                <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                Refresh
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Status Overview */}
          <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
            <div className="flex items-center gap-3">
              {status.isRunning ? (
                <CheckCircle className="h-6 w-6 text-green-500" />
              ) : (
                <AlertCircle className="h-6 w-6 text-red-500" />
              )}
              <div>
                <div className="font-semibold">
                  Status: {' '}
                  <Badge variant={status.isRunning ? 'default' : 'destructive'}>
                    {status.isRunning ? 'Running' : 'Stopped'}
                  </Badge>
                </div>
                <div className="text-sm text-gray-600">
                  Mode: {status.mode}
                  {status.processId && ` (PID: ${status.processId})`}
                </div>
              </div>
            </div>
            
            <div className="text-right text-sm text-gray-600">
              <div className="flex items-center gap-1">
                <Clock className="h-4 w-4" />
                Uptime: {formatUptime(status.startTime)}
              </div>
              <div>Started: {formatDateTime(status.startTime)}</div>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex gap-2">
            <Button
              onClick={() => performAction('start')}
              disabled={status.isRunning || actionLoading === 'start' || status.mode === 'Integrated'}
              className="flex items-center gap-2"
            >
              <Play className="h-4 w-4" />
              {actionLoading === 'start' ? 'Starting...' : 'Start Worker'}
            </Button>
            
            <Button
              variant="destructive"
              onClick={() => performAction('stop')}
              disabled={!status.isRunning || actionLoading === 'stop' || status.mode === 'Integrated'}
              className="flex items-center gap-2"
            >
              <Square className="h-4 w-4" />
              {actionLoading === 'stop' ? 'Stopping...' : 'Stop Worker'}
            </Button>
            
            <Button
              variant="outline"
              onClick={() => performAction('restart')}
              disabled={!status.isRunning || actionLoading === 'restart' || status.mode === 'Integrated'}
              className="flex items-center gap-2"
            >
              <RotateCcw className="h-4 w-4" />
              {actionLoading === 'restart' ? 'Restarting...' : 'Restart Worker'}
            </Button>
          </div>

          {/* Integration Mode Alert */}
          {status.mode === 'Integrated' && (
            <Alert>
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>
                Worker services are running in integrated mode. To control them separately, 
                set <code>EnableWorkerServices</code> to <code>false</code> in configuration and restart the API.
              </AlertDescription>
            </Alert>
          )}

          <Separator />

          {/* Services Status */}
          <div>
            <h3 className="font-semibold mb-3 flex items-center gap-2">
              <Cpu className="h-4 w-4" />
              Worker Services ({status.services.length})
            </h3>
            
            {status.services.length > 0 ? (
              <div className="space-y-2">
                {status.services.map((service, index) => (
                  <div
                    key={index}
                    className="flex items-center justify-between p-3 border rounded-lg"
                  >
                    <div className="flex items-center gap-3">
                      <div className={`w-2 h-2 rounded-full ${
                        service.status === 'Running' ? 'bg-green-500' : 'bg-red-500'
                      }`} />
                      <div>
                        <div className="font-medium">{service.name}</div>
                        {service.errorMessage && (
                          <div className="text-sm text-red-600">{service.errorMessage}</div>
                        )}
                      </div>
                    </div>
                    
                    <div className="text-right text-sm text-gray-600">
                      <Badge variant={service.status === 'Running' ? 'default' : 'destructive'}>
                        {service.status}
                      </Badge>
                      {service.lastActivity && (
                        <div className="mt-1">
                          Last: {formatDateTime(service.lastActivity)}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-gray-500">
                No worker services running
              </div>
            )}
          </div>

          {/* Last Updated */}
          <div className="text-xs text-gray-500 text-center">
            Last updated: {formatDateTime(status.timestamp)}
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default WorkerManagement;
