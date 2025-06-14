import React, { useState } from 'react';
import { Button, Tooltip, IconButton } from '@mui/material';
import { Analytics as AnalyticsIcon, BarChart as BarChartIcon } from '@mui/icons-material';
import StatisticsBrowser from './StatisticsBrowser';

interface StatisticsBrowserButtonProps {
  collectorId?: number;
  itemName?: string;
  variant?: 'button' | 'icon';
  size?: 'small' | 'medium' | 'large';
  disabled?: boolean;
  tooltip?: string;
}

export const StatisticsBrowserButton: React.FC<StatisticsBrowserButtonProps> = ({
  collectorId,
  itemName,
  variant = 'button',
  size = 'small',
  disabled = false,
  tooltip = 'Browse Statistics',
}) => {
  const [open, setOpen] = useState(false);

  const handleOpen = () => {
    setOpen(true);
  };

  const handleClose = () => {
    setOpen(false);
  };

  const buttonContent =
    variant === 'icon' ? (
      <Tooltip title={tooltip}>
        <IconButton onClick={handleOpen} disabled={disabled} size={size} color="primary">
          <AnalyticsIcon />
        </IconButton>
      </Tooltip>
    ) : (
      <Button
        variant="outlined"
        size={size}
        startIcon={<BarChartIcon />}
        onClick={handleOpen}
        disabled={disabled}
      >
        Statistics
      </Button>
    );

  return (
    <>
      {buttonContent}
      <StatisticsBrowser
        open={open}
        onClose={handleClose}
        initialCollectorId={collectorId}
        initialItemName={itemName}
      />
    </>
  );
};

export default StatisticsBrowserButton;
