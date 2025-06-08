import React, { useState, useRef, useEffect, useCallback } from 'react';
import { Box, Paper, Typography, useTheme, alpha, styled, keyframes } from '@mui/material';

// Gesture animations
const swipeLeft = keyframes`
  0% { transform: translateX(0); }
  50% { transform: translateX(-20px); }
  100% { transform: translateX(0); }
`;

const swipeRight = keyframes`
  0% { transform: translateX(0); }
  50% { transform: translateX(20px); }
  100% { transform: translateX(0); }
`;

const pinchZoom = keyframes`
  0% { transform: scale(1); }
  50% { transform: scale(1.1); }
  100% { transform: scale(1); }
`;

const longPressRipple = keyframes`
  0% { transform: scale(0); opacity: 0.7; }
  100% { transform: scale(2); opacity: 0; }
`;

// Touch gesture types
interface TouchPoint {
  x: number;
  y: number;
  id: number;
}

interface GestureState {
  isActive: boolean;
  startTime: number;
  startPoints: TouchPoint[];
  currentPoints: TouchPoint[];
  type: 'tap' | 'swipe' | 'pinch' | 'longpress' | 'pan' | null;
  direction?: 'left' | 'right' | 'up' | 'down';
  distance?: number;
  scale?: number;
  velocity?: { x: number; y: number };
}

// Gesture configuration
interface GestureConfig {
  swipeThreshold: number;
  longPressDelay: number;
  tapTimeout: number;
  pinchThreshold: number;
  panThreshold: number;
  velocityThreshold: number;
}

const defaultConfig: GestureConfig = {
  swipeThreshold: 50,
  longPressDelay: 500,
  tapTimeout: 300,
  pinchThreshold: 10,
  panThreshold: 10,
  velocityThreshold: 0.5,
};

// Gesture event handlers
interface GestureHandlers {
  onTap?: (point: TouchPoint) => void;
  onDoubleTap?: (point: TouchPoint) => void;
  onLongPress?: (point: TouchPoint) => void;
  onSwipe?: (direction: string, distance: number, velocity: { x: number; y: number }) => void;
  onPinch?: (scale: number, center: TouchPoint) => void;
  onPan?: (delta: { x: number; y: number }, velocity: { x: number; y: number }) => void;
  onRotate?: (angle: number, center: TouchPoint) => void;
}

// Swipeable card component
interface SwipeableCardProps {
  children: React.ReactNode;
  onSwipeLeft?: () => void;
  onSwipeRight?: () => void;
  swipeThreshold?: number;
  disabled?: boolean;
}

