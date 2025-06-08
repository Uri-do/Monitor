import React, { useState, useRef, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Card,
  CardContent,
  Button,
  IconButton,
  useTheme,
  alpha,
  styled,
  keyframes,
} from '@mui/material';
import { TrendingUp, Favorite, Share, MoreVert, Visibility, Star } from '@mui/icons-material';

// Advanced hover animations
const floatUp = keyframes`
  0% { transform: translateY(0px); }
  100% { transform: translateY(-8px); }
`;

const scaleIn = keyframes`
  0% { transform: scale(0); opacity: 0; }
  100% { transform: scale(1); opacity: 1; }
`;

const slideInFromBottom = keyframes`
  0% { transform: translateY(100%); opacity: 0; }
  100% { transform: translateY(0); opacity: 1; }
`;

const shimmer = keyframes`
  0% { background-position: -200px 0; }
  100% { background-position: calc(200px + 100%) 0; }
`;

const glow = keyframes`
  0%, 100% { box-shadow: 0 0 5px rgba(25, 118, 210, 0.5); }
  50% { box-shadow: 0 0 20px rgba(25, 118, 210, 0.8), 0 0 30px rgba(25, 118, 210, 0.6); }
`;

const morphBorder = keyframes`
  0% { border-radius: 8px; }
  50% { border-radius: 24px; }
  100% { border-radius: 8px; }
`;

// Tilt effect component
interface TiltCardProps {
  children: React.ReactNode;
  maxTilt?: number;
  scale?: number;
  speed?: number;
}

export const TiltCard: React.FC<TiltCardProps> = ({
  children,
  maxTilt = 15,
  scale = 1.05,
  speed = 300,
}) => {
  const [tilt, setTilt] = useState({ x: 0, y: 0 });
  const [isHovered, setIsHovered] = useState(false);
  const cardRef = useRef<HTMLDivElement>(null);

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!cardRef.current) return;

    const rect = cardRef.current.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;

    const tiltX = ((e.clientY - centerY) / (rect.height / 2)) * maxTilt;
    const tiltY = ((e.clientX - centerX) / (rect.width / 2)) * -maxTilt;

    setTilt({ x: tiltX, y: tiltY });
  };

  const handleMouseLeave = () => {
    setTilt({ x: 0, y: 0 });
    setIsHovered(false);
  };

  const handleMouseEnter = () => {
    setIsHovered(true);
  };

  const TiltContainer = styled(Box)({
    perspective: '1000px',
    transition: `transform ${speed}ms cubic-bezier(0.4, 0, 0.2, 1)`,
    transform: `rotateX(${tilt.x}deg) rotateY(${tilt.y}deg) scale(${isHovered ? scale : 1})`,
    transformStyle: 'preserve-3d',
  });

  return (
    <TiltContainer
      ref={cardRef}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      onMouseEnter={handleMouseEnter}
    >
      {children}
    </TiltContainer>
  );
};

// Magnetic button with attraction effect
interface MagneticButtonProps {
  children: React.ReactNode;
  strength?: number;
  distance?: number;
  onClick?: () => void;
}

export const MagneticButton: React.FC<MagneticButtonProps> = ({
  children,
  strength = 0.4,
  distance = 80,
  onClick,
}) => {
  const [position, setPosition] = useState({ x: 0, y: 0 });
  const [isHovered, setIsHovered] = useState(false);
  const buttonRef = useRef<HTMLButtonElement>(null);

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!buttonRef.current) return;

    const rect = buttonRef.current.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;

    const deltaX = e.clientX - centerX;
    const deltaY = e.clientY - centerY;
    const distanceFromCenter = Math.sqrt(deltaX * deltaX + deltaY * deltaY);

    if (distanceFromCenter < distance) {
      const factor = (distance - distanceFromCenter) / distance;
      setPosition({
        x: deltaX * strength * factor,
        y: deltaY * strength * factor,
      });
      setIsHovered(true);
    } else {
      setPosition({ x: 0, y: 0 });
      setIsHovered(false);
    }
  };

  const handleMouseLeave = () => {
    setPosition({ x: 0, y: 0 });
    setIsHovered(false);
  };

  const MagneticContainer = styled(Button)(({ theme }) => ({
    transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
    transform: `translate(${position.x}px, ${position.y}px) scale(${isHovered ? 1.1 : 1})`,
    boxShadow: isHovered ? theme.shadows[8] : theme.shadows[2],
  }));

  return (
    <MagneticContainer
      ref={buttonRef}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      onClick={onClick}
      variant="contained"
    >
      {children}
    </MagneticContainer>
  );
};

