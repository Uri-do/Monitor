import React from 'react';
import { Card, CardProps } from 'antd';

interface CustomCardProps extends CardProps {
  children: React.ReactNode;
  gradient?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  hoverEffect?: boolean;
  glowEffect?: boolean;
}

const gradientMap = {
  primary: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
  secondary: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
  success: 'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
  warning: 'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
  error: 'linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%)',
  info: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
};

const borderColorMap = {
  primary: 'rgba(102, 126, 234, 0.2)',
  secondary: 'rgba(240, 147, 251, 0.2)',
  success: 'rgba(67, 233, 123, 0.2)',
  warning: 'rgba(250, 112, 154, 0.2)',
  error: 'rgba(255, 107, 107, 0.2)',
  info: 'rgba(79, 172, 254, 0.2)',
};

export type { CustomCardProps };

export const CustomCard: React.FC<CustomCardProps> = ({
  children,
  gradient = 'primary',
  hoverEffect = true,
  glowEffect = false,
  style,
  ...props
}) => {
  const cardStyle: React.CSSProperties = {
    background: glowEffect ? gradientMap[gradient] : undefined,
    border: `1px solid ${borderColorMap[gradient]}`,
    borderRadius: '8px',
    transition: 'all 0.3s ease-in-out',
    position: 'relative',
    overflow: 'hidden',
    ...(hoverEffect && {
      cursor: 'pointer',
    }),
    ...(glowEffect && {
      color: 'white',
    }),
    ...style,
  };

  const hoverClass = hoverEffect ? 'custom-card-hover' : '';

  return (
    <>
      <style>
        {`
          .custom-card-hover:hover {
            transform: translateY(-4px);
            box-shadow: 0 12px 30px rgba(102, 126, 234, 0.3);
          }
        `}
      </style>
      <Card
        style={cardStyle}
        className={hoverClass}
        {...props}
      >
        <div style={{ position: 'relative', zIndex: 1 }}>
          {children}
        </div>
      </Card>
    </>
  );
};

export default CustomCard;
