import { forwardRef, InputHTMLAttributes, ReactNode, useState } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { clsx } from 'clsx'

import { Icon, IconName } from './Icon'

const inputVariants = cva(
  'flex w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm ring-offset-white file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-gray-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:border-gray-600 dark:bg-gray-800 dark:ring-offset-gray-900 dark:placeholder:text-gray-400 dark:focus-visible:ring-brand-400',
  {
    variants: {
      variant: {
        default: '',
        error: 'border-error-500 focus-visible:ring-error-500 dark:border-error-400 dark:focus-visible:ring-error-400',
        success: 'border-success-500 focus-visible:ring-success-500 dark:border-success-400 dark:focus-visible:ring-success-400',
      },
      size: {
        default: 'h-10 px-3 py-2',
        sm: 'h-9 px-3 py-2 text-xs',
        lg: 'h-11 px-4 py-2',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
)

export interface InputProps
  extends InputHTMLAttributes<HTMLInputElement>,
    VariantProps<typeof inputVariants> {
  label?: string
  description?: string
  error?: string
  leftIcon?: IconName
  rightIcon?: IconName
  leftElement?: ReactNode
  rightElement?: ReactNode
  containerClassName?: string
}

const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ 
    className, 
    variant, 
    size, 
    type = 'text',
    label,
    description,
    error,
    leftIcon,
    rightIcon,
    leftElement,
    rightElement,
    containerClassName,
    id,
    ...props 
  }, ref) => {
    const inputId = id || `input-${Math.random().toString(36).substr(2, 9)}`
    const hasError = !!error
    const inputVariant = hasError ? 'error' : variant

    return (
      <div className={clsx('space-y-2', containerClassName)}>
        {label && (
          <label 
            htmlFor={inputId}
            className="block text-sm font-medium text-gray-700 dark:text-gray-300"
          >
            {label}
          </label>
        )}
        
        <div className="relative">
          {/* Left icon or element */}
          {(leftIcon || leftElement) && (
            <div className="absolute inset-y-0 left-0 flex items-center pl-3">
              {leftIcon ? (
                <Icon 
                  name={leftIcon} 
                  className="h-4 w-4 text-gray-400 dark:text-gray-500" 
                />
              ) : (
                leftElement
              )}
            </div>
          )}

          <input
            type={type}
            className={clsx(
              inputVariants({ variant: inputVariant, size, className }),
              {
                'pl-10': leftIcon || leftElement,
                'pr-10': rightIcon || rightElement,
              }
            )}
            ref={ref}
            id={inputId}
            aria-invalid={hasError}
            aria-describedby={
              description || error 
                ? `${inputId}-description ${inputId}-error`.trim()
                : undefined
            }
            {...props}
          />

          {/* Right icon or element */}
          {(rightIcon || rightElement) && (
            <div className="absolute inset-y-0 right-0 flex items-center pr-3">
              {rightIcon ? (
                <Icon 
                  name={rightIcon} 
                  className="h-4 w-4 text-gray-400 dark:text-gray-500" 
                />
              ) : (
                rightElement
              )}
            </div>
          )}
        </div>

        {/* Description */}
        {description && !error && (
          <p 
            id={`${inputId}-description`}
            className="text-sm text-gray-500 dark:text-gray-400"
          >
            {description}
          </p>
        )}

        {/* Error message */}
        {error && (
          <p 
            id={`${inputId}-error`}
            className="text-sm text-error-600 dark:text-error-400"
            role="alert"
          >
            {error}
          </p>
        )}
      </div>
    )
  }
)

Input.displayName = 'Input'

export { Input, inputVariants }

// Specialized input components
export const SearchInput = forwardRef<HTMLInputElement, Omit<InputProps, 'leftIcon' | 'type'>>(
  ({ placeholder = 'Search...', ...props }, ref) => (
    <Input
      ref={ref}
      type="search"
      leftIcon="search"
      placeholder={placeholder}
      {...props}
    />
  )
)

SearchInput.displayName = 'SearchInput'

export const PasswordInput = forwardRef<HTMLInputElement, Omit<InputProps, 'type' | 'rightIcon'>>(
  ({ ...props }, ref) => {
    const [showPassword, setShowPassword] = useState(false)

    return (
      <Input
        ref={ref}
        type={showPassword ? 'text' : 'password'}
        rightElement={
          <button
            type="button"
            onClick={() => setShowPassword(!showPassword)}
            className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
            tabIndex={-1}
          >
            <Icon name={showPassword ? 'eye-off' : 'eye'} className="h-4 w-4" />
          </button>
        }
        {...props}
      />
    )
  }
)

PasswordInput.displayName = 'PasswordInput'

export const EmailInput = forwardRef<HTMLInputElement, Omit<InputProps, 'type' | 'leftIcon'>>(
  ({ ...props }, ref) => (
    <Input
      ref={ref}
      type="email"
      leftIcon="mail"
      {...props}
    />
  )
)

EmailInput.displayName = 'EmailInput'

export const NumberInput = forwardRef<HTMLInputElement, Omit<InputProps, 'type'>>(
  ({ ...props }, ref) => (
    <Input
      ref={ref}
      type="number"
      {...props}
    />
  )
)

NumberInput.displayName = 'NumberInput'
