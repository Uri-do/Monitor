import React from 'react';
import { ConfirmationDialog } from '@/components';
import { IndicatorDto } from '@/types/api';

interface IndicatorActionDialogsProps {
  indicator: IndicatorDto;
  deleteDialogOpen: boolean;
  executeDialogOpen: boolean;
  deleteLoading: boolean;
  executeLoading: boolean;
  onDeleteConfirm: () => void;
  onDeleteCancel: () => void;
  onExecuteConfirm: () => void;
  onExecuteCancel: () => void;
}

export const IndicatorActionDialogs: React.FC<IndicatorActionDialogsProps> = ({
  indicator,
  deleteDialogOpen,
  executeDialogOpen,
  deleteLoading,
  executeLoading,
  onDeleteConfirm,
  onDeleteCancel,
  onExecuteConfirm,
  onExecuteCancel,
}) => {
  return (
    <>
      {/* Delete Confirmation Dialog */}
      <ConfirmationDialog
        open={deleteDialogOpen}
        title="Delete Indicator"
        message={`Are you sure you want to delete "${indicator.indicatorName}"? This action cannot be undone.`}
        confirmLabel="Delete"
        confirmColor="error"
        loading={deleteLoading}
        onConfirm={onDeleteConfirm}
        onCancel={onDeleteCancel}
      />

      {/* Execute Confirmation Dialog */}
      <ConfirmationDialog
        open={executeDialogOpen}
        title="Execute Indicator"
        message={`Execute "${indicator.indicatorName}" now? This will run the indicator outside of its normal schedule.`}
        confirmLabel="Execute"
        loading={executeLoading}
        onConfirm={onExecuteConfirm}
        onCancel={onExecuteCancel}
      />
    </>
  );
};

export default IndicatorActionDialogs;
