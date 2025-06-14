import * as yup from 'yup';

// Common validation patterns and messages
export const ValidationMessages = {
  required: (field: string) => `${field} is required`,
  email: 'Please enter a valid email address',
  phone: 'Please enter a valid phone number',
  url: 'Please enter a valid URL',
  minLength: (field: string, min: number) => `${field} must be at least ${min} characters`,
  maxLength: (field: string, max: number) => `${field} must be less than ${max} characters`,
  min: (field: string, min: number) => `${field} must be at least ${min}`,
  max: (field: string, max: number) => `${field} must be less than ${max}`,
  positive: (field: string) => `${field} must be a positive number`,
  integer: (field: string) => `${field} must be a whole number`,
  alphanumeric: (field: string) => `${field} can only contain letters and numbers`,
  noSpecialChars: (field: string) => `${field} cannot contain special characters`,
  strongPassword:
    'Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character',
  passwordMatch: 'Passwords do not match',
  futureDate: 'Date must be in the future',
  pastDate: 'Date must be in the past',
  businessHours: 'Time must be during business hours (9 AM - 5 PM)',
} as const;

// Regular expressions for common patterns
export const ValidationPatterns = {
  email: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
  phone: /^[\d\s\-\+\(\)]+$/,
  alphanumeric: /^[a-zA-Z0-9]+$/,
  alphanumericWithSpaces: /^[a-zA-Z0-9\s]+$/,
  noSpecialChars: /^[a-zA-Z0-9\s\-_]+$/,
  strongPassword: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
  url: /^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$/,
  ipAddress:
    /^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/,
  macAddress: /^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$/,
  hexColor: /^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/,
  slug: /^[a-z0-9]+(?:-[a-z0-9]+)*$/,
  version: /^\d+\.\d+\.\d+$/,
} as const;

// Base field validators
export const BaseValidators = {
  // Text fields
  requiredString: (fieldName: string) =>
    yup.string().required(ValidationMessages.required(fieldName)),

  optionalString: () => yup.string().nullable().optional(),

  stringWithLength: (fieldName: string, min: number, max: number) =>
    yup
      .string()
      .required(ValidationMessages.required(fieldName))
      .min(min, ValidationMessages.minLength(fieldName, min))
      .max(max, ValidationMessages.maxLength(fieldName, max)),

  // Email validation
  email: (required: boolean = true) => {
    const schema = yup
      .string()
      .email(ValidationMessages.email)
      .matches(ValidationPatterns.email, ValidationMessages.email);

    return required
      ? schema.required(ValidationMessages.required('Email'))
      : schema.nullable().optional();
  },

  // Phone validation
  phone: (required: boolean = false) => {
    const schema = yup.string().matches(ValidationPatterns.phone, ValidationMessages.phone);

    return required
      ? schema.required(ValidationMessages.required('Phone'))
      : schema.nullable().optional();
  },

  // URL validation
  url: (required: boolean = false) => {
    const schema = yup.string().matches(ValidationPatterns.url, ValidationMessages.url);

    return required
      ? schema.required(ValidationMessages.required('URL'))
      : schema.nullable().optional();
  },

  // Number fields
  requiredNumber: (fieldName: string, min?: number, max?: number) => {
    let schema = yup
      .number()
      .required(ValidationMessages.required(fieldName))
      .typeError(`${fieldName} must be a number`);

    if (min !== undefined) {
      schema = schema.min(min, ValidationMessages.min(fieldName, min));
    }

    if (max !== undefined) {
      schema = schema.max(max, ValidationMessages.max(fieldName, max));
    }

    return schema;
  },

  optionalNumber: (min?: number, max?: number) => {
    let schema = yup.number().nullable().optional();

    if (min !== undefined) {
      schema = schema.min(min, ValidationMessages.min('Value', min));
    }

    if (max !== undefined) {
      schema = schema.max(max, ValidationMessages.max('Value', max));
    }

    return schema;
  },

  positiveNumber: (fieldName: string, required: boolean = true) => {
    const schema = yup
      .number()
      .positive(ValidationMessages.positive(fieldName))
      .typeError(`${fieldName} must be a number`);

    return required
      ? schema.required(ValidationMessages.required(fieldName))
      : schema.nullable().optional();
  },

  integer: (fieldName: string, required: boolean = true) => {
    const schema = yup
      .number()
      .integer(ValidationMessages.integer(fieldName))
      .typeError(`${fieldName} must be a number`);

    return required
      ? schema.required(ValidationMessages.required(fieldName))
      : schema.nullable().optional();
  },

  // Boolean fields
  requiredBoolean: (fieldName: string) =>
    yup.boolean().required(ValidationMessages.required(fieldName)),

  optionalBoolean: () => yup.boolean().optional(),

  // Date fields
  requiredDate: (fieldName: string) => yup.date().required(ValidationMessages.required(fieldName)),

  optionalDate: () => yup.date().nullable().optional(),

  futureDate: (fieldName: string) =>
    yup
      .date()
      .required(ValidationMessages.required(fieldName))
      .min(new Date(), ValidationMessages.futureDate),

  pastDate: (fieldName: string) =>
    yup
      .date()
      .required(ValidationMessages.required(fieldName))
      .max(new Date(), ValidationMessages.pastDate),

  // Array fields
  requiredArray: (fieldName: string, minItems?: number) => {
    let schema = yup
      .array()
      .required(ValidationMessages.required(fieldName))
      .min(1, `At least one ${fieldName.toLowerCase()} is required`);

    if (minItems !== undefined && minItems > 1) {
      schema = schema.min(minItems, `At least ${minItems} ${fieldName.toLowerCase()} are required`);
    }

    return schema;
  },

  optionalArray: () => yup.array().optional(),
} as const;

