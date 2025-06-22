import React from 'react';
import { Card, Progress, Button, Tag, Space, Typography, Row, Col, Tooltip } from 'antd';
import { StopOutlined, EyeOutlined, ExportOutlined, ClockCircleOutlined } from '@ant-design/icons';
import { WorkerTestExecution } from '../../services/workerIntegrationTestService';
import workerIntegrationTestService from '../../services/workerIntegrationTestService';

const { Text, Title } = Typography;

interface WorkerTestExecutionCardProps {
  execution: WorkerTestExecution;
  onStop?: () => void;
  onViewResults?: () => void;
  showActions?: boolean;
}

const WorkerTestExecutionCard: React.FC<WorkerTestExecutionCardProps> = ({
  execution,
  onStop,
  onViewResults,
  showActions = true
}) => {
  // Safety check - if execution is not properly defined, return null
  if (!execution || !execution.id) {
    return null;
  }
  const getStatusColor = () => {
    return workerIntegrationTestService.getStatusColor(execution);
  };

  const getStatusIcon = () => {
    return workerIntegrationTestService.getStatusIcon(execution);
  };

  const formatDuration = () => {
    return workerIntegrationTestService.formatDuration(execution.durationSeconds);
  };

  const getTestTypeLabel = () => {
    const testTypes = workerIntegrationTestService.getAvailableTestTypes();
    return testTypes.find(t => t.value === execution.testType)?.label || execution.testType;
  };

  const getCardClassName = () => {
    let className = 'worker-test-execution-card';
    if (execution.isRunning) className += ' running';
    else if (execution.success === true) className += ' success';
    else if (execution.success === false) className += ' failed';
    else className += ' cancelled';
    return className;
  };

  const handleExport = () => {
    workerIntegrationTestService.exportTestResults(execution);
  };

  const renderProgressBar = () => {
    let status: 'normal' | 'success' | 'exception' = 'normal';
    if (!execution.isRunning) {
      status = execution.success ? 'success' : 'exception';
    }

    return (
      <div className="progress-container">
        <Progress
          percent={execution.progress}
          status={status}
          strokeColor={execution.isRunning ? '#1890ff' : undefined}
          showInfo={true}
        />
        <div className="progress-status">
          <Text type="secondary">{execution.status}</Text>
          {execution.lastUpdate && (
            <Text type="secondary">
              Last update: {new Date(execution.lastUpdate).toLocaleTimeString()}
            </Text>
          )}
        </div>
      </div>
    );
  };

  const renderMetrics = () => {
    if (!execution.results) return null;

    const { results } = execution;
    const successRate = workerIntegrationTestService.calculateSuccessRate(results);
    const performanceRating = workerIntegrationTestService.getPerformanceRating(results.averageExecutionTimeMs);

    return (
      <div className="performance-metrics">
        <div className="metric-item">
          <div className="metric-value" style={{ color: '#52c41a' }}>
            {results.successfulExecutions}
          </div>
          <div className="metric-label">Successful</div>
        </div>
        <div className="metric-item">
          <div className="metric-value" style={{ color: '#ff4d4f' }}>
            {results.failedExecutions}
          </div>
          <div className="metric-label">Failed</div>
        </div>
        <div className="metric-item">
          <div className="metric-value" style={{ color: '#1890ff' }}>
            {successRate.toFixed(1)}%
          </div>
          <div className="metric-label">Success Rate</div>
        </div>
        <div className="metric-item">
          <div className="metric-value" style={{ color: performanceRating.color }}>
            {results.averageExecutionTimeMs.toFixed(0)}ms
          </div>
          <div className="metric-label">Avg Time</div>
        </div>
        {results.alertsTriggered > 0 && (
          <div className="metric-item">
            <div className="metric-value" style={{ color: '#faad14' }}>
              {results.alertsTriggered}
            </div>
            <div className="metric-label">Alerts</div>
          </div>
        )}
        {results.memoryUsageBytes > 0 && (
          <div className="metric-item">
            <div className="metric-value" style={{ color: '#722ed1' }}>
              {workerIntegrationTestService.formatMemoryUsage(results.memoryUsageBytes)}
            </div>
            <div className="metric-label">Memory</div>
          </div>
        )}
      </div>
    );
  };

  return (
    <div className={getCardClassName()}>
      <div className="test-execution-header">
        <div className="test-execution-title">
          <span style={{ fontSize: '18px' }}>{getStatusIcon()}</span>
          <div>
            <Title level={5} style={{ margin: 0 }}>
              {getTestTypeLabel()}
            </Title>
            <Text type="secondary" style={{ fontSize: '12px' }}>
              ID: {execution.id ? execution.id.substring(0, 8) + '...' : 'N/A'}
            </Text>
          </div>
          <Tag color={getStatusColor()}>
            {execution.isRunning ? 'Running' : execution.success ? 'Completed' : 'Failed'}
          </Tag>
        </div>
        
        {showActions && (
          <div className="test-execution-actions">
            <Space>
              {execution.isRunning && onStop && (
                <Button
                  type="primary"
                  danger
                  size="small"
                  icon={<StopOutlined />}
                  onClick={onStop}
                >
                  Stop
                </Button>
              )}
              {onViewResults && (
                <Button
                  size="small"
                  icon={<EyeOutlined />}
                  onClick={onViewResults}
                >
                  View Results
                </Button>
              )}
              {!execution.isRunning && (
                <Tooltip title="Export test results">
                  <Button
                    size="small"
                    icon={<ExportOutlined />}
                    onClick={handleExport}
                  />
                </Tooltip>
              )}
            </Space>
          </div>
        )}
      </div>

      <div className="test-execution-content">
        <div>
          <Text strong>Start Time</Text>
          <br />
          <Text type="secondary">
            <ClockCircleOutlined style={{ marginRight: 4 }} />
            {execution.startTime ? new Date(execution.startTime).toLocaleString() : 'N/A'}
          </Text>
        </div>
        
        <div>
          <Text strong>Duration</Text>
          <br />
          <Text type="secondary">{formatDuration()}</Text>
        </div>
        
        <div>
          <Text strong>Indicators</Text>
          <br />
          <Text type="secondary">
            {execution.indicatorIds && execution.indicatorIds.length > 0
              ? `${execution.indicatorIds.length} selected`
              : 'All available'
            }
          </Text>
        </div>
      </div>

      {execution.isRunning && renderProgressBar()}

      {execution.errorMessage && (
        <div style={{ marginTop: 16 }}>
          <Text type="danger" style={{ fontSize: '12px' }}>
            Error: {execution.errorMessage}
          </Text>
        </div>
      )}

      {renderMetrics()}
    </div>
  );
};

export default WorkerTestExecutionCard;
