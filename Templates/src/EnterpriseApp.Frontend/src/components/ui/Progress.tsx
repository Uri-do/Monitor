import { forwardRef, HTMLAttributes } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { clsx } from 'clsx'

const progressVariants = cva(
  'relative overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700',
  {
    variants: {
      size: {
        sm: 'h-1',
        default: 'h-2',
        lg: 'h-3',
        xl: 'h-4',
      },
    },
    defaultVariants: {
      size: 'default',
    },
  }
)

const progressBarVariants = cva(
  'h-full transition-all duration-300 ease-in-out rounded-full',
  {
    variants: {
      variant: {
        default: 'bg-brand-600 dark:bg-brand-400',
        success: 'bg-green-600 dark:bg-green-400',
        warning: 'bg-yellow-600 dark:bg-yellow-400',
        error: 'bg-red-600 dark:bg-red-400',
        info: 'bg-blue-600 dark:bg-blue-400',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  }
)

export interface ProgressProps
  extends HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof progressVariants>,
    VariantProps<typeof progressBarVariants> {
  value: number
  max?: number
  showValue?: boolean
  label?: string
  animated?: boolean
}

const Progress = forwardRef<HTMLDivElement, ProgressProps>(
  ({ 
    className, 
    size, 
    variant, 
    value, 
    max = 100, 
    showValue = false, 
    label, 
    animated = false,
    ...props 
  }, ref) => {
    const percentage = Math.min(Math.max((value / max) * 100, 0), 100)

    return (
      <div className="space-y-2">
        {(label || showValue) && (
          <div className="flex items-center justify-between text-sm">
            {label && (
              <span className="font-medium text-gray-700 dark:text-gray-300">
                {label}
              </span>
            )}
            {showValue && (
              <span className="text-gray-600 dark:text-gray-400">
                {value}{max === 100 ? '%' : `/${max}`}
              </span>
            )}
          </div>
        )}
        
        <div
          ref={ref}
          className={clsx(progressVariants({ size }), className)}
          role="progressbar"
          aria-valuenow={value}
          aria-valuemin={0}
          aria-valuemax={max}
          aria-label={label}
          {...props}
        >
          <div
            className={clsx(
              progressBarVariants({ variant }),
              animated && 'animate-pulse'
            )}
            style={{ width: `${percentage}%` }}
          />
        </div>
      </div>
    )
  }
)

Progress.displayName = 'Progress'

export { Progress, progressVariants, progressBarVariants }

// Circular Progress Component
export interface CircularProgressProps {
  value: number
  max?: number
  size?: number
  strokeWidth?: number
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info'
  showValue?: boolean
  label?: string
  className?: string
}

export function CircularProgress({
  value,
  max = 100,
  size = 120,
  strokeWidth = 8,
  variant = 'default',
  showValue = true,
  label,
  className,
}: CircularProgressProps) {
  const percentage = Math.min(Math.max((value / max) * 100, 0), 100)
  const radius = (size - strokeWidth) / 2
  const circumference = radius * 2 * Math.PI
  const strokeDasharray = circumference
  const strokeDashoffset = circumference - (percentage / 100) * circumference

  const colors = {
    default: 'stroke-brand-600 dark:stroke-brand-400',
    success: 'stroke-green-600 dark:stroke-green-400',
    warning: 'stroke-yellow-600 dark:stroke-yellow-400',
    error: 'stroke-red-600 dark:stroke-red-400',
    info: 'stroke-blue-600 dark:stroke-blue-400',
  }

  return (
    <div className={clsx('relative inline-flex items-center justify-center', className)}>
      <svg
        width={size}
        height={size}
        className="transform -rotate-90"
        role="progressbar"
        aria-valuenow={value}
        aria-valuemin={0}
        aria-valuemax={max}
        aria-label={label}
      >
        {/* Background circle */}
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke="currentColor"
          strokeWidth={strokeWidth}
          fill="none"
          className="text-gray-200 dark:text-gray-700"
        />
        
        {/* Progress circle */}
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          strokeWidth={strokeWidth}
          fill="none"
          strokeDasharray={strokeDasharray}
          strokeDashoffset={strokeDashoffset}
          strokeLinecap="round"
          className={clsx('transition-all duration-300 ease-in-out', colors[variant])}
        />
      </svg>
      
      {/* Center content */}
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        {showValue && (
          <span className="text-lg font-bold text-gray-900 dark:text-white">
            {Math.round(percentage)}%
          </span>
        )}
        {label && (
          <span className="text-xs text-gray-600 dark:text-gray-400 text-center">
            {label}
          </span>
        )}
      </div>
    </div>
  )
}

// Multi-step Progress Component
export interface StepProgressProps {
  steps: Array<{
    id: string
    title: string
    description?: string
    status: 'completed' | 'current' | 'upcoming'
  }>
  className?: string
}

export function StepProgress({ steps, className }: StepProgressProps) {
  return (
    <div className={clsx('space-y-4', className)}>
      {steps.map((step, index) => (
        <div key={step.id} className="flex items-start space-x-3">
          {/* Step indicator */}
          <div className="flex-shrink-0">
            {step.status === 'completed' ? (
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-green-600">
                <svg className="h-5 w-5 text-white" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
              </div>
            ) : step.status === 'current' ? (
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-600">
                <span className="text-sm font-medium text-white">{index + 1}</span>
              </div>
            ) : (
              <div className="flex h-8 w-8 items-center justify-center rounded-full border-2 border-gray-300 dark:border-gray-600">
                <span className="text-sm font-medium text-gray-500 dark:text-gray-400">{index + 1}</span>
              </div>
            )}
          </div>

          {/* Step content */}
          <div className="flex-1 min-w-0">
            <h3 className={clsx(
              'text-sm font-medium',
              step.status === 'completed' && 'text-green-600 dark:text-green-400',
              step.status === 'current' && 'text-brand-600 dark:text-brand-400',
              step.status === 'upcoming' && 'text-gray-500 dark:text-gray-400'
            )}>
              {step.title}
            </h3>
            {step.description && (
              <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                {step.description}
              </p>
            )}
          </div>

          {/* Connecting line */}
          {index < steps.length - 1 && (
            <div className="absolute left-4 mt-8 h-6 w-0.5 bg-gray-300 dark:bg-gray-600" />
          )}
        </div>
      ))}
    </div>
  )
}

// Animated Progress Bar
export interface AnimatedProgressProps extends ProgressProps {
  duration?: number
  delay?: number
}

export function AnimatedProgress({ 
  value, 
  duration = 1000, 
  delay = 0, 
  ...props 
}: AnimatedProgressProps) {
  return (
    <Progress
      {...props}
      value={value}
      style={{
        '--progress-duration': `${duration}ms`,
        '--progress-delay': `${delay}ms`,
      } as React.CSSProperties}
      className={clsx(
        'transition-all ease-out',
        props.className
      )}
    />
  )
}