// Domain-specific validators
export const DomainValidators = {
  // User/Contact related
  username: () =>
    BaseValidators.stringWithLength('Username', 3, 50).matches(
      ValidationPatterns.alphanumeric,
      ValidationMessages.alphanumeric('Username')
    ),

  displayName: () => BaseValidators.stringWithLength('Display Name', 2, 100),

  firstName: () => BaseValidators.stringWithLength('First Name', 2, 50),

  lastName: () => BaseValidators.stringWithLength('Last Name', 2, 50),

  // Password validation
  password: (requireStrong: boolean = true) => {
    let schema = BaseValidators.stringWithLength('Password', 8, 128);

    if (requireStrong) {
      schema = schema.matches(ValidationPatterns.strongPassword, ValidationMessages.strongPassword);
    }

    return schema;
  },

  confirmPassword: (passwordField: string = 'password') =>
    yup
      .string()
      .required(ValidationMessages.required('Confirm Password'))
      .oneOf([yup.ref(passwordField)], ValidationMessages.passwordMatch),

  // Business logic validators
  indicatorName: () => BaseValidators.stringWithLength('Indicator Name', 3, 100),

  indicatorCode: () =>
    BaseValidators.stringWithLength('Indicator Code', 2, 50).matches(
      ValidationPatterns.noSpecialChars,
      ValidationMessages.noSpecialChars('Indicator Code')
    ),

  description: (required: boolean = false) =>
    required
      ? BaseValidators.stringWithLength('Description', 10, 500)
      : yup
          .string()
          .max(500, ValidationMessages.maxLength('Description', 500))
          .nullable()
          .optional(),

  priority: () =>
    BaseValidators.requiredNumber('Priority', 1, 4).integer(ValidationMessages.integer('Priority')),

  frequency: () =>
    BaseValidators.positiveNumber('Frequency').integer(ValidationMessages.integer('Frequency')),

  lastMinutes: () =>
    BaseValidators.requiredNumber('Data Window', 1, 10080) // Max 1 week
      .integer(ValidationMessages.integer('Data Window')),

  thresholdValue: () => BaseValidators.requiredNumber('Threshold Value'),

  cooldownMinutes: () =>
    BaseValidators.requiredNumber('Cooldown Period', 0, 1440) // Max 24 hours
      .integer(ValidationMessages.integer('Cooldown Period')),

  // System validators
  cronExpression: () =>
    BaseValidators.requiredString('Cron Expression').matches(
      /^(\*|([0-9]|1[0-9]|2[0-9]|3[0-9]|4[0-9]|5[0-9])|\*\/([0-9]|1[0-9]|2[0-9]|3[0-9]|4[0-9]|5[0-9])) (\*|([0-9]|1[0-9]|2[0-3])|\*\/([0-9]|1[0-9]|2[0-3])) (\*|([1-9]|1[0-9]|2[0-9]|3[0-1])|\*\/([1-9]|1[0-9]|2[0-9]|3[0-1])) (\*|([1-9]|1[0-2])|\*\/([1-9]|1[0-2])) (\*|([0-6])|\*\/([0-6]))$/,
      'Invalid cron expression format'
    ),

  timezone: () =>
    BaseValidators.requiredString('Timezone').oneOf(
      Intl.supportedValuesOf('timeZone'),
      'Invalid timezone'
    ),

  ipAddress: () =>
    BaseValidators.requiredString('IP Address').matches(
      ValidationPatterns.ipAddress,
      'Invalid IP address format'
    ),
} as const;

// Pre-built schemas for common forms
export const CommonSchemas = {
  // Contact form schema
  contact: yup
    .object({
      name: DomainValidators.displayName(),
      email: BaseValidators.email(false),
      phone: BaseValidators.phone(false),
      isActive: BaseValidators.requiredBoolean('Active Status'),
    })
    .test(
      'contact-method',
      'At least one contact method (email or phone) is required',
      function (value) {
        return !!(value.email || value.phone);
      }
    ),

  // User form schema
  user: yup.object({
    username: DomainValidators.username(),
    email: BaseValidators.email(true),
    firstName: DomainValidators.firstName(),
    lastName: DomainValidators.lastName(),
    displayName: DomainValidators.displayName(),
    password: DomainValidators.password(),
    confirmPassword: DomainValidators.confirmPassword(),
    isActive: BaseValidators.requiredBoolean('Active Status'),
  }),

  // Login form schema
  login: yup.object({
    username: BaseValidators.requiredString('Username'),
    password: BaseValidators.requiredString('Password'),
    rememberMe: BaseValidators.optionalBoolean(),
  }),

  // Change password schema
  changePassword: yup.object({
    currentPassword: BaseValidators.requiredString('Current Password'),
    newPassword: DomainValidators.password(),
    confirmPassword: DomainValidators.confirmPassword('newPassword'),
  }),
} as const;

export default {
  ValidationMessages,
  ValidationPatterns,
  BaseValidators,
  DomainValidators,
  CommonSchemas,
};
