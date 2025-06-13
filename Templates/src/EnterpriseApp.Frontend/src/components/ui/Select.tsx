import { Fragment, ReactNode } from 'react'
import { Listbox, Transition } from '@headlessui/react'
import { CheckIcon, ChevronUpDownIcon } from '@heroicons/react/20/solid'
import { clsx } from 'clsx'

import { Icon, IconName } from './Icon'

export interface SelectOption<T = any> {
  value: T
  label: string
  description?: string
  icon?: IconName | ReactNode
  disabled?: boolean
}

export interface SelectProps<T = any> {
  value?: T
  onChange: (value: T) => void
  options: SelectOption<T>[]
  placeholder?: string
  label?: string
  description?: string
  error?: string
  disabled?: boolean
  multiple?: boolean
  searchable?: boolean
  clearable?: boolean
  loading?: boolean
  className?: string
  size?: 'sm' | 'default' | 'lg'
}

export function Select<T = any>({
  value,
  onChange,
  options,
  placeholder = 'Select an option...',
  label,
  description,
  error,
  disabled = false,
  multiple = false,
  searchable = false,
  clearable = false,
  loading = false,
  className,
  size = 'default',
}: SelectProps<T>) {
  const selectedOption = multiple 
    ? options.filter(option => Array.isArray(value) && value.includes(option.value))
    : options.find(option => option.value === value)

  const sizeClasses = {
    sm: 'py-1.5 pl-3 pr-8 text-sm',
    default: 'py-2 pl-3 pr-10 text-sm',
    lg: 'py-2.5 pl-4 pr-10 text-base',
  }

  const displayValue = () => {
    if (multiple && Array.isArray(selectedOption)) {
      if (selectedOption.length === 0) return placeholder
      if (selectedOption.length === 1) return selectedOption[0].label
      return `${selectedOption.length} selected`
    }
    return selectedOption?.label || placeholder
  }

  return (
    <div className={clsx('space-y-2', className)}>
      {label && (
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
          {label}
        </label>
      )}

      <Listbox
        value={value}
        onChange={onChange}
        disabled={disabled || loading}
        multiple={multiple}
      >
        <div className="relative">
          <Listbox.Button
            className={clsx(
              'relative w-full cursor-default rounded-md border border-gray-300 bg-white text-left shadow-sm focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 dark:border-gray-600 dark:bg-gray-800 dark:text-white',
              sizeClasses[size],
              error && 'border-error-500 focus:border-error-500 focus:ring-error-500',
              disabled && 'cursor-not-allowed opacity-50',
              loading && 'cursor-wait'
            )}
          >
            <span className={clsx(
              'block truncate',
              !selectedOption && 'text-gray-500 dark:text-gray-400'
            )}>
              {displayValue()}
            </span>
            <span className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-2">
              {loading ? (
                <Icon name="refresh" className="h-4 w-4 animate-spin text-gray-400" />
              ) : (
                <ChevronUpDownIcon className="h-5 w-5 text-gray-400" aria-hidden="true" />
              )}
            </span>
          </Listbox.Button>

          <Transition
            as={Fragment}
            leave="transition ease-in duration-100"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <Listbox.Options className="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md bg-white py-1 text-base shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none dark:bg-gray-800 dark:ring-gray-700 sm:text-sm">
              {clearable && value && (
                <Listbox.Option
                  value={multiple ? [] : null}
                  className={({ active }) =>
                    clsx(
                      'relative cursor-default select-none py-2 pl-3 pr-9',
                      active ? 'bg-brand-600 text-white' : 'text-gray-900 dark:text-gray-100'
                    )
                  }
                >
                  <span className="block truncate font-normal italic">
                    Clear selection
                  </span>
                </Listbox.Option>
              )}

              {options.map((option, optionIdx) => (
                <Listbox.Option
                  key={optionIdx}
                  value={option.value}
                  disabled={option.disabled}
                  className={({ active, disabled }) =>
                    clsx(
                      'relative cursor-default select-none py-2 pl-3 pr-9',
                      active && !disabled ? 'bg-brand-600 text-white' : 'text-gray-900 dark:text-gray-100',
                      disabled && 'cursor-not-allowed opacity-50'
                    )
                  }
                >
                  {({ selected, active }) => (
                    <>
                      <div className="flex items-center">
                        {option.icon && (
                          <span className="mr-3 flex-shrink-0">
                            {typeof option.icon === 'string' ? (
                              <Icon name={option.icon} className="h-4 w-4" />
                            ) : (
                              option.icon
                            )}
                          </span>
                        )}
                        <div className="flex-1">
                          <span
                            className={clsx(
                              'block truncate',
                              selected ? 'font-medium' : 'font-normal'
                            )}
                          >
                            {option.label}
                          </span>
                          {option.description && (
                            <span className={clsx(
                              'block truncate text-xs',
                              active ? 'text-brand-200' : 'text-gray-500 dark:text-gray-400'
                            )}>
                              {option.description}
                            </span>
                          )}
                        </div>
                      </div>

                      {selected && (
                        <span
                          className={clsx(
                            'absolute inset-y-0 right-0 flex items-center pr-4',
                            active ? 'text-white' : 'text-brand-600'
                          )}
                        >
                          <CheckIcon className="h-5 w-5" aria-hidden="true" />
                        </span>
                      )}
                    </>
                  )}
                </Listbox.Option>
              ))}

              {options.length === 0 && (
                <div className="relative cursor-default select-none py-2 pl-3 pr-9 text-gray-500 dark:text-gray-400">
                  No options available
                </div>
              )}
            </Listbox.Options>
          </Transition>
        </div>
      </Listbox>

      {description && !error && (
        <p className="text-sm text-gray-500 dark:text-gray-400">
          {description}
        </p>
      )}

      {error && (
        <p className="text-sm text-error-600 dark:text-error-400" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}

// Simple select for basic use cases
export interface SimpleSelectProps extends Omit<React.SelectHTMLAttributes<HTMLSelectElement>, 'onChange'> {
  label?: string
  description?: string
  error?: string
  options: { value: string | number; label: string }[]
  onChange: (value: string) => void
  containerClassName?: string
}

export function SimpleSelect({
  label,
  description,
  error,
  options,
  onChange,
  className,
  containerClassName,
  ...props
}: SimpleSelectProps) {
  return (
    <div className={clsx('space-y-2', containerClassName)}>
      {label && (
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
          {label}
        </label>
      )}

      <select
        className={clsx(
          'block w-full rounded-md border border-gray-300 bg-white py-2 pl-3 pr-10 text-sm shadow-sm focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 dark:border-gray-600 dark:bg-gray-800 dark:text-white',
          error && 'border-error-500 focus:border-error-500 focus:ring-error-500',
          className
        )}
        onChange={(e) => onChange(e.target.value)}
        {...props}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>

      {description && !error && (
        <p className="text-sm text-gray-500 dark:text-gray-400">
          {description}
        </p>
      )}

      {error && (
        <p className="text-sm text-error-600 dark:text-error-400" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
