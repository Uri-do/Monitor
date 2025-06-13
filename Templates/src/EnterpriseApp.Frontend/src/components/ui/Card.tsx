import { forwardRef, HTMLAttributes, ReactNode } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { clsx } from 'clsx'

const cardVariants = cva(
  'rounded-lg border bg-white text-gray-950 shadow-sm dark:border-gray-700 dark:bg-gray-800 dark:text-gray-50',
  {
    variants: {
      variant: {
        default: 'border-gray-200',
        outline: 'border-gray-300',
        ghost: 'border-transparent shadow-none',
        elevated: 'border-gray-200 shadow-md',
      },
      padding: {
        none: '',
        sm: 'p-4',
        default: 'p-6',
        lg: 'p-8',
      },
    },
    defaultVariants: {
      variant: 'default',
      padding: 'default',
    },
  }
)

export interface CardProps
  extends HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof cardVariants> {}

const Card = forwardRef<HTMLDivElement, CardProps>(
  ({ className, variant, padding, ...props }, ref) => (
    <div
      ref={ref}
      className={clsx(cardVariants({ variant, padding }), className)}
      {...props}
    />
  )
)
Card.displayName = 'Card'

const CardHeader = forwardRef<
  HTMLDivElement,
  HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={clsx('flex flex-col space-y-1.5 p-6', className)}
    {...props}
  />
))
CardHeader.displayName = 'CardHeader'

const CardTitle = forwardRef<
  HTMLParagraphElement,
  HTMLAttributes<HTMLHeadingElement>
>(({ className, ...props }, ref) => (
  <h3
    ref={ref}
    className={clsx(
      'text-2xl font-semibold leading-none tracking-tight',
      className
    )}
    {...props}
  />
))
CardTitle.displayName = 'CardTitle'

const CardDescription = forwardRef<
  HTMLParagraphElement,
  HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
  <p
    ref={ref}
    className={clsx('text-sm text-gray-500 dark:text-gray-400', className)}
    {...props}
  />
))
CardDescription.displayName = 'CardDescription'

const CardContent = forwardRef<
  HTMLDivElement,
  HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div ref={ref} className={clsx('p-6 pt-0', className)} {...props} />
))
CardContent.displayName = 'CardContent'

const CardFooter = forwardRef<
  HTMLDivElement,
  HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={clsx('flex items-center p-6 pt-0', className)}
    {...props}
  />
))
CardFooter.displayName = 'CardFooter'

export { Card, CardHeader, CardFooter, CardTitle, CardDescription, CardContent }

// Specialized card components
export interface StatsCardProps {
  title: string
  value: string | number
  description?: string
  icon?: ReactNode
  trend?: {
    value: number
    label: string
    direction: 'up' | 'down' | 'neutral'
  }
  className?: string
}

export function StatsCard({ 
  title, 
  value, 
  description, 
  icon, 
  trend,
  className 
}: StatsCardProps) {
  return (
    <Card className={clsx('p-6', className)}>
      <div className="flex items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        {icon && <div className="h-4 w-4 text-gray-500">{icon}</div>}
      </div>
      <div>
        <div className="text-2xl font-bold">{value}</div>
        {(description || trend) && (
          <div className="flex items-center space-x-2 text-xs text-gray-500">
            {trend && (
              <span className={clsx(
                'flex items-center',
                trend.direction === 'up' && 'text-success-600',
                trend.direction === 'down' && 'text-error-600',
                trend.direction === 'neutral' && 'text-gray-500'
              )}>
                {trend.direction === 'up' && '↗'}
                {trend.direction === 'down' && '↘'}
                {trend.direction === 'neutral' && '→'}
                {trend.value}% {trend.label}
              </span>
            )}
            {description && <span>{description}</span>}
          </div>
        )}
      </div>
    </Card>
  )
}

export interface ActionCardProps {
  title: string
  description?: string
  icon?: ReactNode
  action?: ReactNode
  onClick?: () => void
  className?: string
  disabled?: boolean
}

export function ActionCard({ 
  title, 
  description, 
  icon, 
  action,
  onClick,
  className,
  disabled = false
}: ActionCardProps) {
  const isClickable = !!onClick && !disabled

  return (
    <Card 
      className={clsx(
        'p-6 transition-colors',
        isClickable && 'cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700',
        disabled && 'opacity-50 cursor-not-allowed',
        className
      )}
      onClick={isClickable ? onClick : undefined}
    >
      <div className="flex items-start justify-between">
        <div className="flex items-start space-x-3">
          {icon && (
            <div className="flex-shrink-0 text-gray-500">
              {icon}
            </div>
          )}
          <div className="flex-1 min-w-0">
            <CardTitle className="text-base">{title}</CardTitle>
            {description && (
              <CardDescription className="mt-1">
                {description}
              </CardDescription>
            )}
          </div>
        </div>
        {action && (
          <div className="flex-shrink-0 ml-4">
            {action}
          </div>
        )}
      </div>
    </Card>
  )
}

export interface FeatureCardProps {
  title: string
  description: string
  icon?: ReactNode
  badge?: string
  features?: string[]
  action?: ReactNode
  className?: string
}

export function FeatureCard({ 
  title, 
  description, 
  icon, 
  badge,
  features,
  action,
  className 
}: FeatureCardProps) {
  return (
    <Card className={clsx('p-6', className)}>
      <div className="flex items-start justify-between">
        <div className="flex items-start space-x-3">
          {icon && (
            <div className="flex-shrink-0 text-brand-600 dark:text-brand-400">
              {icon}
            </div>
          )}
          <div className="flex-1">
            <div className="flex items-center space-x-2">
              <CardTitle className="text-lg">{title}</CardTitle>
              {badge && (
                <span className="inline-flex items-center rounded-full bg-brand-100 px-2.5 py-0.5 text-xs font-medium text-brand-800 dark:bg-brand-900 dark:text-brand-200">
                  {badge}
                </span>
              )}
            </div>
            <CardDescription className="mt-2">
              {description}
            </CardDescription>
            {features && features.length > 0 && (
              <ul className="mt-4 space-y-1">
                {features.map((feature, index) => (
                  <li key={index} className="flex items-center text-sm text-gray-600 dark:text-gray-300">
                    <span className="mr-2 h-1.5 w-1.5 rounded-full bg-brand-600 dark:bg-brand-400" />
                    {feature}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      </div>
      {action && (
        <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
          {action}
        </div>
      )}
    </Card>
  )
}
