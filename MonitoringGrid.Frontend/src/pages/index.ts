// Pages Index - Main export for all pages
// Organized by feature area for easy imports

// Authentication Pages
export { default as Login } from './Auth/Login';
export { default as Register } from './Auth/Register';

// Dashboard
export { default as Dashboard } from './Dashboard/Dashboard';

// Indicator Management
export { default as IndicatorList } from './Indicator/IndicatorList';
export { default as IndicatorDetail } from './Indicator/IndicatorDetail';
export { default as IndicatorCreate } from './Indicator/IndicatorCreate';
export { default as IndicatorEdit } from './Indicator/IndicatorEdit';

// Scheduler Management
export { default as SchedulerList } from './Scheduler/SchedulerList';
export { default as SchedulerDetail } from './Scheduler/SchedulerDetail';
export { default as SchedulerCreate } from './Scheduler/SchedulerCreate';
export { default as SchedulerEdit } from './Scheduler/SchedulerEdit';

// Contact Management
export { default as ContactList } from './Contact/ContactList';
export { default as ContactDetail } from './Contact/ContactDetail';
export { default as ContactCreate } from './Contact/ContactCreate';

// Alert Management
export { default as AlertList } from './Alert/AlertList';
export { default as AlertDetail } from './Alert/AlertDetail';
export { default as AlertEdit } from './Alert/AlertEdit';

// Analytics & Statistics
export { default as Analytics } from './Analytics/Analytics';
export { default as StatisticsPage } from './Statistics/StatisticsPage';

// Execution History
export { default as ExecutionHistoryList } from './ExecutionHistory/ExecutionHistoryList';
export { default as ExecutionHistoryDetail } from './ExecutionHistory/ExecutionHistoryDetail';

// User Management
export { default as UserProfile } from './User/UserProfile';
export { default as UserManagement } from './Users/UserManagement';

// Worker Management
export { default as WorkerManagement } from './Worker/WorkerManagement';

// Settings & Administration
export { default as Settings } from './Settings/Settings';
export { default as SecuritySettings } from './Settings/SecuritySettings';
export { default as Administration } from './Admin/Administration';
export { default as RoleManagement } from './Admin/RoleManagement';
export { default as SystemSettings } from './Admin/SystemSettings';
export { default as ApiKeyManagement } from './Admin/ApiKeyManagement';
