import React, { useState, useEffect, useRef } from 'react';
import { Box, styled, keyframes, useTheme, alpha } from '@mui/material';

// Advanced keyframe animations
export const microAnimations = {
  // Entrance animations
  fadeInUp: keyframes`
    from {
      opacity: 0;
      transform: translateY(30px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  `,

  fadeInScale: keyframes`
    from {
      opacity: 0;
      transform: scale(0.8);
    }
    to {
      opacity: 1;
      transform: scale(1);
    }
  `,

  slideInLeft: keyframes`
    from {
      opacity: 0;
      transform: translateX(-50px);
    }
    to {
      opacity: 1;
      transform: translateX(0);
    }
  `,

  // Attention animations
  pulse: keyframes`
    0% { transform: scale(1); }
    50% { transform: scale(1.05); }
    100% { transform: scale(1); }
  `,

  shake: keyframes`
    0%, 100% { transform: translateX(0); }
    10%, 30%, 50%, 70%, 90% { transform: translateX(-5px); }
    20%, 40%, 60%, 80% { transform: translateX(5px); }
  `,

  bounce: keyframes`
    0%, 20%, 53%, 80%, 100% { transform: translateY(0); }
    40%, 43% { transform: translateY(-15px); }
    70% { transform: translateY(-7px); }
    90% { transform: translateY(-3px); }
  `,

  // Loading animations
  spin: keyframes`
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
  `,

  ripple: keyframes`
    0% {
      transform: scale(0);
      opacity: 1;
    }
    100% {
      transform: scale(4);
      opacity: 0;
    }
  `,

  // Hover animations
  float: keyframes`
    0%, 100% { transform: translateY(0px); }
    50% { transform: translateY(-10px); }
  `,

  glow: keyframes`
    0%, 100% { box-shadow: 0 0 5px rgba(0, 123, 255, 0.5); }
    50% { box-shadow: 0 0 20px rgba(0, 123, 255, 0.8), 0 0 30px rgba(0, 123, 255, 0.6); }
  `,

  // Progress animations
  progressFill: keyframes`
    0% { width: 0%; }
    100% { width: var(--progress-width); }
  `,

  // Morphing animations
  morphCircleToSquare: keyframes`
    0% { border-radius: 50%; }
    100% { border-radius: 8px; }
  `,

  // Stagger animations
  staggerFadeIn: keyframes`
    0% { opacity: 0; transform: translateY(20px); }
    100% { opacity: 1; transform: translateY(0); }
  `,
};

// Animated container with intersection observer
interface AnimatedContainerProps {
  children: React.ReactNode;
  animation?: keyof typeof microAnimations;
  duration?: number;
  delay?: number;
  threshold?: number;
  triggerOnce?: boolean;
  stagger?: boolean;
  staggerDelay?: number;
}

export const AnimatedContainer: React.FC<AnimatedContainerProps> = ({
  children,
  animation = 'fadeInUp',
  duration = 0.6,
  delay = 0,
  threshold = 0.1,
  triggerOnce = true,
  stagger = false,
  staggerDelay = 0.1,
}) => {
  const [isVisible, setIsVisible] = useState(false);
  const [hasTriggered, setHasTriggered] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && (!triggerOnce || !hasTriggered)) {
          setIsVisible(true);
          setHasTriggered(true);
        } else if (!triggerOnce && !entry.isIntersecting) {
          setIsVisible(false);
        }
      },
      { threshold }
    );

    if (ref.current) {
      observer.observe(ref.current);
    }

    return () => observer.disconnect();
  }, [threshold, triggerOnce, hasTriggered]);

  const StyledContainer = styled(Box)(({ theme }) => ({
    animation: isVisible
      ? `${microAnimations[animation]} ${duration}s ease-out ${delay}s both`
      : 'none',
    ...(stagger && {
      '& > *': {
        animation: isVisible
          ? `${microAnimations.staggerFadeIn} ${duration}s ease-out both`
          : 'none',
      },
      '& > *:nth-of-type(1)': { animationDelay: `${delay}s` },
      '& > *:nth-of-type(2)': { animationDelay: `${delay + staggerDelay}s` },
      '& > *:nth-of-type(3)': { animationDelay: `${delay + staggerDelay * 2}s` },
      '& > *:nth-of-type(4)': { animationDelay: `${delay + staggerDelay * 3}s` },
      '& > *:nth-of-type(5)': { animationDelay: `${delay + staggerDelay * 4}s` },
    }),
  }));

  return <StyledContainer ref={ref}>{children}</StyledContainer>;
};

// Ripple effect component
interface RippleEffectProps {
  children: React.ReactNode;
  color?: string;
  duration?: number;
}

export const RippleEffect: React.FC<RippleEffectProps> = ({
  children,
  color = 'rgba(255, 255, 255, 0.6)',
  duration = 600,
}) => {
  const [ripples, setRipples] = useState<Array<{ x: number; y: number; id: number }>>([]);

  const addRipple = (event: React.MouseEvent<HTMLDivElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    const id = Date.now();

    setRipples(prev => [...prev, { x, y, id }]);

    setTimeout(() => {
      setRipples(prev => prev.filter(ripple => ripple.id !== id));
    }, duration);
  };

  const RippleContainer = styled(Box)({
    position: 'relative',
    overflow: 'hidden',
    cursor: 'pointer',
  });

  const RippleElement = styled(Box)(({ theme }) => ({
    position: 'absolute',
    borderRadius: '50%',
    backgroundColor: color,
    transform: 'scale(0)',
    animation: `${microAnimations.ripple} ${duration}ms linear`,
    pointerEvents: 'none',
  }));

  return (
    <RippleContainer onClick={addRipple}>
      {children}
      {ripples.map(({ x, y, id }) => (
        <RippleElement
          key={id}
          sx={{
            left: x - 10,
            top: y - 10,
            width: 20,
            height: 20,
          }}
        />
      ))}
    </RippleContainer>
  );
};

