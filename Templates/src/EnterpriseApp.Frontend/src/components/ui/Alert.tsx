import React, { forwardRef, HTMLAttributes, ReactNode } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { clsx } from 'clsx'

import { Icon, IconName } from './Icon'
import { Button } from './Button'

const alertVariants = cva(
  'relative w-full rounded-lg border p-4',
  {
    variants: {
      variant: {
        default: 'bg-white border-gray-200 text-gray-900 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100',
        info: 'bg-blue-50 border-blue-200 text-blue-900 dark:bg-blue-950 dark:border-blue-800 dark:text-blue-100',
        success: 'bg-success-50 border-success-200 text-success-900 dark:bg-success-950 dark:border-success-800 dark:text-success-100',
        warning: 'bg-warning-50 border-warning-200 text-warning-900 dark:bg-warning-950 dark:border-warning-800 dark:text-warning-100',
        error: 'bg-error-50 border-error-200 text-error-900 dark:bg-error-950 dark:border-error-800 dark:text-error-100',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  }
)

export interface AlertProps
  extends HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof alertVariants> {
  title?: string
  description?: string
  icon?: IconName | ReactNode
  dismissible?: boolean
  onDismiss?: () => void
  actions?: ReactNode
}

const Alert = forwardRef<HTMLDivElement, AlertProps>(
  ({ 
    className, 
    variant, 
    title, 
    description, 
    icon, 
    dismissible, 
    onDismiss, 
    actions,
    children, 
    ...props 
  }, ref) => {
    const defaultIcons: Record<string, IconName> = {
      info: 'info',
      success: 'check-circle',
      warning: 'alert-triangle',
      error: 'alert-circle',
    }

    const iconToShow = icon || (variant && variant !== 'default' ? defaultIcons[variant] : undefined)

    return (
      <div
        ref={ref}
        role="alert"
        className={clsx(alertVariants({ variant }), className)}
        {...props}
      >
        <div className="flex">
          {iconToShow && (
            <div className="flex-shrink-0">
              {typeof iconToShow === 'string' ? (
                <Icon 
                  name={iconToShow} 
                  className={clsx(
                    'h-5 w-5',
                    variant === 'info' && 'text-blue-600 dark:text-blue-400',
                    variant === 'success' && 'text-success-600 dark:text-success-400',
                    variant === 'warning' && 'text-warning-600 dark:text-warning-400',
                    variant === 'error' && 'text-error-600 dark:text-error-400',
                    variant === 'default' && 'text-gray-600 dark:text-gray-400'
                  )}
                />
              ) : (
                iconToShow
              )}
            </div>
          )}
          
          <div className={clsx('flex-1', iconToShow && 'ml-3')}>
            {title && (
              <h3 className={clsx(
                'text-sm font-medium',
                variant === 'info' && 'text-blue-800 dark:text-blue-200',
                variant === 'success' && 'text-success-800 dark:text-success-200',
                variant === 'warning' && 'text-warning-800 dark:text-warning-200',
                variant === 'error' && 'text-error-800 dark:text-error-200',
                variant === 'default' && 'text-gray-800 dark:text-gray-200'
              )}>
                {title}
              </h3>
            )}
            
            {(description || children) && (
              <div className={clsx(
                'text-sm',
                title && 'mt-2',
                variant === 'info' && 'text-blue-700 dark:text-blue-300',
                variant === 'success' && 'text-success-700 dark:text-success-300',
                variant === 'warning' && 'text-warning-700 dark:text-warning-300',
                variant === 'error' && 'text-error-700 dark:text-error-300',
                variant === 'default' && 'text-gray-700 dark:text-gray-300'
              )}>
                {description || children}
              </div>
            )}

            {actions && (
              <div className="mt-4">
                {actions}
              </div>
            )}
          </div>

          {dismissible && (
            <div className="ml-auto pl-3">
              <div className="-mx-1.5 -my-1.5">
                <Button
                  variant="ghost"
                  size="icon-sm"
                  onClick={onDismiss}
                  className={clsx(
                    'rounded-md p-1.5 focus:ring-2 focus:ring-offset-2',
                    variant === 'info' && 'text-blue-500 hover:bg-blue-100 focus:ring-blue-600 focus:ring-offset-blue-50 dark:text-blue-400 dark:hover:bg-blue-900',
                    variant === 'success' && 'text-success-500 hover:bg-success-100 focus:ring-success-600 focus:ring-offset-success-50 dark:text-success-400 dark:hover:bg-success-900',
                    variant === 'warning' && 'text-warning-500 hover:bg-warning-100 focus:ring-warning-600 focus:ring-offset-warning-50 dark:text-warning-400 dark:hover:bg-warning-900',
                    variant === 'error' && 'text-error-500 hover:bg-error-100 focus:ring-error-600 focus:ring-offset-error-50 dark:text-error-400 dark:hover:bg-error-900',
                    variant === 'default' && 'text-gray-500 hover:bg-gray-100 focus:ring-gray-600 focus:ring-offset-gray-50 dark:text-gray-400 dark:hover:bg-gray-800'
                  )}
                >
                  <span className="sr-only">Dismiss</span>
                  <Icon name="x" className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>
    )
  }
)

Alert.displayName = 'Alert'

export { Alert, alertVariants }

// Alert Title component
const AlertTitle = forwardRef<
  HTMLParagraphElement,
  HTMLAttributes<HTMLHeadingElement>
>(({ className, ...props }, ref) => (
  <h5
    ref={ref}
    className={clsx('mb-1 font-medium leading-none tracking-tight', className)}
    {...props}
  />
))
AlertTitle.displayName = 'AlertTitle'

// Alert Description component
const AlertDescription = forwardRef<
  HTMLParagraphElement,
  HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={clsx('text-sm [&_p]:leading-relaxed', className)}
    {...props}
  />
))
AlertDescription.displayName = 'AlertDescription'

export { AlertTitle, AlertDescription }

// Specialized alert components
export interface NotificationAlertProps extends Omit<AlertProps, 'variant'> {
  type: 'info' | 'success' | 'warning' | 'error'
  autoClose?: boolean
  duration?: number
}

export function NotificationAlert({ 
  type, 
  autoClose = false, 
  duration = 5000, 
  onDismiss,
  ...props 
}: NotificationAlertProps) {
  // Auto-close functionality
  React.useEffect(() => {
    if (autoClose && onDismiss) {
      const timer = setTimeout(onDismiss, duration)
      return () => clearTimeout(timer)
    }
  }, [autoClose, duration, onDismiss])

  return (
    <Alert
      variant={type}
      dismissible={!!onDismiss}
      onDismiss={onDismiss}
      {...props}
    />
  )
}

// Banner Alert - for full-width alerts
export interface BannerAlertProps extends AlertProps {
  centered?: boolean
}

export function BannerAlert({ centered = false, className, ...props }: BannerAlertProps) {
  return (
    <Alert
      className={clsx(
        'rounded-none border-x-0 border-t-0',
        centered && 'text-center',
        className
      )}
      {...props}
    />
  )
}

// Inline Alert - for form validation
export interface InlineAlertProps extends Omit<AlertProps, 'variant'> {
  type: 'error' | 'warning' | 'success'
  compact?: boolean
}

export function InlineAlert({ type, compact = false, className, ...props }: InlineAlertProps) {
  return (
    <Alert
      variant={type}
      className={clsx(
        compact && 'p-2 text-xs',
        className
      )}
      {...props}
    />
  )
}
