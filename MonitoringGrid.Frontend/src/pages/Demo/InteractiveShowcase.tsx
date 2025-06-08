import React, { useState } from 'react';
import {
  Box,
  Container,
  Typography,
  Grid,
  Paper,
  Tabs,
  Tab,
  Card,
  CardContent,
  Button,
  useTheme,
  alpha,
} from '@mui/material';
import {
  TouchApp,
  Gesture,
  Mouse,
  Dashboard,
  Timeline,
  FilterList,
  PlayArrow,
} from '@mui/icons-material';
import PageHeader from '../../components/Common/PageHeader';
import {
  AnimatedContainer,
  RippleEffect,
  MagneticHover,
  ParallaxScroll,
  MorphingButton,
} from '../../components/animations/MicroAnimations';
import DragDropDashboard from '../../components/interactions/DragDropDashboard';
import {
  SwipeableCard,
  PinchZoom,
  LongPress,
  PullToRefresh,
} from '../../components/interactions/GestureSupport';
import {
  InteractiveChart,
  DataFilterPanel,
  TimeSeriesPlayer,
} from '../../components/interactions/DataExploration';
import { HoverEffectsShowcase } from '../../components/interactions/HoverEffects';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div hidden={value !== index}>{value === index && <Box sx={{ py: 3 }}>{children}</Box>}</div>
);

