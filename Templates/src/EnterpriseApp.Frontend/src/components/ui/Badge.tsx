import { forwardRef, HTMLAttributes, ReactNode } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { clsx } from 'clsx'

import { Icon, IconName } from './Icon'

const badgeVariants = cva(
  'inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
  {
    variants: {
      variant: {
        default: 'border-transparent bg-brand-100 text-brand-800 dark:bg-brand-900 dark:text-brand-200',
        secondary: 'border-transparent bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200',
        success: 'border-transparent bg-success-100 text-success-800 dark:bg-success-900 dark:text-success-200',
        warning: 'border-transparent bg-warning-100 text-warning-800 dark:bg-warning-900 dark:text-warning-200',
        error: 'border-transparent bg-error-100 text-error-800 dark:bg-error-900 dark:text-error-200',
        outline: 'border-gray-300 text-gray-700 dark:border-gray-600 dark:text-gray-300',
        'outline-brand': 'border-brand-300 text-brand-700 dark:border-brand-600 dark:text-brand-300',
        'outline-success': 'border-success-300 text-success-700 dark:border-success-600 dark:text-success-300',
        'outline-warning': 'border-warning-300 text-warning-700 dark:border-warning-600 dark:text-warning-300',
        'outline-error': 'border-error-300 text-error-700 dark:border-error-600 dark:text-error-300',
      },
      size: {
        sm: 'px-2 py-0.5 text-xs',
        default: 'px-2.5 py-0.5 text-xs',
        lg: 'px-3 py-1 text-sm',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
)

export interface BadgeProps
  extends HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {
  icon?: IconName
  rightIcon?: IconName
  dot?: boolean
  removable?: boolean
  onRemove?: () => void
}

const Badge = forwardRef<HTMLDivElement, BadgeProps>(
  ({ 
    className, 
    variant, 
    size, 
    icon, 
    rightIcon, 
    dot, 
    removable, 
    onRemove, 
    children, 
    ...props 
  }, ref) => {
    return (
      <div
        ref={ref}
        className={clsx(badgeVariants({ variant, size }), className)}
        {...props}
      >
        {dot && (
          <span className="h-1.5 w-1.5 rounded-full bg-current" />
        )}
        {icon && (
          <Icon name={icon} className="h-3 w-3" />
        )}
        {children}
        {rightIcon && (
          <Icon name={rightIcon} className="h-3 w-3" />
        )}
        {removable && (
          <button
            type="button"
            onClick={onRemove}
            className="ml-1 inline-flex h-3 w-3 items-center justify-center rounded-full hover:bg-current hover:bg-opacity-20 focus:outline-none focus:ring-1 focus:ring-current"
          >
            <Icon name="x" className="h-2 w-2" />
            <span className="sr-only">Remove</span>
          </button>
        )}
      </div>
    )
  }
)

Badge.displayName = 'Badge'

export { Badge, badgeVariants }

// Status Badge - for displaying status with predefined colors
export interface StatusBadgeProps extends Omit<BadgeProps, 'variant'> {
  status: 'active' | 'inactive' | 'pending' | 'completed' | 'failed' | 'cancelled' | 'draft'
}

export function StatusBadge({ status, ...props }: StatusBadgeProps) {
  const statusConfig = {
    active: { variant: 'success' as const, children: 'Active' },
    inactive: { variant: 'secondary' as const, children: 'Inactive' },
    pending: { variant: 'warning' as const, children: 'Pending' },
    completed: { variant: 'success' as const, children: 'Completed' },
    failed: { variant: 'error' as const, children: 'Failed' },
    cancelled: { variant: 'secondary' as const, children: 'Cancelled' },
    draft: { variant: 'outline' as const, children: 'Draft' },
  }

  const config = statusConfig[status]

  return (
    <Badge variant={config.variant} dot {...props}>
      {config.children}
    </Badge>
  )
}

// Priority Badge - for displaying priority levels
export interface PriorityBadgeProps extends Omit<BadgeProps, 'variant'> {
  priority: 'low' | 'medium' | 'high' | 'critical'
}

export function PriorityBadge({ priority, ...props }: PriorityBadgeProps) {
  const priorityConfig = {
    low: { variant: 'secondary' as const, children: 'Low', icon: 'chevron-down' as IconName },
    medium: { variant: 'warning' as const, children: 'Medium', icon: 'minus' as IconName },
    high: { variant: 'error' as const, children: 'High', icon: 'chevron-up' as IconName },
    critical: { variant: 'error' as const, children: 'Critical', icon: 'alert-triangle' as IconName },
  }

  const config = priorityConfig[priority]

  return (
    <Badge variant={config.variant} icon={config.icon} {...props}>
      {config.children}
    </Badge>
  )
}

// Count Badge - for displaying counts/numbers
export interface CountBadgeProps extends Omit<BadgeProps, 'children'> {
  count: number
  max?: number
  showZero?: boolean
}

export function CountBadge({ count, max, showZero = false, ...props }: CountBadgeProps) {
  if (count === 0 && !showZero) {
    return null
  }

  const displayCount = max && count > max ? `${max}+` : count.toString()

  return (
    <Badge variant="error" size="sm" {...props}>
      {displayCount}
    </Badge>
  )
}

// Tag Badge - for displaying tags with remove functionality
export interface TagBadgeProps extends Omit<BadgeProps, 'removable' | 'onRemove'> {
  tag: string
  onRemove?: (tag: string) => void
  color?: string
}

export function TagBadge({ tag, onRemove, color, ...props }: TagBadgeProps) {
  const style = color ? { backgroundColor: color, color: 'white' } : undefined

  return (
    <Badge
      variant="outline"
      removable={!!onRemove}
      onRemove={() => onRemove?.(tag)}
      style={style}
      {...props}
    >
      {tag}
    </Badge>
  )
}

// Badge Group - for displaying multiple badges
export interface BadgeGroupProps {
  badges: ReactNode[]
  max?: number
  className?: string
}

export function BadgeGroup({ badges, max, className }: BadgeGroupProps) {
  const visibleBadges = max ? badges.slice(0, max) : badges
  const remainingCount = max && badges.length > max ? badges.length - max : 0

  return (
    <div className={clsx('flex flex-wrap items-center gap-1', className)}>
      {visibleBadges}
      {remainingCount > 0 && (
        <Badge variant="secondary" size="sm">
          +{remainingCount}
        </Badge>
      )}
    </div>
  )
}
