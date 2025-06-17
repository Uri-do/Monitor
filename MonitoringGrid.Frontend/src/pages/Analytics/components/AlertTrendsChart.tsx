import React from 'react';
import { Grid, CardContent, Typography, Box } from '@mui/material';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { Card } from '@/components/UI';
import { AlertStatisticsDto } from '@/types/api';

interface AlertTrendsChartProps {
  alertStats?: AlertStatisticsDto;
}

export const AlertTrendsChart: React.FC<AlertTrendsChartProps> = ({
  alertStats,
}) => {
  return (
    <Grid item xs={12} lg={8}>
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Alert Trends Over Time
          </Typography>
          <ResponsiveContainer width="100%" height={300}>
            {alertStats?.trendData && alertStats.trendData.length > 0 ? (
              <AreaChart data={alertStats.trendData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Area
                  type="monotone"
                  dataKey="alerts"
                  stackId="1"
                  stroke="#f44336"
                  fill="#f44336"
                  fillOpacity={0.6}
                  name="Alerts"
                />
                <Area
                  type="monotone"
                  dataKey="kpiExecutions"
                  stackId="2"
                  stroke="#2196f3"
                  fill="#2196f3"
                  fillOpacity={0.6}
                  name="KPI Executions"
                />
              </AreaChart>
            ) : (
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  height: '100%',
                }}
              >
                <Typography variant="body2" color="text.secondary">
                  No trend data available
                </Typography>
              </Box>
            )}
          </ResponsiveContainer>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default AlertTrendsChart;
