import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Typography,
  Card,
  CardContent,
  Switch,
  FormControlLabel,
  Alert,
  Chip,
  LinearProgress,
} from '@mui/material';
import {
  Speed as PerformanceIcon,
  Memory as MemoryIcon,
  Storage as BundleIcon,
  NetworkCheck as NetworkIcon,
  Warning as WarningIcon,
  CheckCircle as SuccessIcon,
} from '@mui/icons-material';
import { advancedPerformanceMonitor } from '@/utils/advancedPerformance';
import { monitorBundleSize } from '@/utils/bundleOptimization';
import { MetricCard } from '@/components/UI';

interface PerformanceMetrics {
  lcp?: number;
  fid?: number;
  cls?: number;
  memoryUsage?: number;
  bundleSize?: number;
  renderTime?: number;
}

/**
 * Performance monitoring dashboard for administrators
 */
export const PerformanceDashboard: React.FC = () => {
  const [metrics, setMetrics] = useState<PerformanceMetrics>({});
  const [monitoring, setMonitoring] = useState(false);
  const [bundleInfo, setBundleInfo] = useState<any>(null);

  useEffect(() => {
    // Get initial bundle size
    const info = monitorBundleSize();
    setBundleInfo(info);

    // Get performance metrics if available
    if (advancedPerformanceMonitor) {
      const currentMetrics = advancedPerformanceMonitor.getMetrics();
      setMetrics(currentMetrics);
    }
  }, []);

  const handleMonitoringToggle = (enabled: boolean) => {
    setMonitoring(enabled);
    
    if (enabled && advancedPerformanceMonitor) {
      advancedPerformanceMonitor.startMonitoring();
      
      // Update metrics periodically
      const interval = setInterval(() => {
        const currentMetrics = advancedPerformanceMonitor.getMetrics();
        setMetrics(currentMetrics);
      }, 5000);
      
      return () => clearInterval(interval);
    } else if (advancedPerformanceMonitor) {
      advancedPerformanceMonitor.stopMonitoring();
    }
  };

  const getPerformanceStatus = (metric: number | undefined, threshold: number) => {
    if (!metric) return 'unknown';
    return metric <= threshold ? 'good' : metric <= threshold * 1.5 ? 'needs-improvement' : 'poor';
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'good':
        return 'success';
      case 'needs-improvement':
        return 'warning';
      case 'poor':
        return 'error';
      default:
        return 'default';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'good':
        return <SuccessIcon color="success" />;
      case 'needs-improvement':
      case 'poor':
        return <WarningIcon color="warning" />;
      default:
        return <PerformanceIcon />;
    }
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <PerformanceIcon />
          Performance Dashboard
        </Typography>
        
        <FormControlLabel
          control={
            <Switch
              checked={monitoring}
              onChange={(e) => handleMonitoringToggle(e.target.checked)}
            />
          }
          label="Real-time Monitoring"
        />
      </Box>

      {!monitoring && (
        <Alert severity="info" sx={{ mb: 3 }}>
          Enable real-time monitoring to see live performance metrics
        </Alert>
      )}

      <Grid container spacing={3}>
        {/* Core Web Vitals */}
        <Grid item xs={12} md={4}>
          <MetricCard
            title="Largest Contentful Paint"
            value={metrics.lcp ? `${metrics.lcp.toFixed(0)}ms` : 'N/A'}
            icon={<PerformanceIcon />}
            trend={getPerformanceStatus(metrics.lcp, 2500)}
            subtitle="Loading Performance"
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <MetricCard
            title="First Input Delay"
            value={metrics.fid ? `${metrics.fid.toFixed(0)}ms` : 'N/A'}
            icon={<NetworkIcon />}
            trend={getPerformanceStatus(metrics.fid, 100)}
            subtitle="Interactivity"
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <MetricCard
            title="Cumulative Layout Shift"
            value={metrics.cls ? metrics.cls.toFixed(3) : 'N/A'}
            icon={<PerformanceIcon />}
            trend={getPerformanceStatus(metrics.cls, 0.1)}
            subtitle="Visual Stability"
          />
        </Grid>

        {/* Memory and Bundle */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <MemoryIcon />
                Memory Usage
              </Typography>
              
              {metrics.memoryUsage ? (
                <Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Used Memory</Typography>
                    <Typography variant="body2">{metrics.memoryUsage.toFixed(1)} MB</Typography>
                  </Box>
                  <LinearProgress 
                    variant="determinate" 
                    value={Math.min((metrics.memoryUsage / 100) * 100, 100)} 
                    sx={{ height: 8, borderRadius: 4 }}
                  />
                  <Box sx={{ mt: 1 }}>
                    <Chip 
                      size="small" 
                      label={metrics.memoryUsage < 50 ? 'Good' : metrics.memoryUsage < 100 ? 'Fair' : 'High'}
                      color={getStatusColor(metrics.memoryUsage < 50 ? 'good' : metrics.memoryUsage < 100 ? 'needs-improvement' : 'poor')}
                    />
                  </Box>
                </Box>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Memory monitoring not available
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <BundleIcon />
                Bundle Analysis
              </Typography>
              
              {bundleInfo ? (
                <Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">JavaScript</Typography>
                    <Typography variant="body2">{bundleInfo.totalJS} KB</Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">CSS</Typography>
                    <Typography variant="body2">{bundleInfo.totalCSS} KB</Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                    <Typography variant="body2">Images</Typography>
                    <Typography variant="body2">{bundleInfo.totalImages} KB</Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="subtitle2">Total Bundle Size</Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="subtitle2">{bundleInfo.total} KB</Typography>
                      <Chip 
                        size="small" 
                        label={bundleInfo.total < 500 ? 'Excellent' : bundleInfo.total < 1000 ? 'Good' : 'Large'}
                        color={getStatusColor(bundleInfo.total < 500 ? 'good' : bundleInfo.total < 1000 ? 'needs-improvement' : 'poor')}
                      />
                    </Box>
                  </Box>
                </Box>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Bundle analysis not available
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Performance Recommendations */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Performance Recommendations
              </Typography>
              
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                {metrics.lcp && metrics.lcp > 2500 && (
                  <Alert severity="warning" icon={<WarningIcon />}>
                    LCP is above 2.5s - Consider optimizing images and critical resources
                  </Alert>
                )}
                
                {metrics.fid && metrics.fid > 100 && (
                  <Alert severity="warning" icon={<WarningIcon />}>
                    FID is above 100ms - Consider reducing JavaScript execution time
                  </Alert>
                )}
                
                {metrics.cls && metrics.cls > 0.1 && (
                  <Alert severity="warning" icon={<WarningIcon />}>
                    CLS is above 0.1 - Set explicit dimensions for images and dynamic content
                  </Alert>
                )}
                
                {bundleInfo && bundleInfo.total > 1000 && (
                  <Alert severity="info" icon={<BundleIcon />}>
                    Bundle size is large - Consider code splitting and lazy loading
                  </Alert>
                )}
                
                {(!metrics.lcp || metrics.lcp <= 2500) && 
                 (!metrics.fid || metrics.fid <= 100) && 
                 (!metrics.cls || metrics.cls <= 0.1) && (
                  <Alert severity="success" icon={<SuccessIcon />}>
                    All performance metrics are within recommended thresholds!
                  </Alert>
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default PerformanceDashboard;
