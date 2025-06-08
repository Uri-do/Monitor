import React from 'react';
import {
  TextField,
  FormControl,
  FormLabel,
  FormHelperText,
  Select,
  MenuItem,
  Checkbox,
  FormControlLabel,
  Switch,
  Autocomplete,
  Chip,
  Box,
  TextFieldProps,
  SelectProps,
  AutocompleteProps,
} from '@mui/material';
import { Controller, Control, FieldError } from 'react-hook-form';

export interface FormFieldOption {
  value: any;
  label: string;
  disabled?: boolean;
}

interface BaseFormFieldProps {
  name: string;
  control: Control<any>;
  label: string;
  error?: FieldError;
  helperText?: string;
  required?: boolean;
  disabled?: boolean;
  fullWidth?: boolean;
}

interface TextFormFieldProps extends BaseFormFieldProps {
  type: 'text' | 'email' | 'password' | 'number' | 'textarea';
  multiline?: boolean;
  rows?: number;
  placeholder?: string;
  textFieldProps?: Partial<TextFieldProps>;
}

interface SelectFormFieldProps extends BaseFormFieldProps {
  type: 'select';
  options: FormFieldOption[];
  multiple?: boolean;
  selectProps?: Partial<SelectProps>;
}

interface AutocompleteFormFieldProps extends BaseFormFieldProps {
  type: 'autocomplete';
  options: FormFieldOption[];
  multiple?: boolean;
  freeSolo?: boolean;
  autocompleteProps?: Partial<AutocompleteProps<FormFieldOption, boolean, boolean, boolean>>;
}

interface CheckboxFormFieldProps extends BaseFormFieldProps {
  type: 'checkbox' | 'switch';
  checkboxLabel?: string;
}

type FormFieldProps = 
  | TextFormFieldProps 
  | SelectFormFieldProps 
  | AutocompleteFormFieldProps 
  | CheckboxFormFieldProps;

const FormField: React.FC<FormFieldProps> = (props) => {
  const { name, control, label, error, helperText, required, disabled, fullWidth = true } = props;

  const renderTextField = (fieldProps: TextFormFieldProps) => (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <TextField
          {...field}
          label={label}
          type={fieldProps.type === 'textarea' ? 'text' : fieldProps.type}
          multiline={fieldProps.multiline || fieldProps.type === 'textarea'}
          rows={fieldProps.rows || (fieldProps.type === 'textarea' ? 4 : undefined)}
          placeholder={fieldProps.placeholder}
          error={!!error}
          helperText={error?.message || helperText}
          required={required}
          disabled={disabled}
          fullWidth={fullWidth}
          {...fieldProps.textFieldProps}
        />
      )}
    />
  );

  const renderSelect = (fieldProps: SelectFormFieldProps) => (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControl fullWidth={fullWidth} error={!!error} disabled={disabled}>
          <FormLabel required={required}>{label}</FormLabel>
          <Select
            {...field}
            multiple={fieldProps.multiple}
            renderValue={fieldProps.multiple ? (selected) => (
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                {(selected as any[]).map((value) => {
                  const option = fieldProps.options.find(opt => opt.value === value);
                  return (
                    <Chip key={value} label={option?.label || value} size="small" />
                  );
                })}
              </Box>
            ) : undefined}
            {...fieldProps.selectProps}
          >
            {fieldProps.options.map((option) => (
              <MenuItem 
                key={option.value} 
                value={option.value}
                disabled={option.disabled}
              >
                {option.label}
              </MenuItem>
            ))}
          </Select>
          {(error?.message || helperText) && (
            <FormHelperText>{error?.message || helperText}</FormHelperText>
          )}
        </FormControl>
      )}
    />
  );

  const renderAutocomplete = (fieldProps: AutocompleteFormFieldProps) => (
    <Controller
      name={name}
      control={control}
      render={({ field: { onChange, value, ...field } }) => (
        <Autocomplete
          {...field}
          options={fieldProps.options}
          getOptionLabel={(option) => typeof option === 'string' ? option : option.label || ''}
          isOptionEqualToValue={(option, value) => option.value === value?.value}
          multiple={fieldProps.multiple}
          freeSolo={fieldProps.freeSolo}
          value={value || (fieldProps.multiple ? [] : null)}
          onChange={(_, newValue) => onChange(newValue)}
          disabled={disabled}
          renderInput={(params) => (
            <TextField
              {...params}
              label={label}
              error={!!error}
              helperText={error?.message || helperText}
              required={required}
              fullWidth={fullWidth}
            />
          )}
          renderTags={(value, getTagProps) =>
            value.map((option, index) => (
              <Chip
                variant="outlined"
                label={option.label}
                {...getTagProps({ index })}
                key={option.value}
              />
            ))
          }
          {...fieldProps.autocompleteProps}
        />
      )}
    />
  );

  const renderCheckbox = (fieldProps: CheckboxFormFieldProps) => (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <FormControl error={!!error} disabled={disabled}>
          <FormControlLabel
            control={
              fieldProps.type === 'switch' ? (
                <Switch {...field} checked={field.value || false} />
              ) : (
                <Checkbox {...field} checked={field.value || false} />
              )
            }
            label={fieldProps.checkboxLabel || label}
          />
          {(error?.message || helperText) && (
            <FormHelperText>{error?.message || helperText}</FormHelperText>
          )}
        </FormControl>
      )}
    />
  );

  switch (props.type) {
    case 'text':
    case 'email':
    case 'password':
    case 'number':
    case 'textarea':
      return renderTextField(props);
    case 'select':
      return renderSelect(props);
    case 'autocomplete':
      return renderAutocomplete(props);
    case 'checkbox':
    case 'switch':
      return renderCheckbox(props);
    default:
      return null;
  }
};

export default FormField;
