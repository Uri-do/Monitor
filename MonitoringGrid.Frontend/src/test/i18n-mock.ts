import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

// Mock translations
const resources = {
  en: {
    translation: {
      // Common
      'common.save': 'Save',
      'common.cancel': 'Cancel',
      'common.delete': 'Delete',
      'common.edit': 'Edit',
      'common.create': 'Create',
      'common.back': 'Back',
      'common.loading': 'Loading...',
      'common.error': 'Error',
      'common.success': 'Success',
      'common.confirm': 'Confirm',
      'common.yes': 'Yes',
      'common.no': 'No',
      'common.search': 'Search',
      'common.filter': 'Filter',
      'common.clear': 'Clear',
      'common.refresh': 'Refresh',
      'common.export': 'Export',
      'common.import': 'Import',
      'common.settings': 'Settings',
      'common.profile': 'Profile',
      'common.logout': 'Logout',
      'common.login': 'Login',
      'common.register': 'Register',
      'common.dashboard': 'Dashboard',
      'common.administration': 'Administration',
      'common.users': 'Users',
      'common.roles': 'Roles',
      'common.permissions': 'Permissions',
      'common.active': 'Active',
      'common.inactive': 'Inactive',
      'common.enabled': 'Enabled',
      'common.disabled': 'Disabled',
      'common.name': 'Name',
      'common.description': 'Description',
      'common.status': 'Status',
      'common.actions': 'Actions',
      'common.createdAt': 'Created At',
      'common.updatedAt': 'Updated At',
      'common.lastExecuted': 'Last Executed',
      'common.nextExecution': 'Next Execution',

      // Navigation
      'nav.dashboard': 'Dashboard',
      'nav.indicators': 'Indicators',
      'nav.schedulers': 'Schedulers',
      'nav.contacts': 'Contacts',
      'nav.alerts': 'Alerts',
      'nav.analytics': 'Analytics',
      'nav.statistics': 'Statistics',
      'nav.executionHistory': 'Execution History',
      'nav.worker': 'Worker',
      'nav.administration': 'Administration',

      // Indicators
      'indicator.title': 'Indicators',
      'indicator.create': 'Create Indicator',
      'indicator.edit': 'Edit Indicator',
      'indicator.editTitle': 'Edit Indicator',
      'indicator.delete': 'Delete Indicator',
      'indicator.view': 'View Indicator',
      'indicator.list': 'Indicator List',
      'indicator.details': 'Indicator Details',
      'indicator.name': 'Indicator Name',
      'indicator.description': 'Description',
      'indicator.isActive': 'Active',
      'indicator.lastMinutes': 'Last Minutes',
      'indicator.createSuccess': 'Indicator created successfully',
      'indicator.updateSuccess': 'Indicator updated successfully',
      'indicator.deleteSuccess': 'Indicator deleted successfully',
      'indicator.createError': 'Failed to create indicator',
      'indicator.updateError': 'Failed to update indicator',
      'indicator.deleteError': 'Failed to delete indicator',
      'indicator.loadError': 'Failed to load indicator',

      // Users
      'user.title': 'Users',
      'user.create': 'Create User',
      'user.edit': 'Edit User',
      'user.delete': 'Delete User',
      'user.username': 'Username',
      'user.email': 'Email',
      'user.firstName': 'First Name',
      'user.lastName': 'Last Name',
      'user.roles': 'Roles',

      // Alerts
      'alert.title': 'Alerts',
      'alert.severity': 'Severity',
      'alert.message': 'Message',
      'alert.resolved': 'Resolved',
      'alert.unresolved': 'Unresolved',

      // Auth
      'auth.login': 'Login',
      'auth.logout': 'Logout',
      'auth.register': 'Register',
      'auth.username': 'Username',
      'auth.password': 'Password',
      'auth.email': 'Email',
      'auth.loginSuccess': 'Login successful',
      'auth.loginError': 'Login failed',
      'auth.logoutSuccess': 'Logout successful',

      // Validation
      'validation.required': 'This field is required',
      'validation.email': 'Please enter a valid email',
      'validation.minLength': 'Minimum length is {{min}} characters',
      'validation.maxLength': 'Maximum length is {{max}} characters',

      // Errors
      'error.generic': 'An error occurred',
      'error.network': 'Network error',
      'error.unauthorized': 'Unauthorized',
      'error.forbidden': 'Forbidden',
      'error.notFound': 'Not found',
      'error.serverError': 'Server error',
    },
  },
};

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: 'en',
    fallbackLng: 'en',
    debug: false,
    interpolation: {
      escapeValue: false,
    },
  });

export default i18n;
