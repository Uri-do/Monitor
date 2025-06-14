import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Box, Typography, Button, Paper } from '@mui/material';
import { ArrowBack, Edit } from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { toast } from 'react-hot-toast';

import { useIndicator, useUpdateIndicator } from '@/hooks/useIndicators';
import { IndicatorForm } from '@/components/Business/Indicator/IndicatorForm';
import { LoadingSpinner } from '@/components/UI/LoadingSpinner';
import { PageHeader } from '@/components/UI/PageHeader';
import { FormLayout } from '@/components/UI/FormLayout';

/**
 * IndicatorEdit component without wizard/stepper
 */
const IndicatorEdit: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();

  const {
    data: indicator,
    isLoading,
    error,
  } = useIndicator(id ? parseInt(id, 10) : 0, {
    enabled: !!id,
  });

  const updateMutation = useUpdateIndicator();

  const handleSubmit = async (data: any) => {
    if (!id) return;

    try {
      await updateMutation.mutateAsync({
        id: parseInt(id, 10),
        ...data,
      });
      toast.success(t('indicator.updateSuccess'));
      navigate(`/indicators/${id}`);
    } catch (error) {
      toast.error(t('indicator.updateError'));
    }
  };

  const handleCancel = () => {
    navigate(`/indicators/${id}`);
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (error || !indicator) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography color="error">
          {t('indicator.loadError')}
        </Typography>
      </Box>
    );
  }

  return (
    <FormLayout>
      <PageHeader
        title={t('indicator.editTitle')}
        subtitle={indicator.name}
        icon={<Edit />}
        actions={
          <Button
            variant="outlined"
            startIcon={<ArrowBack />}
            onClick={handleCancel}
          >
            {t('common.back')}
          </Button>
        }
      />

      <Paper sx={{ p: 3 }}>
        <IndicatorForm
          initialData={indicator}
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isLoading={updateMutation.isPending}
          mode="edit"
        />
      </Paper>
    </FormLayout>
  );
};

export default IndicatorEdit;