// Magnetic hover effect
interface MagneticHoverProps {
  children: React.ReactNode;
  strength?: number;
  distance?: number;
}

export const MagneticHover: React.FC<MagneticHoverProps> = ({
  children,
  strength = 0.3,
  distance = 100,
}) => {
  const [position, setPosition] = useState({ x: 0, y: 0 });
  const ref = useRef<HTMLDivElement>(null);

  const handleMouseMove = (event: React.MouseEvent<HTMLDivElement>) => {
    if (!ref.current) return;

    const rect = ref.current.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;

    const deltaX = event.clientX - centerX;
    const deltaY = event.clientY - centerY;
    const distanceFromCenter = Math.sqrt(deltaX * deltaX + deltaY * deltaY);

    if (distanceFromCenter < distance) {
      const factor = (distance - distanceFromCenter) / distance;
      setPosition({
        x: deltaX * strength * factor,
        y: deltaY * strength * factor,
      });
    }
  };

  const handleMouseLeave = () => {
    setPosition({ x: 0, y: 0 });
  };

  const MagneticContainer = styled(Box)({
    transition: 'transform 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
    transform: `translate(${position.x}px, ${position.y}px)`,
  });

  return (
    <MagneticContainer ref={ref} onMouseMove={handleMouseMove} onMouseLeave={handleMouseLeave}>
      {children}
    </MagneticContainer>
  );
};

// Parallax scroll effect
interface ParallaxScrollProps {
  children: React.ReactNode;
  speed?: number;
  direction?: 'up' | 'down' | 'left' | 'right';
}

export const ParallaxScroll: React.FC<ParallaxScrollProps> = ({
  children,
  speed = 0.5,
  direction = 'up',
}) => {
  const [offset, setOffset] = useState(0);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleScroll = () => {
      if (!ref.current) return;

      const rect = ref.current.getBoundingClientRect();
      const scrolled = window.pageYOffset;
      const rate = scrolled * -speed;

      setOffset(rate);
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, [speed]);

  const getTransform = () => {
    switch (direction) {
      case 'up':
        return `translateY(${offset}px)`;
      case 'down':
        return `translateY(${-offset}px)`;
      case 'left':
        return `translateX(${offset}px)`;
      case 'right':
        return `translateX(${-offset}px)`;
      default:
        return `translateY(${offset}px)`;
    }
  };

  const ParallaxContainer = styled(Box)({
    transform: getTransform(),
    transition: 'transform 0.1s ease-out',
  });

  return <ParallaxContainer ref={ref}>{children}</ParallaxContainer>;
};

// Morphing button component
interface MorphingButtonProps {
  children: React.ReactNode;
  morphTo?: React.ReactNode;
  trigger?: 'hover' | 'click';
  duration?: number;
  onClick?: () => void;
}

export const MorphingButton: React.FC<MorphingButtonProps> = ({
  children,
  morphTo,
  trigger = 'hover',
  duration = 0.3,
  onClick,
}) => {
  const [isMorphed, setIsMorphed] = useState(false);
  const theme = useTheme();

  const handleInteraction = () => {
    if (trigger === 'hover') {
      setIsMorphed(true);
    } else if (trigger === 'click') {
      setIsMorphed(!isMorphed);
      onClick?.();
    }
  };

  const handleLeave = () => {
    if (trigger === 'hover') {
      setIsMorphed(false);
    }
  };

  const MorphContainer = styled(Box)(({ theme }) => ({
    position: 'relative',
    overflow: 'hidden',
    cursor: 'pointer',
    transition: `all ${duration}s cubic-bezier(0.4, 0, 0.2, 1)`,
    '&:hover':
      trigger === 'hover'
        ? {
            transform: 'scale(1.05)',
          }
        : {},
  }));

  const ContentWrapper = styled(Box)({
    transition: `all ${duration}s cubic-bezier(0.4, 0, 0.2, 1)`,
    opacity: isMorphed ? 0 : 1,
    transform: isMorphed ? 'scale(0.8)' : 'scale(1)',
  });

  const MorphWrapper = styled(Box)({
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    transition: `all ${duration}s cubic-bezier(0.4, 0, 0.2, 1)`,
    opacity: isMorphed ? 1 : 0,
    transform: isMorphed ? 'scale(1)' : 'scale(0.8)',
  });

  return (
    <MorphContainer
      onMouseEnter={handleInteraction}
      onMouseLeave={handleLeave}
      onClick={trigger === 'click' ? handleInteraction : onClick}
    >
      <ContentWrapper>{children}</ContentWrapper>
      {morphTo && <MorphWrapper>{morphTo}</MorphWrapper>}
    </MorphContainer>
  );
};

export default {
  AnimatedContainer,
  RippleEffect,
  MagneticHover,
  ParallaxScroll,
  MorphingButton,
  microAnimations,
};
