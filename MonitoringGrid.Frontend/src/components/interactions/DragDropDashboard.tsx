import React, { useState, useRef, useCallback } from 'react';
import {
  Box,
  Paper,
  Typography,
  IconButton,
  Menu,
  MenuItem,
  Fab,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Grid,
  Card,
  CardContent,
  useTheme,
  alpha,
  styled,
  keyframes,
} from '@mui/material';
import {
  DragIndicator,
  Add,
  MoreVert,
  Delete,
  Edit,
  Fullscreen,
  Dashboard,
  BarChart,
  PieChart,
  Timeline,
  Speed,
} from '@mui/icons-material';

// Drag animations
const dragStart = keyframes`
  from { transform: scale(1) rotate(0deg); }
  to { transform: scale(1.05) rotate(2deg); }
`;

const dropZoneActive = keyframes`
  0%, 100% { border-color: #2196f3; }
  50% { border-color: #64b5f6; }
`;

const slideIn = keyframes`
  from { transform: translateY(20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
`;

// Styled components
const DragHandle = styled(Box)(({ theme }) => ({
  position: 'absolute',
  top: 8,
  right: 8,
  cursor: 'grab',
  opacity: 0,
  transition: 'opacity 0.2s ease',
  '&:active': {
    cursor: 'grabbing',
  },
}));

const DraggableWidget = styled(Paper)<{ isDragging: boolean }>(({ theme, isDragging }) => ({
  position: 'relative',
  minHeight: 200,
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  cursor: 'pointer',
  animation: isDragging ? `${dragStart} 0.2s ease-out` : 'none',
  transform: isDragging ? 'scale(1.05) rotate(2deg)' : 'scale(1)',
  zIndex: isDragging ? 1000 : 1,
  boxShadow: isDragging ? theme.shadows[12] : theme.shadows[2],
  '&:hover': {
    transform: isDragging ? 'scale(1.05) rotate(2deg)' : 'translateY(-4px)',
    boxShadow: isDragging ? theme.shadows[12] : theme.shadows[8],
    [`& ${DragHandle}`]: {
      opacity: 1,
    },
  },
}));

const DropZone = styled(Box)<{ isActive: boolean }>(({ theme, isActive }) => ({
  minHeight: 200,
  border: `2px dashed ${isActive ? theme.palette.primary.main : theme.palette.divider}`,
  borderRadius: theme.shape.borderRadius * 2,
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  backgroundColor: isActive 
    ? alpha(theme.palette.primary.main, 0.05) 
    : alpha(theme.palette.background.paper, 0.5),
  transition: 'all 0.3s ease',
  animation: isActive ? `${dropZoneActive} 1s ease-in-out infinite` : 'none',
  '&:hover': {
    backgroundColor: alpha(theme.palette.primary.main, 0.08),
    borderColor: theme.palette.primary.main,
  },
}));

const WidgetLibrary = styled(Paper)(({ theme }) => ({
  position: 'fixed',
  right: 24,
  top: '50%',
  transform: 'translateY(-50%)',
  width: 280,
  maxHeight: '80vh',
  overflowY: 'auto',
  zIndex: 1200,
  animation: `${slideIn} 0.3s ease-out`,
}));

// Widget types
interface Widget {
  id: string;
  type: 'chart' | 'kpi' | 'table' | 'metric';
  title: string;
  config: any;
  position: { x: number; y: number };
  size: { width: number; height: number };
}

interface WidgetTemplate {
  id: string;
  type: Widget['type'];
  title: string;
  icon: React.ReactNode;
  description: string;
  defaultConfig: any;
}

const widgetTemplates: WidgetTemplate[] = [
  {
    id: 'line-chart',
    type: 'chart',
    title: 'Line Chart',
    icon: <Timeline />,
    description: 'Time series data visualization',
    defaultConfig: { chartType: 'line', dataSource: 'api' },
  },
  {
    id: 'bar-chart',
    type: 'chart',
    title: 'Bar Chart',
    icon: <BarChart />,
    description: 'Categorical data comparison',
    defaultConfig: { chartType: 'bar', dataSource: 'api' },
  },
  {
    id: 'pie-chart',
    type: 'chart',
    title: 'Pie Chart',
    icon: <PieChart />,
    description: 'Proportional data display',
    defaultConfig: { chartType: 'pie', dataSource: 'api' },
  },
  {
    id: 'kpi-metric',
    type: 'kpi',
    title: 'KPI Metric',
    icon: <Speed />,
    description: 'Key performance indicator',
    defaultConfig: { metric: 'response_time', target: 200 },
  },
];

