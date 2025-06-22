import React from 'react';
import { Modal, Tabs, Table, Tag, Typography, Row, Col, Statistic, Card, Progress, Space, Button } from 'antd';
import { DownloadOutlined, CheckCircleOutlined, CloseCircleOutlined, ExclamationCircleOutlined } from '@ant-design/icons';
import { WorkerTestExecution, IndicatorExecutionResult } from '../../services/workerIntegrationTestService';
import workerIntegrationTestService from '../../services/workerIntegrationTestService';

const { Title, Text } = Typography;
const { TabPane } = Tabs;

interface WorkerTestResultsModalProps {
  execution: WorkerTestExecution | null;
  visible: boolean;
  onClose: () => void;
}

const WorkerTestResultsModal: React.FC<WorkerTestResultsModalProps> = ({
  execution,
  visible,
  onClose
}) => {
  if (!execution) return null;

  const handleExport = () => {
    workerIntegrationTestService.exportTestResults(execution);
  };

  const getTestTypeLabel = () => {
    const testTypes = workerIntegrationTestService.getAvailableTestTypes();
    return testTypes.find(t => t.value === execution.testType)?.label || execution.testType;
  };

  const formatDuration = () => {
    return workerIntegrationTestService.formatDuration(execution.durationSeconds);
  };

  const getSuccessRate = () => {
    if (!execution.results) return 0;
    return workerIntegrationTestService.calculateSuccessRate(execution.results);
  };

  const getPerformanceRating = () => {
    if (!execution.results) return { rating: 'N/A', color: 'gray' };
    return workerIntegrationTestService.getPerformanceRating(execution.results.averageExecutionTimeMs);
  };

  const indicatorColumns = [
    {
      title: 'Status',
      dataIndex: 'success',
      key: 'success',
      width: 80,
      render: (success: boolean) => (
        <Tag color={success ? 'green' : 'red'} icon={success ? <CheckCircleOutlined /> : <CloseCircleOutlined />}>
          {success ? 'Success' : 'Failed'}
        </Tag>
      )
    },
    {
      title: 'Indicator',
      dataIndex: 'indicatorName',
      key: 'indicatorName',
      ellipsis: true
    },
    {
      title: 'Execution Time',
      dataIndex: 'executionTimeMs',
      key: 'executionTimeMs',
      width: 120,
      render: (time: number) => (
        <span className="execution-time">{time.toFixed(0)}ms</span>
      ),
      sorter: (a: IndicatorExecutionResult, b: IndicatorExecutionResult) => a.executionTimeMs - b.executionTimeMs
    },
    {
      title: 'Records',
      dataIndex: 'recordsProcessed',
      key: 'recordsProcessed',
      width: 100,
      render: (records: number) => records.toLocaleString()
    },
    {
      title: 'Alerts',
      dataIndex: 'alertsTriggered',
      key: 'alertsTriggered',
      width: 80,
      render: (alerts: boolean) => (
        alerts ? <Tag color="orange" icon={<ExclamationCircleOutlined />}>Yes</Tag> : <Tag>No</Tag>
      )
    },
    {
      title: 'Start Time',
      dataIndex: 'startTime',
      key: 'startTime',
      width: 150,
      render: (time: string) => new Date(time).toLocaleTimeString()
    }
  ];

  const renderOverviewTab = () => (
    <div>
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col span={6}>
          <Card>
            <Statistic
              title="Test Status"
              value={execution.success ? 'Success' : execution.isRunning ? 'Running' : 'Failed'}
              valueStyle={{ color: execution.success ? '#52c41a' : execution.isRunning ? '#1890ff' : '#ff4d4f' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Duration"
              value={formatDuration()}
              valueStyle={{ color: '#1890ff' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Success Rate"
              value={`${getSuccessRate().toFixed(1)}%`}
              valueStyle={{ color: getSuccessRate() > 80 ? '#52c41a' : getSuccessRate() > 50 ? '#faad14' : '#ff4d4f' }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Performance"
              value={getPerformanceRating().rating}
              valueStyle={{ color: getPerformanceRating().color }}
            />
          </Card>
        </Col>
      </Row>

      {execution.results && (
        <Row gutter={[16, 16]}>
          <Col span={12}>
            <Card title="Execution Summary" size="small">
              <Space direction="vertical" style={{ width: '100%' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text>Indicators Processed:</Text>
                  <Text strong>{execution.results.indicatorsProcessed}</Text>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text>Successful Executions:</Text>
                  <Text strong style={{ color: '#52c41a' }}>{execution.results.successfulExecutions}</Text>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text>Failed Executions:</Text>
                  <Text strong style={{ color: '#ff4d4f' }}>{execution.results.failedExecutions}</Text>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text>Alerts Triggered:</Text>
                  <Text strong style={{ color: '#faad14' }}>{execution.results.alertsTriggered}</Text>
                </div>
              </Space>
            </Card>
          </Col>
          <Col span={12}>
            <Card title="Performance Metrics" size="small">
              <Space direction="vertical" style={{ width: '100%' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text>Average Execution Time:</Text>
                  <Text strong>{execution.results.averageExecutionTimeMs.toFixed(0)}ms</Text>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text>Total Execution Time:</Text>
                  <Text strong>{execution.results.totalExecutionTimeMs.toFixed(0)}ms</Text>
                </div>
                {execution.results.memoryUsageBytes > 0 && (
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Text>Memory Usage:</Text>
                    <Text strong>{workerIntegrationTestService.formatMemoryUsage(execution.results.memoryUsageBytes)}</Text>
                  </div>
                )}
                {execution.results.cpuUsagePercent > 0 && (
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Text>CPU Usage:</Text>
                    <Text strong>{execution.results.cpuUsagePercent.toFixed(1)}%</Text>
                  </div>
                )}
              </Space>
            </Card>
          </Col>
        </Row>
      )}

      {execution.isRunning && (
        <Card title="Current Progress" style={{ marginTop: 16 }}>
          <Progress
            percent={execution.progress}
            status="active"
            strokeColor="#1890ff"
          />
          <Text type="secondary" style={{ marginTop: 8, display: 'block' }}>
            Status: {execution.status}
          </Text>
          {execution.lastUpdate && (
            <Text type="secondary" style={{ fontSize: '12px' }}>
              Last update: {new Date(execution.lastUpdate).toLocaleString()}
            </Text>
          )}
        </Card>
      )}

      {execution.errorMessage && (
        <Card title="Error Details" style={{ marginTop: 16 }}>
          <Text type="danger">{execution.errorMessage}</Text>
        </Card>
      )}
    </div>
  );

  const renderIndicatorResultsTab = () => (
    <div>
      {execution.results?.indicatorResults && execution.results.indicatorResults.length > 0 ? (
        <Table
          className="indicator-results-table"
          columns={indicatorColumns}
          dataSource={execution.results.indicatorResults}
          rowKey="indicatorId"
          size="small"
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total, range) => `${range[0]}-${range[1]} of ${total} indicators`
          }}
          rowClassName={(record: IndicatorExecutionResult) => record.success ? 'success' : 'failed'}
        />
      ) : (
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <Text type="secondary">No indicator results available</Text>
        </div>
      )}
    </div>
  );

  const renderRawDataTab = () => (
    <div>
      <pre style={{ 
        background: '#f5f5f5', 
        padding: '16px', 
        borderRadius: '4px', 
        fontSize: '12px',
        overflow: 'auto',
        maxHeight: '400px'
      }}>
        {JSON.stringify(execution, null, 2)}
      </pre>
    </div>
  );

  return (
    <Modal
      title={
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <Title level={4} style={{ margin: 0 }}>
              {getTestTypeLabel()} - Test Results
            </Title>
            <Text type="secondary" style={{ fontSize: '12px' }}>
              Test ID: {execution.id}
            </Text>
          </div>
          <Button
            type="primary"
            icon={<DownloadOutlined />}
            onClick={handleExport}
            size="small"
          >
            Export
          </Button>
        </div>
      }
      visible={visible}
      onCancel={onClose}
      width={1000}
      footer={null}
      className="test-results-modal"
    >
      <Tabs defaultActiveKey="overview" className="test-results-tabs">
        <TabPane tab="Overview" key="overview">
          {renderOverviewTab()}
        </TabPane>
        
        {execution.results?.indicatorResults && execution.results.indicatorResults.length > 0 && (
          <TabPane tab={`Indicator Results (${execution.results.indicatorResults.length})`} key="indicators">
            {renderIndicatorResultsTab()}
          </TabPane>
        )}
        
        <TabPane tab="Raw Data" key="raw">
          {renderRawDataTab()}
        </TabPane>
      </Tabs>
    </Modal>
  );
};

export default WorkerTestResultsModal;