export const SwipeableCard: React.FC<SwipeableCardProps> = ({
  children,
  onSwipeLeft,
  onSwipeRight,
  swipeThreshold = 100,
  disabled = false,
}) => {
  const [swipeState, setSwipeState] = useState({
    isDragging: false,
    startX: 0,
    currentX: 0,
    direction: null as 'left' | 'right' | null,
  });

  const cardRef = useRef<HTMLDivElement>(null);
  const theme = useTheme();

  const handleTouchStart = (e: React.TouchEvent) => {
    if (disabled) return;

    const touch = e.touches[0];
    setSwipeState({
      isDragging: true,
      startX: touch.clientX,
      currentX: touch.clientX,
      direction: null,
    });
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    if (!swipeState.isDragging || disabled) return;

    const touch = e.touches[0];
    const deltaX = touch.clientX - swipeState.startX;

    setSwipeState(prev => ({
      ...prev,
      currentX: touch.clientX,
      direction: deltaX > 0 ? 'right' : 'left',
    }));
  };

  const handleTouchEnd = () => {
    if (!swipeState.isDragging || disabled) return;

    const deltaX = swipeState.currentX - swipeState.startX;
    const absDistance = Math.abs(deltaX);

    if (absDistance > swipeThreshold) {
      if (deltaX > 0 && onSwipeRight) {
        onSwipeRight();
      } else if (deltaX < 0 && onSwipeLeft) {
        onSwipeLeft();
      }
    }

    setSwipeState({
      isDragging: false,
      startX: 0,
      currentX: 0,
      direction: null,
    });
  };

  const translateX = swipeState.isDragging
    ? Math.max(-swipeThreshold, Math.min(swipeThreshold, swipeState.currentX - swipeState.startX))
    : 0;

  const SwipeContainer = styled(Paper)(({ theme }) => ({
    transform: `translateX(${translateX}px)`,
    transition: swipeState.isDragging ? 'none' : 'transform 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    cursor: disabled ? 'default' : 'grab',
    userSelect: 'none',
    touchAction: 'pan-y',
    '&:active': {
      cursor: disabled ? 'default' : 'grabbing',
    },
  }));

  return (
    <SwipeContainer
      ref={cardRef}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
      sx={{
        position: 'relative',
        overflow: 'hidden',
        backgroundColor:
          swipeState.direction === 'left'
            ? alpha(theme.palette.error.main, 0.1)
            : swipeState.direction === 'right'
              ? alpha(theme.palette.success.main, 0.1)
              : 'background.paper',
      }}
    >
      {children}

      {/* Swipe indicators */}
      {swipeState.isDragging && (
        <>
          {swipeState.direction === 'left' && (
            <Box
              sx={{
                position: 'absolute',
                right: 16,
                top: '50%',
                transform: 'translateY(-50%)',
                color: 'error.main',
                opacity: Math.min(1, Math.abs(translateX) / swipeThreshold),
              }}
            >
              ← Delete
            </Box>
          )}
          {swipeState.direction === 'right' && (
            <Box
              sx={{
                position: 'absolute',
                left: 16,
                top: '50%',
                transform: 'translateY(-50%)',
                color: 'success.main',
                opacity: Math.min(1, Math.abs(translateX) / swipeThreshold),
              }}
            >
              Archive →
            </Box>
          )}
        </>
      )}
    </SwipeContainer>
  );
};

// Pinch-to-zoom container
interface PinchZoomProps {
  children: React.ReactNode;
  minScale?: number;
  maxScale?: number;
  onScaleChange?: (scale: number) => void;
}

export const PinchZoom: React.FC<PinchZoomProps> = ({
  children,
  minScale = 0.5,
  maxScale = 3,
  onScaleChange,
}) => {
  const [transform, setTransform] = useState({
    scale: 1,
    translateX: 0,
    translateY: 0,
  });

  const [gestureState, setGestureState] = useState({
    initialDistance: 0,
    initialScale: 1,
    isGesturing: false,
  });

  const containerRef = useRef<HTMLDivElement>(null);

  const getDistance = (touches: React.TouchList) => {
    if (touches.length < 2) return 0;

    const touch1 = touches[0];
    const touch2 = touches[1];

    return Math.sqrt(
      Math.pow(touch2.clientX - touch1.clientX, 2) + Math.pow(touch2.clientY - touch1.clientY, 2)
    );
  };

  const handleTouchStart = (e: React.TouchEvent) => {
    if (e.touches.length === 2) {
      const distance = getDistance(e.touches);
      setGestureState({
        initialDistance: distance,
        initialScale: transform.scale,
        isGesturing: true,
      });
    }
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    if (e.touches.length === 2 && gestureState.isGesturing) {
      e.preventDefault();

      const distance = getDistance(e.touches);
      const scale = Math.max(
        minScale,
        Math.min(maxScale, gestureState.initialScale * (distance / gestureState.initialDistance))
      );

      setTransform(prev => ({ ...prev, scale }));
      onScaleChange?.(scale);
    }
  };

  const handleTouchEnd = () => {
    setGestureState(prev => ({ ...prev, isGesturing: false }));
  };

  const ZoomContainer = styled(Box)({
    transform: `scale(${transform.scale}) translate(${transform.translateX}px, ${transform.translateY}px)`,
    transformOrigin: 'center center',
    transition: gestureState.isGesturing ? 'none' : 'transform 0.3s ease',
    touchAction: 'none',
  });

  return (
    <Box
      ref={containerRef}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
      sx={{ overflow: 'hidden', position: 'relative' }}
    >
      <ZoomContainer>{children}</ZoomContainer>
    </Box>
  );
};

// Long press component
interface LongPressProps {
  children: React.ReactNode;
  onLongPress: () => void;
  delay?: number;
  disabled?: boolean;
}

