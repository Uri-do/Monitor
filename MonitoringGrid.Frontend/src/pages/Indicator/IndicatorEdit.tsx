import React from 'react';
import { useParams } from 'react-router-dom';
import IndicatorCreate from './IndicatorCreate';

/**
 * IndicatorEdit component - wrapper around IndicatorCreate for editing mode
 */
const IndicatorEdit: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  
  // The IndicatorCreate component already handles edit mode when an ID is present
  // This component serves as a dedicated route for editing
  return <IndicatorCreate />;
};

export default IndicatorEdit;
