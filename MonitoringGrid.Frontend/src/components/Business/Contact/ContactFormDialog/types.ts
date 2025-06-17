import * as yup from 'yup';

// Validation schema
export const contactSchema = yup
  .object({
    name: yup.string().required('Contact name is required'),
    email: yup.string().email('Invalid email format').nullable(),
    phone: yup.string().nullable(),
    isActive: yup.boolean().required(),
  })
  .test(
    'contact-method',
    'At least one contact method (email or phone) is required',
    function (value) {
      return !!(value.email || value.phone);
    }
  );

export type ContactFormData = yup.InferType<typeof contactSchema>;