// Reveal card with sliding overlay
interface RevealCardProps {
  children: React.ReactNode;
  overlay: React.ReactNode;
  direction?: 'up' | 'down' | 'left' | 'right';
}

export const RevealCard: React.FC<RevealCardProps> = ({ children, overlay, direction = 'up' }) => {
  const [isHovered, setIsHovered] = useState(false);
  const theme = useTheme();

  const getOverlayTransform = () => {
    if (!isHovered) {
      switch (direction) {
        case 'up':
          return 'translateY(100%)';
        case 'down':
          return 'translateY(-100%)';
        case 'left':
          return 'translateX(100%)';
        case 'right':
          return 'translateX(-100%)';
        default:
          return 'translateY(100%)';
      }
    }
    return 'translate(0, 0)';
  };

  const RevealContainer = styled(Card)({
    position: 'relative',
    overflow: 'hidden',
    cursor: 'pointer',
    '&:hover': {
      '& .reveal-overlay': {
        transform: 'translate(0, 0)',
      },
    },
  });

  const Overlay = styled(Box)(({ theme }) => ({
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: alpha(theme.palette.primary.main, 0.95),
    color: theme.palette.primary.contrastText,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    transform: getOverlayTransform(),
    transition: 'transform 0.4s cubic-bezier(0.4, 0, 0.2, 1)',
  }));

  return (
    <RevealContainer
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {children}
      <Overlay className="reveal-overlay">{overlay}</Overlay>
    </RevealContainer>
  );
};

// Shimmer loading effect
interface ShimmerCardProps {
  children: React.ReactNode;
  loading?: boolean;
}

export const ShimmerCard: React.FC<ShimmerCardProps> = ({ children, loading = false }) => {
  const theme = useTheme();

  const ShimmerContainer = styled(Card)(({ theme }) => ({
    position: 'relative',
    overflow: 'hidden',
    '&::before': loading
      ? {
          content: '""',
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `linear-gradient(90deg, transparent, ${alpha(theme.palette.common.white, 0.4)}, transparent)`,
          animation: `${shimmer} 2s infinite`,
          zIndex: 1,
        }
      : {},
  }));

  return (
    <ShimmerContainer>
      <Box sx={{ opacity: loading ? 0.7 : 1, transition: 'opacity 0.3s ease' }}>{children}</Box>
    </ShimmerContainer>
  );
};

// Morphing icon button
interface MorphingIconButtonProps {
  icon: React.ReactNode;
  morphIcon: React.ReactNode;
  onClick?: () => void;
  color?: 'primary' | 'secondary' | 'error' | 'warning' | 'info' | 'success';
}

export const MorphingIconButton: React.FC<MorphingIconButtonProps> = ({
  icon,
  morphIcon,
  onClick,
  color = 'primary',
}) => {
  const [isMorphed, setIsMorphed] = useState(false);
  const theme = useTheme();

  const MorphContainer = styled(IconButton)(({ theme }) => ({
    position: 'relative',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    '&:hover': {
      transform: 'scale(1.1)',
      animation: `${glow} 2s ease-in-out infinite`,
    },
  }));

  const IconWrapper = styled(Box)({
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    transform: isMorphed ? 'scale(0) rotate(180deg)' : 'scale(1) rotate(0deg)',
    opacity: isMorphed ? 0 : 1,
  });

  const MorphWrapper = styled(Box)({
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: `translate(-50%, -50%) scale(${isMorphed ? 1 : 0}) rotate(${isMorphed ? 0 : -180}deg)`,
    opacity: isMorphed ? 1 : 0,
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  });

  return (
    <MorphContainer
      color={color}
      onMouseEnter={() => setIsMorphed(true)}
      onMouseLeave={() => setIsMorphed(false)}
      onClick={onClick}
    >
      <IconWrapper>{icon}</IconWrapper>
      <MorphWrapper>{morphIcon}</MorphWrapper>
    </MorphContainer>
  );
};

// Floating action card
interface FloatingActionCardProps {
  children: React.ReactNode;
  actions: Array<{
    icon: React.ReactNode;
    label: string;
    onClick: () => void;
    color?: string;
  }>;
}

