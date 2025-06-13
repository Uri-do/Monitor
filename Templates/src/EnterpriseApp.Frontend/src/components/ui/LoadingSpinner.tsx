import { cva, type VariantProps } from 'class-variance-authority'
import { clsx } from 'clsx'

const spinnerVariants = cva(
  'animate-spin rounded-full border-2 border-current border-t-transparent',
  {
    variants: {
      size: {
        xs: 'h-3 w-3',
        sm: 'h-4 w-4',
        default: 'h-6 w-6',
        lg: 'h-8 w-8',
        xl: 'h-12 w-12',
      },
      variant: {
        default: 'text-brand-600 dark:text-brand-400',
        white: 'text-white',
        gray: 'text-gray-600 dark:text-gray-400',
        current: 'text-current',
      },
    },
    defaultVariants: {
      size: 'default',
      variant: 'default',
    },
  }
)

export interface LoadingSpinnerProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof spinnerVariants> {
  label?: string
}

export function LoadingSpinner({ 
  className, 
  size, 
  variant, 
  label = 'Loading...',
  ...props 
}: LoadingSpinnerProps) {
  return (
    <div
      className={clsx(spinnerVariants({ size, variant }), className)}
      role="status"
      aria-label={label}
      {...props}
    >
      <span className="sr-only">{label}</span>
    </div>
  )
}

// Full page loading component
export function PageLoader({ message = 'Loading...' }: { message?: string }) {
  return (
    <div className="flex min-h-[400px] flex-col items-center justify-center space-y-4">
      <LoadingSpinner size="xl" />
      <p className="text-sm text-gray-600 dark:text-gray-400">{message}</p>
    </div>
  )
}

// Overlay loading component
export function LoadingOverlay({ 
  show, 
  message = 'Loading...',
  className 
}: { 
  show: boolean
  message?: string
  className?: string 
}) {
  if (!show) return null

  return (
    <div className={clsx(
      'absolute inset-0 z-50 flex items-center justify-center bg-white/80 backdrop-blur-sm dark:bg-gray-900/80',
      className
    )}>
      <div className="flex flex-col items-center space-y-4">
        <LoadingSpinner size="lg" />
        <p className="text-sm font-medium text-gray-900 dark:text-gray-100">{message}</p>
      </div>
    </div>
  )
}

// Skeleton loading component
export function Skeleton({ 
  className,
  ...props 
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={clsx('animate-pulse rounded-md bg-gray-200 dark:bg-gray-700', className)}
      {...props}
    />
  )
}

// Text skeleton
export function TextSkeleton({ 
  lines = 1,
  className 
}: { 
  lines?: number
  className?: string 
}) {
  return (
    <div className={clsx('space-y-2', className)}>
      {Array.from({ length: lines }).map((_, i) => (
        <Skeleton
          key={i}
          className={clsx(
            'h-4',
            i === lines - 1 && lines > 1 ? 'w-3/4' : 'w-full'
          )}
        />
      ))}
    </div>
  )
}

// Card skeleton
export function CardSkeleton({ className }: { className?: string }) {
  return (
    <div className={clsx('rounded-lg border border-gray-200 p-6 dark:border-gray-700', className)}>
      <div className="space-y-4">
        <Skeleton className="h-6 w-1/3" />
        <TextSkeleton lines={3} />
        <div className="flex space-x-2">
          <Skeleton className="h-8 w-20" />
          <Skeleton className="h-8 w-20" />
        </div>
      </div>
    </div>
  )
}

// Table skeleton
export function TableSkeleton({ 
  rows = 5,
  columns = 4,
  className 
}: { 
  rows?: number
  columns?: number
  className?: string 
}) {
  return (
    <div className={clsx('space-y-4', className)}>
      {/* Header */}
      <div className="flex space-x-4">
        {Array.from({ length: columns }).map((_, i) => (
          <Skeleton key={i} className="h-6 flex-1" />
        ))}
      </div>
      
      {/* Rows */}
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="flex space-x-4">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <Skeleton key={colIndex} className="h-8 flex-1" />
          ))}
        </div>
      ))}
    </div>
  )
}
