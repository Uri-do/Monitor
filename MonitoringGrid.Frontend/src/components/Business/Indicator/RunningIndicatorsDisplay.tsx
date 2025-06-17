import React from 'react';
import { SectionVariant } from './RunningIndicatorsDisplay/SectionVariant';
import { CardVariant } from './RunningIndicatorsDisplay/CardVariant';
import { CompactVariant } from './RunningIndicatorsDisplay/CompactVariant';

// Re-export types for backward compatibility
export type { RunningIndicator } from './types';

interface RunningIndicatorsDisplayProps {
  runningIndicators: RunningIndicator[];
  variant?: 'card' | 'section';
  title?: string;
  showNavigateButton?: boolean;
  onNavigate?: () => void;
  maxDisplay?: number;
  showProgress?: boolean;
  compact?: boolean;
}

const RunningIndicatorsDisplay: React.FC<RunningIndicatorsDisplayProps> = ({
  runningIndicators,
  variant = 'card',
  title = 'Currently Executing Indicators',
  showNavigateButton = false,
  onNavigate,
  maxDisplay,
  showProgress = true,
  compact = false,
}) => {
  const displayIndicators = maxDisplay ? runningIndicators.slice(0, maxDisplay) : runningIndicators;

  if (compact) {
    return (
      <CompactVariant
        runningIndicators={runningIndicators}
        displayIndicators={displayIndicators}
        showProgress={showProgress}
      />
    );
  }

  if (variant === 'section') {
    return (
      <SectionVariant
        title={title}
        runningIndicators={runningIndicators}
        displayIndicators={displayIndicators}
        showProgress={showProgress}
      />
    );
  }

  // Card variant (default)
  return (
    <CardVariant
      title={title}
      runningIndicators={runningIndicators}
      displayIndicators={displayIndicators}
      showProgress={showProgress}
      showNavigateButton={showNavigateButton}
      onNavigate={onNavigate}
    />
  );
};

export default RunningIndicatorsDisplay;