export const LongPress: React.FC<LongPressProps> = ({
  children,
  onLongPress,
  delay = 500,
  disabled = false,
}) => {
  const [isPressed, setIsPressed] = useState(false);
  const [showRipple, setShowRipple] = useState(false);
  const timeoutRef = useRef<NodeJS.Timeout>();
  const theme = useTheme();

  const startPress = useCallback(() => {
    if (disabled) return;

    setIsPressed(true);
    setShowRipple(true);

    timeoutRef.current = setTimeout(() => {
      onLongPress();
      setIsPressed(false);
      setShowRipple(false);
    }, delay);
  }, [onLongPress, delay, disabled]);

  const endPress = useCallback(() => {
    setIsPressed(false);
    setShowRipple(false);

    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
  }, []);

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  const LongPressContainer = styled(Box)(({ theme }) => ({
    position: 'relative',
    cursor: disabled ? 'default' : 'pointer',
    userSelect: 'none',
    '&::after': showRipple
      ? {
          content: '""',
          position: 'absolute',
          top: '50%',
          left: '50%',
          width: '20px',
          height: '20px',
          borderRadius: '50%',
          backgroundColor: alpha(theme.palette.primary.main, 0.3),
          transform: 'translate(-50%, -50%)',
          animation: `${longPressRipple} ${delay}ms ease-out`,
        }
      : {},
  }));

  return (
    <LongPressContainer
      onMouseDown={startPress}
      onMouseUp={endPress}
      onMouseLeave={endPress}
      onTouchStart={startPress}
      onTouchEnd={endPress}
      onTouchCancel={endPress}
    >
      {children}
    </LongPressContainer>
  );
};

// Pull to refresh component
interface PullToRefreshProps {
  children: React.ReactNode;
  onRefresh: () => Promise<void>;
  threshold?: number;
  disabled?: boolean;
}

export const PullToRefresh: React.FC<PullToRefreshProps> = ({
  children,
  onRefresh,
  threshold = 80,
  disabled = false,
}) => {
  const theme = useTheme();
  const [pullState, setPullState] = useState({
    isPulling: false,
    isRefreshing: false,
    pullDistance: 0,
    startY: 0,
  });

  const containerRef = useRef<HTMLDivElement>(null);

  const handleTouchStart = (e: React.TouchEvent) => {
    if (disabled || window.scrollY > 0) return;

    setPullState(prev => ({
      ...prev,
      startY: e.touches[0].clientY,
      isPulling: true,
    }));
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    if (!pullState.isPulling || disabled) return;

    const currentY = e.touches[0].clientY;
    const pullDistance = Math.max(0, currentY - pullState.startY);

    if (pullDistance > 0) {
      e.preventDefault();
      setPullState(prev => ({ ...prev, pullDistance }));
    }
  };

  const handleTouchEnd = async () => {
    if (!pullState.isPulling || disabled) return;

    if (pullState.pullDistance > threshold) {
      setPullState(prev => ({ ...prev, isRefreshing: true }));
      await onRefresh();
      setPullState(prev => ({ ...prev, isRefreshing: false }));
    }

    setPullState(prev => ({
      ...prev,
      isPulling: false,
      pullDistance: 0,
    }));
  };

  const pullProgress = Math.min(1, pullState.pullDistance / threshold);

  return (
    <Box
      ref={containerRef}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
      sx={{ position: 'relative', overflow: 'hidden' }}
    >
      {/* Pull indicator */}
      {pullState.isPulling && (
        <Box
          sx={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            height: pullState.pullDistance,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            backgroundColor: alpha(theme.palette.primary.main, 0.1),
            zIndex: 1000,
          }}
        >
          <Typography
            variant="body2"
            color="primary"
            sx={{
              opacity: pullProgress,
              transform: `scale(${0.8 + pullProgress * 0.2})`,
            }}
          >
            {pullProgress >= 1 ? 'Release to refresh' : 'Pull to refresh'}
          </Typography>
        </Box>
      )}

      {/* Content */}
      <Box
        sx={{
          transform: `translateY(${pullState.pullDistance}px)`,
          transition: pullState.isPulling ? 'none' : 'transform 0.3s ease',
        }}
      >
        {children}
      </Box>
    </Box>
  );
};

export default {
  SwipeableCard,
  PinchZoom,
  LongPress,
  PullToRefresh,
};
