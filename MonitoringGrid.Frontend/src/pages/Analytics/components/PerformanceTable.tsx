import React from 'react';
import { Grid, CardContent, Typography, Box } from '@mui/material';
import { Card, StatusChip } from '@/components/UI';

interface PerformanceTableProps {
  // Currently no data available - placeholder for future implementation
}

export const PerformanceTable: React.FC<PerformanceTableProps> = () => {
  return (
    <Grid item xs={12}>
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            KPI Performance Summary
          </Typography>
          <Box sx={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid #e0e0e0' }}>
                  <th style={{ textAlign: 'left', padding: '12px' }}>KPI Name</th>
                  <th style={{ textAlign: 'center', padding: '12px' }}>Executions</th>
                  <th style={{ textAlign: 'center', padding: '12px' }}>Alerts</th>
                  <th style={{ textAlign: 'center', padding: '12px' }}>Success Rate</th>
                  <th style={{ textAlign: 'center', padding: '12px' }}>Status</th>
                </tr>
              </thead>
              <tbody>
                {/* Recent executions not available in IndicatorDashboardDto - showing placeholder */}
                {false ? (
                  [].map((execution: any, index: number) => (
                    <tr key={index} style={{ borderBottom: '1px solid #f0f0f0' }}>
                      <td style={{ padding: '12px', fontWeight: 'medium' }}>
                        {execution.indicator}
                      </td>
                      <td style={{ textAlign: 'center', padding: '12px' }}>1</td>
                      <td style={{ textAlign: 'center', padding: '12px' }}>0</td>
                      <td style={{ textAlign: 'center', padding: '12px' }}>
                        {execution.isSuccessful ? '100' : '0'}%
                      </td>
                      <td style={{ textAlign: 'center', padding: '12px' }}>
                        <StatusChip status={execution.isSuccessful ? 'success' : 'error'} />
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={5} style={{ textAlign: 'center', padding: '24px' }}>
                      <Typography variant="body2" color="text.secondary">
                        No KPI execution data available
                      </Typography>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </Box>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default PerformanceTable;
