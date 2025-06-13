import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import Backend from 'i18next-http-backend';

// Import translation resources
import enCommon from './locales/en/common.json';
import enDashboard from './locales/en/dashboard.json';
import enIndicators from './locales/en/indicators.json';
import enAlerts from './locales/en/alerts.json';
import enSecurity from './locales/en/security.json';
import enValidation from './locales/en/validation.json';

import esCommon from './locales/es/common.json';
import esDashboard from './locales/es/dashboard.json';
import esIndicators from './locales/es/indicators.json';
import esAlerts from './locales/es/alerts.json';
import esSecurity from './locales/es/security.json';
import esValidation from './locales/es/validation.json';

import frCommon from './locales/fr/common.json';
import frDashboard from './locales/fr/dashboard.json';
import frIndicators from './locales/fr/indicators.json';
import frAlerts from './locales/fr/alerts.json';
import frSecurity from './locales/fr/security.json';
import frValidation from './locales/fr/validation.json';

import deCommon from './locales/de/common.json';
import deDashboard from './locales/de/dashboard.json';
import deIndicators from './locales/de/indicators.json';
import deAlerts from './locales/de/alerts.json';
import deSecurity from './locales/de/security.json';
import deValidation from './locales/de/validation.json';

// Supported languages configuration
export const supportedLanguages = [
  { code: 'en', name: 'English', nativeName: 'English', flag: '🇺🇸' },
  { code: 'es', name: 'Spanish', nativeName: 'Español', flag: '🇪🇸' },
  { code: 'fr', name: 'French', nativeName: 'Français', flag: '🇫🇷' },
  { code: 'de', name: 'German', nativeName: 'Deutsch', flag: '🇩🇪' },
  { code: 'pt', name: 'Portuguese', nativeName: 'Português', flag: '🇵🇹' },
  { code: 'it', name: 'Italian', nativeName: 'Italiano', flag: '🇮🇹' },
  { code: 'ja', name: 'Japanese', nativeName: '日本語', flag: '🇯🇵' },
  { code: 'zh', name: 'Chinese', nativeName: '中文', flag: '🇨🇳' },
  { code: 'ko', name: 'Korean', nativeName: '한국어', flag: '🇰🇷' },
  { code: 'ar', name: 'Arabic', nativeName: 'العربية', flag: '🇸🇦', rtl: true },
];

// Translation resources
const resources = {
  en: {
    common: enCommon,
    dashboard: enDashboard,
    indicators: enIndicators,
    alerts: enAlerts,
    security: enSecurity,
    validation: enValidation,
  },
  es: {
    common: esCommon,
    dashboard: esDashboard,
    indicators: esIndicators,
    alerts: esAlerts,
    security: esSecurity,
    validation: esValidation,
  },
  fr: {
    common: frCommon,
    dashboard: frDashboard,
    indicators: frIndicators,
    alerts: frAlerts,
    security: frSecurity,
    validation: frValidation,
  },
  de: {
    common: deCommon,
    dashboard: deDashboard,
    indicators: deIndicators,
    alerts: deAlerts,
    security: deSecurity,
    validation: deValidation,
  },
};

