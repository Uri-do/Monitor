import React from 'react';
import { Grid, CardContent, Typography, Box } from '@mui/material';
import {
  PieChart as RechartsPieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { Card } from '@/components/UI';
import { AlertStatisticsDto } from '@/types/api';

interface SeverityDistributionChartProps {
  alertStats?: AlertStatisticsDto;
}

export const SeverityDistributionChart: React.FC<SeverityDistributionChartProps> = ({
  alertStats,
}) => {
  return (
    <Grid item xs={12} lg={4}>
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Alert Severity Distribution
          </Typography>
          <ResponsiveContainer width="100%" height={300}>
            {alertStats?.severityDistribution && alertStats.severityDistribution.length > 0 ? (
              <RechartsPieChart>
                <Pie
                  data={alertStats.severityDistribution}
                  cx="50%"
                  cy="50%"
                  outerRadius={80}
                  dataKey="value"
                  label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                >
                  {alertStats.severityDistribution.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip />
              </RechartsPieChart>
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
                  No severity data available
                </Typography>
              </Box>
            )}
          </ResponsiveContainer>
        </CardContent>
      </Card>
    </Grid>
  );
};

export default SeverityDistributionChart;