export const FloatingActionCard: React.FC<FloatingActionCardProps> = ({ children, actions }) => {
  const [isHovered, setIsHovered] = useState(false);
  const theme = useTheme();

  const FloatingContainer = styled(Card)(({ theme }) => ({
    position: 'relative',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    '&:hover': {
      transform: 'translateY(-8px)',
      boxShadow: theme.shadows[12],
    },
  }));

  const ActionsContainer = styled(Box)(({ theme }) => ({
    position: 'absolute',
    top: 16,
    right: 16,
    display: 'flex',
    flexDirection: 'column',
    gap: theme.spacing(1),
    opacity: isHovered ? 1 : 0,
    transform: isHovered ? 'translateX(0)' : 'translateX(20px)',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    pointerEvents: isHovered ? 'auto' : 'none',
  }));

  const ActionButton = styled(IconButton)(({ theme }) => ({
    backgroundColor: theme.palette.background.paper,
    boxShadow: theme.shadows[4],
    '&:hover': {
      transform: 'scale(1.1)',
      boxShadow: theme.shadows[8],
    },
  }));

  return (
    <FloatingContainer
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {children}
      <ActionsContainer>
        {actions.map((action, index) => (
          <ActionButton
            key={index}
            onClick={action.onClick}
            size="small"
            sx={{
              animationDelay: `${index * 50}ms`,
              animation: isHovered ? `${scaleIn} 0.3s ease-out` : 'none',
            }}
          >
            {action.icon}
          </ActionButton>
        ))}
      </ActionsContainer>
    </FloatingContainer>
  );
};

// Showcase component demonstrating all hover effects
export const HoverEffectsShowcase: React.FC = () => {
  const theme = useTheme();

  return (
    <Box sx={{ p: 4, display: 'flex', flexDirection: 'column', gap: 4 }}>
      <Typography variant="h4" gutterBottom>
        Advanced Hover Effects & Micro-interactions
      </Typography>

      {/* Tilt Cards */}
      <Box>
        <Typography variant="h6" gutterBottom>
          Tilt Effect Cards
        </Typography>
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          <TiltCard maxTilt={10} scale={1.05}>
            <Card sx={{ width: 200, height: 150 }}>
              <CardContent>
                <Typography variant="h6">Tilt Card</Typography>
                <Typography variant="body2" color="text.secondary">
                  Hover to see 3D tilt effect
                </Typography>
              </CardContent>
            </Card>
          </TiltCard>
        </Box>
      </Box>

      {/* Magnetic Buttons */}
      <Box>
        <Typography variant="h6" gutterBottom>
          Magnetic Attraction
        </Typography>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <MagneticButton strength={0.3} distance={100}>
            Magnetic Button
          </MagneticButton>
        </Box>
      </Box>

      {/* Reveal Cards */}
      <Box>
        <Typography variant="h6" gutterBottom>
          Reveal Overlay Effects
        </Typography>
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          <RevealCard
            direction="up"
            overlay={
              <Box sx={{ textAlign: 'center' }}>
                <Typography variant="h6">Revealed!</Typography>
                <Typography variant="body2">Hidden content appears</Typography>
              </Box>
            }
          >
            <Card sx={{ width: 200, height: 150 }}>
              <CardContent>
                <Typography variant="h6">Hover Me</Typography>
                <Typography variant="body2" color="text.secondary">
                  Overlay slides up on hover
                </Typography>
              </CardContent>
            </Card>
          </RevealCard>
        </Box>
      </Box>

      {/* Morphing Icons */}
      <Box>
        <Typography variant="h6" gutterBottom>
          Morphing Icon Buttons
        </Typography>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <MorphingIconButton icon={<Favorite />} morphIcon={<Star />} color="error" />
          <MorphingIconButton icon={<Visibility />} morphIcon={<Share />} color="primary" />
        </Box>
      </Box>

      {/* Floating Action Cards */}
      <Box>
        <Typography variant="h6" gutterBottom>
          Floating Action Cards
        </Typography>
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          <FloatingActionCard
            actions={[
              { icon: <Favorite />, label: 'Like', onClick: () => console.log('Liked') },
              { icon: <Share />, label: 'Share', onClick: () => console.log('Shared') },
              { icon: <MoreVert />, label: 'More', onClick: () => console.log('More') },
            ]}
          >
            <Card sx={{ width: 250, height: 180 }}>
              <CardContent>
                <Typography variant="h6">Interactive Card</Typography>
                <Typography variant="body2" color="text.secondary">
                  Hover to reveal floating actions
                </Typography>
                <Box sx={{ mt: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
                  <TrendingUp color="success" />
                  <Typography variant="body2">+12.5%</Typography>
                </Box>
              </CardContent>
            </Card>
          </FloatingActionCard>
        </Box>
      </Box>
    </Box>
  );
};

export default {
  TiltCard,
  MagneticButton,
  RevealCard,
  ShimmerCard,
  MorphingIconButton,
  FloatingActionCard,
  HoverEffectsShowcase,
};
