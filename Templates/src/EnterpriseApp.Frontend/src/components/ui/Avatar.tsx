import { forwardRef, HTMLAttributes, useState } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { clsx } from 'clsx'

import { Icon } from './Icon'

const avatarVariants = cva(
  'inline-flex items-center justify-center font-medium text-white select-none shrink-0 bg-gray-500 dark:bg-gray-600',
  {
    variants: {
      size: {
        xs: 'h-6 w-6 text-xs',
        sm: 'h-8 w-8 text-sm',
        default: 'h-10 w-10 text-sm',
        lg: 'h-12 w-12 text-base',
        xl: 'h-16 w-16 text-lg',
        '2xl': 'h-20 w-20 text-xl',
      },
      shape: {
        circle: 'rounded-full',
        square: 'rounded-md',
      },
    },
    defaultVariants: {
      size: 'default',
      shape: 'circle',
    },
  }
)

export interface AvatarProps
  extends HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof avatarVariants> {
  src?: string
  alt?: string
  name?: string
  fallback?: string
  showFallback?: boolean
  status?: 'online' | 'offline' | 'away' | 'busy'
  statusPosition?: 'top-right' | 'bottom-right' | 'top-left' | 'bottom-left'
}

const Avatar = forwardRef<HTMLDivElement, AvatarProps>(
  ({ 
    className, 
    size, 
    shape, 
    src, 
    alt, 
    name, 
    fallback, 
    showFallback = true,
    status,
    statusPosition = 'bottom-right',
    ...props 
  }, ref) => {
    const [imageError, setImageError] = useState(false)
    const [imageLoaded, setImageLoaded] = useState(false)

    // Generate initials from name
    const getInitials = (name?: string) => {
      if (!name) return ''
      return name
        .split(' ')
        .map(word => word.charAt(0))
        .join('')
        .toUpperCase()
        .slice(0, 2)
    }

    // Generate background color from name
    const getBackgroundColor = (name?: string) => {
      if (!name) return 'bg-gray-500'
      
      const colors = [
        'bg-red-500',
        'bg-orange-500',
        'bg-amber-500',
        'bg-yellow-500',
        'bg-lime-500',
        'bg-green-500',
        'bg-emerald-500',
        'bg-teal-500',
        'bg-cyan-500',
        'bg-sky-500',
        'bg-blue-500',
        'bg-indigo-500',
        'bg-violet-500',
        'bg-purple-500',
        'bg-fuchsia-500',
        'bg-pink-500',
        'bg-rose-500',
      ]
      
      let hash = 0
      for (let i = 0; i < name.length; i++) {
        hash = name.charCodeAt(i) + ((hash << 5) - hash)
      }
      
      return colors[Math.abs(hash) % colors.length]
    }

    const shouldShowImage = src && !imageError && imageLoaded
    const shouldShowFallback = showFallback && !shouldShowImage
    const initials = getInitials(name || alt)
    const bgColor = getBackgroundColor(name || alt)

    const statusPositionClasses = {
      'top-right': 'top-0 right-0',
      'bottom-right': 'bottom-0 right-0',
      'top-left': 'top-0 left-0',
      'bottom-left': 'bottom-0 left-0',
    }

    const statusColors = {
      online: 'bg-success-500',
      offline: 'bg-gray-400',
      away: 'bg-warning-500',
      busy: 'bg-error-500',
    }

    const statusSizes = {
      xs: 'h-2 w-2',
      sm: 'h-2.5 w-2.5',
      default: 'h-3 w-3',
      lg: 'h-3.5 w-3.5',
      xl: 'h-4 w-4',
      '2xl': 'h-5 w-5',
    }

    return (
      <div className="relative inline-flex">
        <div
          ref={ref}
          className={clsx(
            avatarVariants({ size, shape }),
            !shouldShowImage && bgColor,
            className
          )}
          {...props}
        >
          {src && (
            <img
              src={src}
              alt={alt || name || 'Avatar'}
              className={clsx(
                'h-full w-full object-cover',
                shape === 'circle' ? 'rounded-full' : 'rounded-md',
                shouldShowImage ? 'opacity-100' : 'opacity-0'
              )}
              onLoad={() => setImageLoaded(true)}
              onError={() => setImageError(true)}
            />
          )}
          
          {shouldShowFallback && (
            <span className="absolute inset-0 flex items-center justify-center">
              {fallback || initials || (
                <Icon name="user" className={clsx(
                  size === 'xs' && 'h-3 w-3',
                  size === 'sm' && 'h-4 w-4',
                  size === 'default' && 'h-5 w-5',
                  size === 'lg' && 'h-6 w-6',
                  size === 'xl' && 'h-8 w-8',
                  size === '2xl' && 'h-10 w-10'
                )} />
              )}
            </span>
          )}
        </div>

        {/* Status indicator */}
        {status && (
          <span
            className={clsx(
              'absolute rounded-full border-2 border-white dark:border-gray-800',
              statusPositionClasses[statusPosition],
              statusColors[status],
              statusSizes[size || 'default']
            )}
          />
        )}
      </div>
    )
  }
)

Avatar.displayName = 'Avatar'

export { Avatar, avatarVariants }

// Avatar Group - for displaying multiple avatars
export interface AvatarGroupProps {
  avatars: Array<{
    src?: string
    alt?: string
    name?: string
  }>
  max?: number
  size?: AvatarProps['size']
  shape?: AvatarProps['shape']
  className?: string
}

export function AvatarGroup({ 
  avatars, 
  max = 5, 
  size = 'default', 
  shape = 'circle',
  className 
}: AvatarGroupProps) {
  const visibleAvatars = avatars.slice(0, max)
  const remainingCount = avatars.length - max

  const spacingClasses = {
    xs: '-space-x-1',
    sm: '-space-x-1.5',
    default: '-space-x-2',
    lg: '-space-x-2.5',
    xl: '-space-x-3',
    '2xl': '-space-x-4',
  }

  return (
    <div className={clsx('flex items-center', spacingClasses[size || 'default'], className)}>
      {visibleAvatars.map((avatar, index) => (
        <Avatar
          key={index}
          src={avatar.src}
          alt={avatar.alt}
          name={avatar.name}
          size={size}
          shape={shape}
          className="ring-2 ring-white dark:ring-gray-800"
        />
      ))}
      
      {remainingCount > 0 && (
        <Avatar
          size={size}
          shape={shape}
          fallback={`+${remainingCount}`}
          className="ring-2 ring-white dark:ring-gray-800 bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300"
        />
      )}
    </div>
  )
}

// Avatar with dropdown menu
export interface AvatarMenuProps extends AvatarProps {
  menuItems?: Array<{
    label: string
    onClick: () => void
    icon?: string
    disabled?: boolean
  }>
}

export function AvatarMenu({ menuItems, ...avatarProps }: AvatarMenuProps) {
  // This would typically use a dropdown component
  // For now, just render the avatar
  return <Avatar {...avatarProps} />
}