interface DragDropDashboardProps {
  initialWidgets?: Widget[];
  onWidgetsChange?: (widgets: Widget[]) => void;
  editable?: boolean;
}

export const DragDropDashboard: React.FC<DragDropDashboardProps> = ({
  initialWidgets = [],
  onWidgetsChange,
  editable = true,
}) => {
  const theme = useTheme();
  const [widgets, setWidgets] = useState<Widget[]>(initialWidgets);
  const [draggedWidget, setDraggedWidget] = useState<Widget | null>(null);
  const [draggedTemplate, setDraggedTemplate] = useState<WidgetTemplate | null>(null);
  const [dropZoneActive, setDropZoneActive] = useState<string | null>(null);
  const [showLibrary, setShowLibrary] = useState(false);
  const [selectedWidget, setSelectedWidget] = useState<Widget | null>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [configDialog, setConfigDialog] = useState(false);

  const dragRef = useRef<{ startX: number; startY: number; widget: Widget | null }>({
    startX: 0,
    startY: 0,
    widget: null,
  });

  // Handle widget drag start
  const handleDragStart = useCallback((widget: Widget, event: React.MouseEvent) => {
    if (!editable) return;
    
    event.preventDefault();
    setDraggedWidget(widget);
    dragRef.current = {
      startX: event.clientX,
      startY: event.clientY,
      widget,
    };

    const handleMouseMove = (e: MouseEvent) => {
      // Update widget position during drag
      const deltaX = e.clientX - dragRef.current.startX;
      const deltaY = e.clientY - dragRef.current.startY;
      
      // Visual feedback could be added here
    };

    const handleMouseUp = () => {
      setDraggedWidget(null);
      setDropZoneActive(null);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }, [editable]);

  // Handle template drag from library
  const handleTemplateDragStart = useCallback((template: WidgetTemplate, event: React.MouseEvent) => {
    event.preventDefault();
    setDraggedTemplate(template);

    const handleMouseMove = (e: MouseEvent) => {
      // Visual feedback for template dragging
    };

    const handleMouseUp = (e: MouseEvent) => {
      // Check if dropped in valid area
      const dropTarget = document.elementFromPoint(e.clientX, e.clientY);
      if (dropTarget?.closest('[data-drop-zone]')) {
        addWidgetFromTemplate(template);
      }
      
      setDraggedTemplate(null);
      setDropZoneActive(null);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }, []);

  // Add widget from template
  const addWidgetFromTemplate = useCallback((template: WidgetTemplate) => {
    const newWidget: Widget = {
      id: `widget-${Date.now()}`,
      type: template.type,
      title: template.title,
      config: template.defaultConfig,
      position: { x: 0, y: 0 },
      size: { width: 4, height: 3 },
    };

    const updatedWidgets = [...widgets, newWidget];
    setWidgets(updatedWidgets);
    onWidgetsChange?.(updatedWidgets);
  }, [widgets, onWidgetsChange]);

  // Remove widget
  const removeWidget = useCallback((widgetId: string) => {
    const updatedWidgets = widgets.filter(w => w.id !== widgetId);
    setWidgets(updatedWidgets);
    onWidgetsChange?.(updatedWidgets);
  }, [widgets, onWidgetsChange]);

  // Handle widget menu
  const handleWidgetMenu = (event: React.MouseEvent<HTMLElement>, widget: Widget) => {
    event.stopPropagation();
    setAnchorEl(event.currentTarget);
    setSelectedWidget(widget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedWidget(null);
  };

  // Render widget content based on type
  const renderWidgetContent = (widget: Widget) => {
    switch (widget.type) {
      case 'chart':
        return (
          <Box sx={{ p: 2, height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Typography variant="h6" color="text.secondary">
              {widget.config.chartType} Chart
            </Typography>
          </Box>
        );
      case 'kpi':
        return (
          <Box sx={{ p: 2, height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
            <Typography variant="h4" fontWeight={700} color="primary">
              {Math.floor(Math.random() * 1000)}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {widget.config.metric}
            </Typography>
          </Box>
        );
      default:
        return (
          <Box sx={{ p: 2, height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Typography variant="body1" color="text.secondary">
              {widget.title}
            </Typography>
          </Box>
        );
    }
  };

  return (
    <Box sx={{ position: 'relative', minHeight: '100vh', p: 3 }}>
      {/* Dashboard Grid */}
      <Grid container spacing={3}>
        {widgets.map((widget) => (
          <Grid item xs={12} sm={6} md={4} lg={3} key={widget.id}>
            <DraggableWidget
              isDragging={draggedWidget?.id === widget.id}
              onMouseDown={(e) => handleDragStart(widget, e)}
              data-widget-id={widget.id}
            >
              {/* Widget Header */}
              <Box sx={{ 
                display: 'flex', 
                justifyContent: 'space-between', 
                alignItems: 'center',
                p: 2,
                borderBottom: `1px solid ${theme.palette.divider}`,
              }}>
                <Typography variant="h6" fontWeight={600}>
                  {widget.title}
                </Typography>
                <IconButton 
                  size="small" 
                  onClick={(e) => handleWidgetMenu(e, widget)}
                >
                  <MoreVert />
                </IconButton>
              </Box>

              {/* Widget Content */}
              {renderWidgetContent(widget)}

              {/* Drag Handle */}
              {editable && (
                <DragHandle>
                  <DragIndicator />
                </DragHandle>
              )}
            </DraggableWidget>
          </Grid>
        ))}

        {/* Empty Drop Zone */}
        {widgets.length === 0 && (
          <Grid item xs={12}>
            <DropZone 
              isActive={!!draggedTemplate}
              data-drop-zone="main"
            >
              <Box sx={{ textAlign: 'center' }}>
                <Dashboard sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" color="text.secondary" gutterBottom>
                  Your dashboard is empty
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Drag widgets from the library to get started
                </Typography>
              </Box>
            </DropZone>
          </Grid>
        )}
      </Grid>

      {/* Add Widget FAB */}
      {editable && (
        <Fab
          color="primary"
          sx={{ position: 'fixed', bottom: 24, right: 24 }}
          onClick={() => setShowLibrary(!showLibrary)}
        >
          <Add />
        </Fab>
      )}

      {/* Widget Library */}
      {showLibrary && (
        <WidgetLibrary>
          <Box sx={{ p: 2, borderBottom: `1px solid ${theme.palette.divider}` }}>
            <Typography variant="h6" fontWeight={600}>
              Widget Library
            </Typography>
          </Box>
          <Box sx={{ p: 2 }}>
            {widgetTemplates.map((template) => (
              <Card
                key={template.id}
                sx={{ 
                  mb: 2, 
                  cursor: 'grab',
                  transition: 'all 0.2s ease',
                  '&:hover': {
                    transform: 'translateY(-2px)',
                    boxShadow: theme.shadows[4],
                  },
                  '&:active': {
                    cursor: 'grabbing',
                  },
                }}
                onMouseDown={(e) => handleTemplateDragStart(template, e)}
              >
                <CardContent sx={{ p: 2 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                    <Box sx={{ mr: 1, color: 'primary.main' }}>
                      {template.icon}
                    </Box>
                    <Typography variant="subtitle2" fontWeight={600}>
                      {template.title}
                    </Typography>
                  </Box>
                  <Typography variant="caption" color="text.secondary">
                    {template.description}
                  </Typography>
                </CardContent>
              </Card>
            ))}
          </Box>
        </WidgetLibrary>
      )}

      {/* Widget Context Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem onClick={() => { setConfigDialog(true); handleMenuClose(); }}>
          <Edit sx={{ mr: 1 }} /> Configure
        </MenuItem>
        <MenuItem onClick={() => { /* Handle fullscreen */ handleMenuClose(); }}>
          <Fullscreen sx={{ mr: 1 }} /> Fullscreen
        </MenuItem>
        <MenuItem 
          onClick={() => { 
            if (selectedWidget) removeWidget(selectedWidget.id); 
            handleMenuClose(); 
          }}
          sx={{ color: 'error.main' }}
        >
          <Delete sx={{ mr: 1 }} /> Remove
        </MenuItem>
      </Menu>

      {/* Widget Configuration Dialog */}
      <Dialog open={configDialog} onClose={() => setConfigDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Configure Widget</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary">
            Widget configuration options would go here...
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfigDialog(false)}>Cancel</Button>
          <Button variant="contained" onClick={() => setConfigDialog(false)}>Save</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default DragDropDashboard;
