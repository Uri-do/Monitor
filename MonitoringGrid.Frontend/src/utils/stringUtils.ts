/**
 * String utility functions for safe string operations
 */

/**
 * Safely converts a value to lowercase string
 * @param value - The value to convert
 * @returns Lowercase string or empty string if value is null/undefined
 */
export const safeToLowerCase = (value: string | null | undefined): string => {
  return value?.toString().toLowerCase() || '';
};

/**
 * Safely checks if a string contains a substring (case-insensitive)
 * @param haystack - The string to search in
 * @param needle - The substring to search for
 * @returns True if haystack contains needle (case-insensitive), false otherwise
 */
export const safeIncludes = (haystack: string | null | undefined, needle: string | null | undefined): boolean => {
  if (!haystack || !needle) return false;
  return safeToLowerCase(haystack).includes(safeToLowerCase(needle));
};

/**
 * Safely trims a string
 * @param value - The value to trim
 * @returns Trimmed string or empty string if value is null/undefined
 */
export const safeTrim = (value: string | null | undefined): string => {
  return value?.toString().trim() || '';
};

/**
 * Safely gets string length
 * @param value - The value to get length of
 * @returns Length of string or 0 if value is null/undefined
 */
export const safeLength = (value: string | null | undefined): number => {
  return value?.toString().length || 0;
};

/**
 * Safely capitalizes first letter of a string
 * @param value - The value to capitalize
 * @returns Capitalized string or empty string if value is null/undefined
 */
export const safeCapitalize = (value: string | null | undefined): string => {
  const str = value?.toString();
  if (!str) return '';
  return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
};

/**
 * Safely converts string to title case
 * @param value - The value to convert
 * @returns Title case string or empty string if value is null/undefined
 */
export const safeToTitleCase = (value: string | null | undefined): string => {
  const str = value?.toString();
  if (!str) return '';
  return str.replace(/\w\S*/g, (txt) => 
    txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase()
  );
};

/**
 * Safely truncates a string to specified length
 * @param value - The value to truncate
 * @param maxLength - Maximum length
 * @param suffix - Suffix to add when truncated (default: '...')
 * @returns Truncated string or empty string if value is null/undefined
 */
export const safeTruncate = (
  value: string | null | undefined, 
  maxLength: number, 
  suffix: string = '...'
): string => {
  const str = value?.toString();
  if (!str) return '';
  if (str.length <= maxLength) return str;
  return str.substring(0, maxLength - suffix.length) + suffix;
};

/**
 * Safely checks if a string is empty or whitespace
 * @param value - The value to check
 * @returns True if value is null, undefined, empty, or only whitespace
 */
export const isEmpty = (value: string | null | undefined): boolean => {
  return !value || safeTrim(value).length === 0;
};

/**
 * Safely checks if a string is not empty
 * @param value - The value to check
 * @returns True if value has content (not null, undefined, empty, or only whitespace)
 */
export const isNotEmpty = (value: string | null | undefined): boolean => {
  return !isEmpty(value);
};

/**
 * Multi-field search helper for filtering arrays
 * @param searchTerm - The search term
 * @param fields - Array of field values to search in
 * @returns True if any field contains the search term (case-insensitive)
 */
export const multiFieldSearch = (
  searchTerm: string | null | undefined,
  fields: (string | null | undefined)[]
): boolean => {
  if (!searchTerm) return true;
  return fields.some(field => safeIncludes(field, searchTerm));
};

/**
 * Safe string comparison (case-insensitive)
 * @param a - First string
 * @param b - Second string
 * @returns True if strings are equal (case-insensitive)
 */
export const safeEquals = (a: string | null | undefined, b: string | null | undefined): boolean => {
  return safeToLowerCase(a) === safeToLowerCase(b);
};

/**
 * Safe string starts with check (case-insensitive)
 * @param value - The string to check
 * @param prefix - The prefix to check for
 * @returns True if value starts with prefix (case-insensitive)
 */
export const safeStartsWith = (value: string | null | undefined, prefix: string | null | undefined): boolean => {
  if (!value || !prefix) return false;
  return safeToLowerCase(value).startsWith(safeToLowerCase(prefix));
};

/**
 * Safe string ends with check (case-insensitive)
 * @param value - The string to check
 * @param suffix - The suffix to check for
 * @returns True if value ends with suffix (case-insensitive)
 */
export const safeEndsWith = (value: string | null | undefined, suffix: string | null | undefined): boolean => {
  if (!value || !suffix) return false;
  return safeToLowerCase(value).endsWith(safeToLowerCase(suffix));
};
