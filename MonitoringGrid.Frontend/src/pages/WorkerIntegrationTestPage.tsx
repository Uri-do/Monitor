import React, { useState, useEffect, useCallback } from 'react';
import { Card, Button, Select, Progress, Alert, Spin, Row, Col, Typography, Space, Tag, Statistic } from 'antd';
import { PlayCircleOutlined, StopOutlined, ReloadOutlined, ExportOutlined } from '@ant-design/icons';
import { useSignalR } from '../hooks/useSignalR';
import workerIntegrationTestService, { 
  WorkerIntegrationTestStatus, 
  WorkerTestExecution, 
  StartWorkerTestRequest 
} from '../services/workerIntegrationTestService';
import WorkerTestExecutionCard from '../components/WorkerIntegrationTest/WorkerTestExecutionCard';
import WorkerTestResultsModal from '../components/WorkerIntegrationTest/WorkerTestResultsModal';
import './WorkerIntegrationTestPage.css';

const { Title, Text } = Typography;
const { Option } = Select;

const WorkerIntegrationTestPage: React.FC = () => {
  const [status, setStatus] = useState<WorkerIntegrationTestStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeExecutions, setActiveExecutions] = useState<Map<string, WorkerTestExecution>>(new Map());
  const [selectedTestType, setSelectedTestType] = useState<string>('indicator-execution');
  const [selectedIndicators, setSelectedIndicators] = useState<number[]>([]);
  const [isStarting, setIsStarting] = useState(false);
  const [selectedExecution, setSelectedExecution] = useState<WorkerTestExecution | null>(null);
  const [showResults, setShowResults] = useState(false);

  // SignalR connection for real-time updates
  const {
    connection,
    isConnected,
    error: signalRError,
    retryCount,
    maxRetriesReached,
    connect,
    on,
    off,
    invoke
  } = useSignalR({
    url: '/hubs/worker-integration-test',
    automaticReconnect: true,
    maxRetryAttempts: 3,
    onConnected: () => {
      console.log('Connected to Worker Integration Test hub');
    },
    onDisconnected: (error) => {
      console.log('Disconnected from Worker Integration Test hub:', error);
    },
    onMaxRetriesReached: () => {
      console.error('SignalR max retry attempts reached. Please check if the API is running.');
    }
  });

  // Connect to SignalR on component mount
  useEffect(() => {
    connect().catch(console.error);
  }, [connect]);

  // Load initial data
  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      const statusData = await workerIntegrationTestService.getStatus();
      setStatus(statusData);
      
      // Load active executions
      const executions = new Map<string, WorkerTestExecution>();
      if (statusData.recentExecutions && Array.isArray(statusData.recentExecutions)) {
        for (const execution of statusData.recentExecutions) {
          if (execution.isRunning) {
            executions.set(execution.id, execution);
          }
        }
      }
      setActiveExecutions(executions);
    } catch (error) {
      console.error('Failed to load worker integration test status:', error);
    } finally {
      setLoading(false);
    }
  }, []);

  // Setup SignalR event handlers
  useEffect(() => {
    if (!isConnected) return;

    const handleTestStarted = (data: any) => {
      console.log('Worker test started:', data);
      loadData(); // Refresh status
    };

    const handleTestProgress = (data: any) => {
      console.log('Worker test progress:', data);
      setActiveExecutions(prev => {
        const updated = new Map(prev);
        const existing = updated.get(data.testId);
        if (existing) {
          updated.set(data.testId, {
            ...existing,
            status: data.status,
            progress: data.progress,
            lastUpdate: data.lastUpdate
          });
        }
        return updated;
      });
    };

    const handleTestCompleted = (data: any) => {
      console.log('Worker test completed:', data);
      setActiveExecutions(prev => {
        const updated = new Map(prev);
        updated.delete(data.testId);
        return updated;
      });
      loadData(); // Refresh status
    };

    const handleTestStopped = (data: any) => {
      console.log('Worker test stopped:', data);
      setActiveExecutions(prev => {
        const updated = new Map(prev);
        updated.delete(data.testId);
        return updated;
      });
      loadData(); // Refresh status
    };

    const handleIndicatorTestResult = (data: any) => {
      console.log('Indicator test result:', data);
      // Update progress for specific test
      setActiveExecutions(prev => {
        const updated = new Map(prev);
        const existing = updated.get(data.testId);
        if (existing) {
          updated.set(data.testId, {
            ...existing,
            progress: data.progress
          });
        }
        return updated;
      });
    };

    // Subscribe to SignalR events using the hook's on method
    on('WorkerTestStarted', handleTestStarted);
    on('WorkerTestProgress', handleTestProgress);
    on('WorkerTestCompleted', handleTestCompleted);
    on('WorkerTestStopped', handleTestStopped);
    on('IndicatorTestResult', handleIndicatorTestResult);

    // Subscribe to worker test updates
    invoke('SubscribeToWorkerTests').catch(console.error);

    return () => {
      off('WorkerTestStarted', handleTestStarted);
      off('WorkerTestProgress', handleTestProgress);
      off('WorkerTestCompleted', handleTestCompleted);
      off('WorkerTestStopped', handleTestStopped);
      off('IndicatorTestResult', handleIndicatorTestResult);
      invoke('UnsubscribeFromWorkerTests').catch(console.error);
    };
  }, [isConnected, loadData, on, off, invoke]);

  // Load data on component mount
  useEffect(() => {
    loadData();
  }, [loadData]);

  // Start a new test
  const handleStartTest = async () => {
    try {
      setIsStarting(true);
      
      const request: StartWorkerTestRequest = {
        testType: selectedTestType,
        indicatorIds: selectedIndicators.length > 0 ? selectedIndicators : undefined
      };

      const response = await workerIntegrationTestService.startTest(request);
      console.log('Test started:', response);
      
      // Add to active executions
      const newExecution: WorkerTestExecution = {
        id: response.testId,
        testType: selectedTestType,
        indicatorIds: selectedIndicators,
        startTime: response.startTime,
        isRunning: true,
        status: response.status,
        progress: 0
      };
      
      setActiveExecutions(prev => new Map(prev.set(response.testId, newExecution)));
      
      // Join the specific test group for real-time updates
      if (isConnected) {
        await invoke('JoinWorkerTestGroup', response.testId);
      }
      
    } catch (error) {
      console.error('Failed to start test:', error);
    } finally {
      setIsStarting(false);
    }
  };

  // Stop a running test
  const handleStopTest = async (testId: string) => {
    try {
      await workerIntegrationTestService.stopTest(testId);
      console.log('Test stopped:', testId);
    } catch (error) {
      console.error('Failed to stop test:', error);
    }
  };

  // View test results
  const handleViewResults = async (execution: WorkerTestExecution) => {
    try {
      const fullExecution = await workerIntegrationTestService.getTestExecution(execution.id);
      setSelectedExecution(fullExecution);
      setShowResults(true);
    } catch (error) {
      console.error('Failed to load test results:', error);
    }
  };

  const testTypes = workerIntegrationTestService.getAvailableTestTypes();

  if (loading) {
    return (
      <div className="worker-integration-test-page">
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div className="worker-integration-test-page">
      <div className="page-header">
        <Title level={2}>
          Worker Integration Testing
          <Tag color={isConnected ? 'green' : maxRetriesReached ? 'red' : 'orange'} style={{ marginLeft: 16 }}>
            {isConnected ? 'Connected' : maxRetriesReached ? 'Connection Failed' : 'Disconnected'}
          </Tag>
          {retryCount > 0 && !isConnected && !maxRetriesReached && (
            <Tag color="orange" style={{ marginLeft: 8 }}>
              Retry {retryCount}/3
            </Tag>
          )}
        </Title>
        <Text type="secondary">
          Test actual worker execution with real-time monitoring and validation
        </Text>
      </div>

      {/* SignalR Connection Error Alert */}
      {(signalRError || maxRetriesReached) && (
        <Alert
          type="error"
          showIcon
          closable
          style={{ marginBottom: 24 }}
          message="SignalR Connection Issue"
          description={
            <div>
              {maxRetriesReached && (
                <div>
                  Failed to connect to the real-time hub after 3 attempts.
                  Please ensure the API is running and accessible.
                </div>
              )}
              {signalRError && (
                <div>Error: {signalRError}</div>
              )}
              <div style={{ marginTop: 8 }}>
                <Button
                  size="small"
                  type="primary"
                  onClick={() => window.location.reload()}
                >
                  Reload Page
                </Button>
              </div>
            </div>
          }
        />
      )}

      {/* Status Overview */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col span={6}>
          <Card>
            <Statistic
              title="Active Tests"
              value={status?.activeTests || 0}
              valueStyle={{ color: status?.isRunning ? '#1890ff' : '#666' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Available Indicators"
              value={status?.totalIndicators || 0}
              valueStyle={{ color: '#52c41a' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="System Status"
              value={status?.isRunning ? 'Running' : 'Idle'}
              valueStyle={{ color: status?.isRunning ? '#1890ff' : '#666' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="SignalR Connection"
              value={
                isConnected ? 'Connected' :
                maxRetriesReached ? 'Failed' :
                retryCount > 0 ? `Retrying (${retryCount}/3)` :
                'Disconnected'
              }
              valueStyle={{
                color: isConnected ? '#52c41a' :
                       maxRetriesReached ? '#ff4d4f' :
                       retryCount > 0 ? '#fa8c16' : '#666'
              }}
            />
          </Card>
        </Col>
      </Row>

      {/* Test Controls */}
      <Card title="Start New Test" style={{ marginBottom: 24 }}>
        <Row gutter={[16, 16]} align="middle">
          <Col span={8}>
            <Select
              style={{ width: '100%' }}
              placeholder="Select test type"
              value={selectedTestType}
              onChange={setSelectedTestType}
            >
              {testTypes.map(type => (
                <Option key={type.value} value={type.value}>
                  {type.label}
                </Option>
              ))}
            </Select>
          </Col>
          <Col span={8}>
            <Select
              mode="multiple"
              style={{ width: '100%' }}
              placeholder="Select indicators (optional)"
              value={selectedIndicators}
              onChange={setSelectedIndicators}
            >
              {status?.availableIndicators && Array.isArray(status.availableIndicators) && status.availableIndicators.map(indicator => (
                <Option key={indicator.id} value={indicator.id}>
                  {indicator.name}
                </Option>
              ))}
            </Select>
          </Col>
          <Col span={8}>
            <Space>
              <Button
                type="primary"
                icon={<PlayCircleOutlined />}
                loading={isStarting}
                onClick={handleStartTest}
                disabled={!selectedTestType}
              >
                Start Test
              </Button>
              <Button
                icon={<ReloadOutlined />}
                onClick={loadData}
              >
                Refresh
              </Button>
            </Space>
          </Col>
        </Row>
        
        {selectedTestType && (
          <Alert
            style={{ marginTop: 16 }}
            message={testTypes.find(t => t.value === selectedTestType)?.description}
            type="info"
            showIcon
          />
        )}
      </Card>

      {/* Active Test Executions */}
      {activeExecutions.size > 0 && (
        <Card title="Active Test Executions" style={{ marginBottom: 24 }}>
          <Row gutter={[16, 16]}>
            {Array.from(activeExecutions.values()).map(execution => (
              <Col span={24} key={execution.id}>
                <WorkerTestExecutionCard
                  execution={execution}
                  onStop={() => handleStopTest(execution.id)}
                  onViewResults={() => handleViewResults(execution)}
                />
              </Col>
            ))}
          </Row>
        </Card>
      )}

      {/* Recent Test History */}
      {status?.recentExecutions && Array.isArray(status.recentExecutions) && status.recentExecutions.length > 0 && (
        <Card title="Recent Test History">
          <Row gutter={[16, 16]}>
            {status.recentExecutions
              .filter(execution => !execution.isRunning)
              .slice(0, 5)
              .map(execution => (
                <Col span={24} key={execution.id}>
                  <WorkerTestExecutionCard
                    execution={execution}
                    onViewResults={() => handleViewResults(execution)}
                    showActions={false}
                  />
                </Col>
              ))}
          </Row>
        </Card>
      )}

      {/* Test Results Modal */}
      <WorkerTestResultsModal
        execution={selectedExecution}
        visible={showResults}
        onClose={() => {
          setShowResults(false);
          setSelectedExecution(null);
        }}
      />
    </div>
  );
};

export default WorkerIntegrationTestPage;
