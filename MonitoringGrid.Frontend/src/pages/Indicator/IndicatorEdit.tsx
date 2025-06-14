import React from 'react';
import { useParams } from 'react-router-dom';
import IndicatorEditSimple from './IndicatorEditSimple';

/**
 * IndicatorEdit component - uses the simple edit form without wizard
 */
const IndicatorEdit: React.FC = () => {
  const { id } = useParams<{ id: string }>();

  // Use the simple edit form for better UX
  return <IndicatorEditSimple />;
};

export default IndicatorEdit;
