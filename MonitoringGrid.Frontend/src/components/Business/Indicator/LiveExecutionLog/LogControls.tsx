import React from 'react';
import {
  Box,
  IconButton,
  Tooltip,
  Switch,
  FormControlLabel,
} from '@mui/material';
import {
  PlayArrow as PlayIcon,
  Pause as PauseIcon,
  Clear as ClearIcon,
  ExpandMore as ExpandIcon,
  ExpandLess as CollapseIcon,
} from '@mui/icons-material';

interface LogControlsProps {
  isPaused: boolean;
  isExpanded: boolean;
  filterErrors: boolean;
  onPauseToggle: () => void;
  onExpandToggle: () => void;
  onFilterToggle: (checked: boolean) => void;
  onClearLog: () => void;
}

export const LogControls: React.FC<LogControlsProps> = ({
  isPaused,
  isExpanded,
  filterErrors,
  onPauseToggle,
  onExpandToggle,
  onFilterToggle,
  onClearLog,
}) => {
  return (
    <Box display="flex" alignItems="center" gap={1}>
      <FormControlLabel
        control={
          <Switch
            checked={filterErrors}
            onChange={(e) => onFilterToggle(e.target.checked)}
            size="small"
          />
        }
        label="Errors Only"
      />
      <Tooltip title={isPaused ? "Resume logging" : "Pause logging"}>
        <IconButton onClick={onPauseToggle} size="small">
          {isPaused ? <PlayIcon /> : <PauseIcon />}
        </IconButton>
      </Tooltip>
      <Tooltip title="Clear log">
        <IconButton onClick={onClearLog} size="small">
          <ClearIcon />
        </IconButton>
      </Tooltip>
      <Tooltip title={isExpanded ? "Collapse" : "Expand"}>
        <IconButton onClick={onExpandToggle} size="small">
          {isExpanded ? <CollapseIcon /> : <ExpandIcon />}
        </IconButton>
      </Tooltip>
    </Box>
  );
};

export default LogControls;
