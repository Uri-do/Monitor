import React from 'react';
import { Assessment as IndicatorIcon } from '@mui/icons-material';
import * as yup from 'yup';

import { CreateFormDialog, EditFormDialog } from '../../UI/GenericFormDialog';
import { DomainValidators, BaseValidators } from '../../../utils/validationSchemas';
import { CreateIndicatorRequest, UpdateIndicatorRequest } from '../../../services/indicatorService';

// Import form sections
import {
  BasicInformationFormSection,
  DataSourceFormSection,
  SchedulingFormSection,
  ThresholdFormSection,
  PriorityOwnershipFormSection,
} from './FormSections';

// Form data interface
interface IndicatorFormData {
  indicatorName: string;
  indicatorCode: string;
  indicatorDesc?: string;
  collectorID?: number;
  collectorItemName: string;
  schedulerID?: number;
  lastMinutes: number;
  thresholdType: string;
  thresholdField: string;
  thresholdComparison: string;
  thresholdValue: number;
  priority: number;
  ownerContactId: number;
  averageLastDays?: number;
  isActive: boolean;
}

// Validation schema using the new validation library
const indicatorValidationSchema = yup.object({
  indicatorName: DomainValidators.indicatorName(),
  indicatorCode: DomainValidators.indicatorCode(),
  indicatorDesc: DomainValidators.description(false),
  collectorID: BaseValidators.optionalNumber(),
  collectorItemName: BaseValidators.requiredString('Collector Item Name'),
  schedulerID: BaseValidators.optionalNumber(),
  lastMinutes: DomainValidators.lastMinutes(),
  thresholdType: BaseValidators.requiredString('Threshold Type'),
  thresholdField: BaseValidators.requiredString('Threshold Field'),
  thresholdComparison: BaseValidators.requiredString('Threshold Comparison'),
  thresholdValue: DomainValidators.thresholdValue(),
  priority: DomainValidators.priority(),
  ownerContactId: BaseValidators.requiredNumber('Owner Contact', 1),
  averageLastDays: BaseValidators.optionalNumber(1, 365),
  isActive: BaseValidators.requiredBoolean('Active Status'),
});

// Form fields component - now using smaller sections

const IndicatorFormFields: React.FC = () => {
  return (
    <>
      <BasicInformationFormSection />
      <DataSourceFormSection />
      <SchedulingFormSection />
      <ThresholdFormSection />
      <PriorityOwnershipFormSection />
    </>
  );
};

// Create Indicator Dialog
interface CreateIndicatorDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateIndicatorRequest) => void | Promise<void>;
  loading?: boolean;
}

export const CreateIndicatorDialog: React.FC<CreateIndicatorDialogProps> = ({
  open,
  onClose,
  onSubmit,
  loading = false,
}) => {
  const defaultValues: IndicatorFormData = {
    indicatorName: '',
    indicatorCode: '',
    indicatorDesc: '',
    collectorID: undefined,
    collectorItemName: '',
    schedulerID: undefined,
    lastMinutes: 60,
    thresholdType: 'count',
    thresholdField: '',
    thresholdComparison: 'gt',
    thresholdValue: 0,
    priority: 3,
    ownerContactId: 0,
    averageLastDays: undefined,
    isActive: true,
  };

  return (
    <CreateFormDialog
      entityName="Indicator"
      open={open}
      onClose={onClose}
      onSubmit={onSubmit}
      loading={loading}
      validationSchema={indicatorValidationSchema}
      defaultValues={defaultValues}
      maxWidth="lg"
      icon={<IndicatorIcon />}
      subtitle="Configure a new monitoring indicator"
    >
      <IndicatorFormFields />
    </CreateFormDialog>
  );
};

// Edit Indicator Dialog
interface EditIndicatorDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: UpdateIndicatorRequest) => void | Promise<void>;
  loading?: boolean;
  initialData: IndicatorFormData;
}

export const EditIndicatorDialog: React.FC<EditIndicatorDialogProps> = ({
  open,
  onClose,
  onSubmit,
  loading = false,
  initialData,
}) => {
  return (
    <EditFormDialog
      entityName="Indicator"
      open={open}
      onClose={onClose}
      onSubmit={onSubmit}
      loading={loading}
      validationSchema={indicatorValidationSchema}
      defaultValues={initialData}
      maxWidth="lg"
      icon={<IndicatorIcon />}
      subtitle="Modify indicator configuration"
    >
      <IndicatorFormFields />
    </EditFormDialog>
  );
};

// Export IndicatorForm as an alias for IndicatorFormFields for compatibility
export const IndicatorForm = IndicatorFormFields;

export default {
  CreateIndicatorDialog,
  EditIndicatorDialog,
  IndicatorFormFields,
  IndicatorForm,
};