// i18n configuration
i18n
  .use(Backend)
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: 'en',
    debug: process.env.NODE_ENV === 'development',

    // Language detection options
    detection: {
      order: ['localStorage', 'navigator', 'htmlTag'],
      caches: ['localStorage'],
      lookupLocalStorage: 'i18nextLng',
    },

    // Backend options for dynamic loading
    backend: {
      loadPath: '/locales/{{lng}}/{{ns}}.json',
      addPath: '/locales/add/{{lng}}/{{ns}}',
    },

    // Interpolation options
    interpolation: {
      escapeValue: false, // React already escapes values
      format: (value, format, lng) => {
        if (format === 'uppercase') return value.toUpperCase();
        if (format === 'lowercase') return value.toLowerCase();
        if (format === 'currency') {
          return new Intl.NumberFormat(lng, {
            style: 'currency',
            currency: 'USD',
          }).format(value);
        }
        if (format === 'number') {
          return new Intl.NumberFormat(lng).format(value);
        }
        if (format === 'date') {
          return new Intl.DateTimeFormat(lng).format(new Date(value));
        }
        if (format === 'datetime') {
          return new Intl.DateTimeFormat(lng, {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
          }).format(new Date(value));
        }
        if (format === 'relative') {
          const rtf = new Intl.RelativeTimeFormat(lng, { numeric: 'auto' });
          const diff = (new Date(value).getTime() - Date.now()) / 1000;
          const absDiff = Math.abs(diff);

          if (absDiff < 60) return rtf.format(Math.round(diff), 'second');
          if (absDiff < 3600) return rtf.format(Math.round(diff / 60), 'minute');
          if (absDiff < 86400) return rtf.format(Math.round(diff / 3600), 'hour');
          return rtf.format(Math.round(diff / 86400), 'day');
        }
        return value;
      },
    },

    // Namespace configuration
    defaultNS: 'common',
    ns: ['common', 'dashboard', 'indicators', 'alerts', 'security', 'validation'],

    // React options
    react: {
      useSuspense: false,
      bindI18n: 'languageChanged',
      bindI18nStore: '',
      transEmptyNodeValue: '',
      transSupportBasicHtmlNodes: true,
      transKeepBasicHtmlNodesFor: ['br', 'strong', 'i', 'em'],
    },

    // Pluralization
    pluralSeparator: '_',
    contextSeparator: '_',

    // Performance
    load: 'languageOnly',
    preload: ['en'],

    // Error handling
    missingKeyHandler: (lng, ns, key, _fallbackValue) => {
      if (process.env.NODE_ENV === 'development') {
        console.warn(`Missing translation key: ${ns}:${key} for language: ${lng}`);
      }
    },

    // Custom post processor for advanced formatting
    // postProcess: ['interval'], // Disabled for now
  });

// Custom interval post processor for time ranges
// Note: Disabled for now to avoid initialization issues
// i18n.on('initialized', () => {
//   if (i18n.services?.postProcessor?.addPostProcessor) {
//     i18n.services.postProcessor.addPostProcessor('interval', {
//       name: 'interval',
//       type: 'postProcessor',
//       process: (value: string, key: string, options: any) => {
//         if (key.includes('interval')) {
//           const count = options.count || 0;
//           if (count === 1) return value.replace('{{count}}', '1');
//           return value.replace('{{count}}', count.toString());
//         }
//         return value;
//       },
//     });
//   }
// });

// Utility functions
export const getCurrentLanguage = () => i18n.language;
export const getSupportedLanguages = () => supportedLanguages;
export const isRTL = (lng?: string) => {
  const language = lng || getCurrentLanguage();
  return supportedLanguages.find(l => l.code === language)?.rtl || false;
};

export const changeLanguage = async (lng: string) => {
  await i18n.changeLanguage(lng);

  // Update document direction for RTL languages
  document.dir = isRTL(lng) ? 'rtl' : 'ltr';
  document.documentElement.lang = lng;

  // Update theme direction if needed
  const event = new CustomEvent('languageChanged', {
    detail: { language: lng, isRTL: isRTL(lng) },
  });
  window.dispatchEvent(event);
};

// Format utilities with i18n support
export const formatters = {
  currency: (value: number, currency = 'USD', lng?: string) => {
    return new Intl.NumberFormat(lng || getCurrentLanguage(), {
      style: 'currency',
      currency,
    }).format(value);
  },

  number: (value: number, lng?: string) => {
    return new Intl.NumberFormat(lng || getCurrentLanguage()).format(value);
  },

  percentage: (value: number, lng?: string) => {
    return new Intl.NumberFormat(lng || getCurrentLanguage(), {
      style: 'percent',
      minimumFractionDigits: 1,
      maximumFractionDigits: 2,
    }).format(value / 100);
  },

  date: (value: Date | string, lng?: string) => {
    return new Intl.DateTimeFormat(lng || getCurrentLanguage()).format(new Date(value));
  },

  datetime: (value: Date | string, lng?: string) => {
    return new Intl.DateTimeFormat(lng || getCurrentLanguage(), {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  },

  relative: (value: Date | string, lng?: string) => {
    const rtf = new Intl.RelativeTimeFormat(lng || getCurrentLanguage(), { numeric: 'auto' });
    const diff = (new Date(value).getTime() - Date.now()) / 1000;
    const absDiff = Math.abs(diff);

    if (absDiff < 60) return rtf.format(Math.round(diff), 'second');
    if (absDiff < 3600) return rtf.format(Math.round(diff / 60), 'minute');
    if (absDiff < 86400) return rtf.format(Math.round(diff / 3600), 'hour');
    return rtf.format(Math.round(diff / 86400), 'day');
  },
};

export default i18n;