const InteractiveShowcase: React.FC = () => {
  const theme = useTheme();
  const [tabValue, setTabValue] = useState(0);
  const [refreshing, setRefreshing] = useState(false);

  // Sample data for demonstrations
  const sampleChartData = Array.from({ length: 50 }, (_, i) => ({
    name: `Point ${i + 1}`,
    value: Math.floor(Math.random() * 1000) + 100,
    timestamp: Date.now() + i * 60000,
  }));

  const sampleTimeSeriesData = Array.from({ length: 100 }, (_, i) => ({
    timestamp: Date.now() + i * 60000,
    value: Math.sin(i * 0.1) * 100 + 200 + Math.random() * 50,
    category: ['API', 'Database', 'Cache'][i % 3],
  }));

  const sampleFilters = [
    {
      id: 'range',
      label: 'Value Range',
      type: 'range' as const,
      value: [0, 1000],
      min: 0,
      max: 1000,
    },
    {
      id: 'categories',
      label: 'Categories',
      type: 'multiselect' as const,
      options: ['API', 'Database', 'Cache', 'External'],
      value: ['API', 'Database'],
    },
  ];

  const handleRefresh = async () => {
    setRefreshing(true);
    await new Promise(resolve => setTimeout(resolve, 2000));
    setRefreshing(false);
  };

  const handleFilterChange = (filterId: string, value: any) => {
    console.log('Filter changed:', filterId, value);
  };

  const handleResetFilters = () => {
    console.log('Filters reset');
  };

  return (
    <Container maxWidth="xl" sx={{ py: 4 }}>
      <PageHeader
        title="Advanced Interactions & Micro-animations"
        subtitle="Sophisticated user interactions with gesture support and delightful animations"
        icon={<TouchApp />}
      />

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={(_, newValue) => setTabValue(newValue)}>
          <Tab icon={<Mouse />} label="Hover Effects" />
          <Tab icon={<Gesture />} label="Gesture Support" />
          <Tab icon={<Dashboard />} label="Drag & Drop" />
          <Tab icon={<Timeline />} label="Data Exploration" />
          <Tab icon={<TouchApp />} label="Micro-animations" />
        </Tabs>
      </Box>

      <TabPanel value={tabValue} index={0}>
        <HoverEffectsShowcase />
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Touch Gestures & Mobile Interactions
        </Typography>

        <Grid container spacing={3}>
          {/* Swipeable Cards */}
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Swipeable Cards
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              {[1, 2, 3].map(item => (
                <SwipeableCard
                  key={item}
                  onSwipeLeft={() => console.log(`Delete item ${item}`)}
                  onSwipeRight={() => console.log(`Archive item ${item}`)}
                >
                  <Card>
                    <CardContent>
                      <Typography variant="h6">Swipeable Item {item}</Typography>
                      <Typography variant="body2" color="text.secondary">
                        Swipe left to delete, right to archive
                      </Typography>
                    </CardContent>
                  </Card>
                </SwipeableCard>
              ))}
            </Box>
          </Grid>

          {/* Pinch to Zoom */}
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Pinch to Zoom
            </Typography>
            <PinchZoom onScaleChange={scale => console.log('Scale:', scale)}>
              <Paper sx={{ p: 4, textAlign: 'center', minHeight: 200 }}>
                <Typography variant="h4" gutterBottom>
                  Zoomable Content
                </Typography>
                <Typography variant="body1">
                  Use pinch gesture on mobile or scroll wheel on desktop to zoom
                </Typography>
                <Box sx={{ mt: 2, display: 'flex', justifyContent: 'center', gap: 2 }}>
                  <Box sx={{ width: 50, height: 50, bgcolor: 'primary.main', borderRadius: 1 }} />
                  <Box sx={{ width: 50, height: 50, bgcolor: 'secondary.main', borderRadius: 1 }} />
                  <Box sx={{ width: 50, height: 50, bgcolor: 'success.main', borderRadius: 1 }} />
                </Box>
              </Paper>
            </PinchZoom>
          </Grid>

          {/* Long Press */}
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Long Press Interactions
            </Typography>
            <LongPress onLongPress={() => alert('Long press detected!')}>
              <Card sx={{ cursor: 'pointer' }}>
                <CardContent>
                  <Typography variant="h6">Long Press Me</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Hold for 500ms to trigger action
                  </Typography>
                </CardContent>
              </Card>
            </LongPress>
          </Grid>

          {/* Pull to Refresh */}
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Pull to Refresh
            </Typography>
            <Paper sx={{ height: 300, overflow: 'hidden' }}>
              <PullToRefresh onRefresh={handleRefresh}>
                <Box sx={{ p: 2, height: 400 }}>
                  <Typography variant="body1" gutterBottom>
                    Pull down to refresh content
                  </Typography>
                  {refreshing ? (
                    <Typography variant="body2" color="primary">
                      Refreshing...
                    </Typography>
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      Content loaded at {new Date().toLocaleTimeString()}
                    </Typography>
                  )}
                  <Box sx={{ mt: 2 }}>
                    {Array.from({ length: 10 }, (_, i) => (
                      <Typography key={i} variant="body2" sx={{ mb: 1 }}>
                        Sample content item {i + 1}
                      </Typography>
                    ))}
                  </Box>
                </Box>
              </PullToRefresh>
            </Paper>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Drag & Drop Dashboard Builder
        </Typography>
        <DragDropDashboard
          editable
          onWidgetsChange={widgets => console.log('Widgets updated:', widgets)}
        />
      </TabPanel>

      <TabPanel value={tabValue} index={3}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Interactive Data Exploration
        </Typography>

        <Grid container spacing={3}>
          {/* Interactive Chart */}
          <Grid item xs={12}>
            <Paper sx={{ p: 3, borderRadius: '16px' }}>
              <Typography variant="h6" gutterBottom>
                Interactive Chart with Zoom & Brush
              </Typography>
              <InteractiveChart
                data={sampleChartData}
                onDataPointSelect={point => console.log('Selected point:', point)}
                onRangeSelect={range => console.log('Selected range:', range)}
                height={400}
              />
            </Paper>
          </Grid>

          {/* Data Filters */}
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 3, borderRadius: '16px' }}>
              <Typography variant="h6" gutterBottom>
                Advanced Data Filters
              </Typography>
              <DataFilterPanel
                filters={sampleFilters}
                onFilterChange={handleFilterChange}
                onResetFilters={handleResetFilters}
              />
            </Paper>
          </Grid>

          {/* Time Series Player */}
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 3, borderRadius: '16px' }}>
              <Typography variant="h6" gutterBottom>
                Time Series Player
              </Typography>
              <TimeSeriesPlayer
                data={sampleTimeSeriesData}
                onTimeChange={timestamp => console.log('Time changed:', new Date(timestamp))}
                autoPlay={false}
                playbackSpeed={1000}
              />
            </Paper>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={4}>
        <Typography variant="h5" gutterBottom fontWeight={600} sx={{ mb: 3 }}>
          Micro-animations & Advanced Effects
        </Typography>

        <Grid container spacing={3}>
          {/* Animated Containers */}
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Scroll-triggered Animations
            </Typography>
            <Box
              sx={{
                height: 400,
                overflow: 'auto',
                border: 1,
                borderColor: 'divider',
                borderRadius: 2,
              }}
            >
              {Array.from({ length: 10 }, (_, i) => (
                <AnimatedContainer key={i} animation="fadeInUp" delay={i * 0.1} stagger>
                  <Paper sx={{ m: 2, p: 2 }}>
                    <Typography variant="h6">Animated Item {i + 1}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      This item animates when it comes into view
                    </Typography>
                  </Paper>
                </AnimatedContainer>
              ))}
            </Box>
          </Grid>

          {/* Ripple Effects */}
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Ripple Effects
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <RippleEffect>
                <Paper sx={{ p: 3, cursor: 'pointer' }}>
                  <Typography variant="h6">Click for Ripple</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Material Design ripple effect
                  </Typography>
                </Paper>
              </RippleEffect>
            </Box>
          </Grid>

          {/* Magnetic Hover */}
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Magnetic Hover Effect
            </Typography>
            <MagneticHover strength={0.3} distance={100}>
              <Paper sx={{ p: 3, textAlign: 'center' }}>
                <Typography variant="h6">Magnetic Element</Typography>
                <Typography variant="body2" color="text.secondary">
                  Follows your cursor with magnetic attraction
                </Typography>
              </Paper>
            </MagneticHover>
          </Grid>

          {/* Morphing Button */}
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Morphing Interactions
            </Typography>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <MorphingButton
                morphTo={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <PlayArrow />
                    <Typography>Play</Typography>
                  </Box>
                }
                trigger="hover"
              >
                <Button variant="contained">Hover to Morph</Button>
              </MorphingButton>
            </Box>
          </Grid>

          {/* Parallax Scroll */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom>
              Parallax Scroll Effect
            </Typography>
            <Box
              sx={{
                height: 300,
                overflow: 'auto',
                border: 1,
                borderColor: 'divider',
                borderRadius: 2,
              }}
            >
              <Box sx={{ height: 800, position: 'relative' }}>
                <ParallaxScroll speed={0.5}>
                  <Paper
                    sx={{
                      position: 'absolute',
                      top: 100,
                      left: '50%',
                      transform: 'translateX(-50%)',
                      p: 3,
                      backgroundColor: alpha(theme.palette.primary.main, 0.1),
                    }}
                  >
                    <Typography variant="h6">Parallax Element</Typography>
                    <Typography variant="body2">Scrolls at different speed than content</Typography>
                  </Paper>
                </ParallaxScroll>
                <Box sx={{ p: 3 }}>
                  {Array.from({ length: 20 }, (_, i) => (
                    <Typography key={i} variant="body1" sx={{ mb: 2 }}>
                      Scroll content line {i + 1} - Notice how the parallax element moves
                      differently
                    </Typography>
                  ))}
                </Box>
              </Box>
            </Box>
          </Grid>
        </Grid>
      </TabPanel>
    </Container>
  );
};

export default InteractiveShowcase;
