import { Fragment, ReactNode } from 'react'
import { Dialog, Transition } from '@headlessui/react'
import { XMarkIcon } from '@heroicons/react/24/outline'
import { clsx } from 'clsx'

import { Button } from './Button'
import { Icon } from './Icon'

export interface ModalProps {
  open: boolean
  onClose: () => void
  title?: string
  description?: string
  children: ReactNode
  size?: 'sm' | 'md' | 'lg' | 'xl' | '2xl' | 'full'
  showCloseButton?: boolean
  closeOnOverlayClick?: boolean
  className?: string
}

export function Modal({
  open,
  onClose,
  title,
  description,
  children,
  size = 'md',
  showCloseButton = true,
  closeOnOverlayClick = true,
  className,
}: ModalProps) {
  const sizeClasses = {
    sm: 'max-w-md',
    md: 'max-w-lg',
    lg: 'max-w-2xl',
    xl: 'max-w-4xl',
    '2xl': 'max-w-6xl',
    full: 'max-w-full mx-4',
  }

  return (
    <Transition.Root show={open} as={Fragment}>
      <Dialog 
        as="div" 
        className="relative z-50" 
        onClose={closeOnOverlayClick ? onClose : () => {}}
      >
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" />
        </Transition.Child>

        <div className="fixed inset-0 z-10 overflow-y-auto">
          <div className="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
            <Transition.Child
              as={Fragment}
              enter="ease-out duration-300"
              enterFrom="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
              enterTo="opacity-100 translate-y-0 sm:scale-100"
              leave="ease-in duration-200"
              leaveFrom="opacity-100 translate-y-0 sm:scale-100"
              leaveTo="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
            >
              <Dialog.Panel 
                className={clsx(
                  'relative transform overflow-hidden rounded-lg bg-white px-4 pb-4 pt-5 text-left shadow-xl transition-all sm:my-8 sm:w-full sm:p-6 dark:bg-gray-800',
                  sizeClasses[size],
                  className
                )}
              >
                {/* Header */}
                {(title || showCloseButton) && (
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      {title && (
                        <Dialog.Title 
                          as="h3" 
                          className="text-lg font-semibold leading-6 text-gray-900 dark:text-white"
                        >
                          {title}
                        </Dialog.Title>
                      )}
                      {description && (
                        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                          {description}
                        </p>
                      )}
                    </div>
                    {showCloseButton && (
                      <button
                        type="button"
                        className="ml-4 rounded-md bg-white text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2 dark:bg-gray-800 dark:hover:text-gray-300"
                        onClick={onClose}
                      >
                        <span className="sr-only">Close</span>
                        <XMarkIcon className="h-6 w-6" aria-hidden="true" />
                      </button>
                    )}
                  </div>
                )}

                {/* Content */}
                <div className={clsx(
                  (title || description) && 'mt-4'
                )}>
                  {children}
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition.Root>
  )
}

// Confirmation Modal
export interface ConfirmModalProps {
  open: boolean
  onClose: () => void
  onConfirm: () => void
  title: string
  message: string
  confirmText?: string
  cancelText?: string
  variant?: 'danger' | 'warning' | 'info'
  loading?: boolean
}

export function ConfirmModal({
  open,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  variant = 'info',
  loading = false,
}: ConfirmModalProps) {
  const iconMap = {
    danger: <Icon name="alert-triangle" className="h-6 w-6 text-error-600" />,
    warning: <Icon name="alert-circle" className="h-6 w-6 text-warning-600" />,
    info: <Icon name="info" className="h-6 w-6 text-brand-600" />,
  }

  const buttonVariantMap = {
    danger: 'destructive' as const,
    warning: 'warning' as const,
    info: 'default' as const,
  }

  return (
    <Modal open={open} onClose={onClose} size="sm">
      <div className="flex items-start space-x-4">
        <div className="flex-shrink-0">
          {iconMap[variant]}
        </div>
        <div className="flex-1">
          <h3 className="text-lg font-medium text-gray-900 dark:text-white">
            {title}
          </h3>
          <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">
            {message}
          </p>
        </div>
      </div>

      <div className="mt-6 flex justify-end space-x-3">
        <Button
          variant="outline"
          onClick={onClose}
          disabled={loading}
        >
          {cancelText}
        </Button>
        <Button
          variant={buttonVariantMap[variant]}
          onClick={onConfirm}
          loading={loading}
        >
          {confirmText}
        </Button>
      </div>
    </Modal>
  )
}

// Form Modal
export interface FormModalProps {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  children: ReactNode
  onSubmit?: () => void
  submitText?: string
  cancelText?: string
  loading?: boolean
  submitDisabled?: boolean
  size?: ModalProps['size']
}

export function FormModal({
  open,
  onClose,
  title,
  description,
  children,
  onSubmit,
  submitText = 'Save',
  cancelText = 'Cancel',
  loading = false,
  submitDisabled = false,
  size = 'md',
}: FormModalProps) {
  return (
    <Modal 
      open={open} 
      onClose={onClose} 
      title={title} 
      description={description}
      size={size}
    >
      <form onSubmit={(e) => {
        e.preventDefault()
        onSubmit?.()
      }}>
        <div className="space-y-4">
          {children}
        </div>

        <div className="mt-6 flex justify-end space-x-3">
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={loading}
          >
            {cancelText}
          </Button>
          {onSubmit && (
            <Button
              type="submit"
              loading={loading}
              disabled={submitDisabled}
            >
              {submitText}
            </Button>
          )}
        </div>
      </form>
    </Modal>
  )
}

// Drawer (slide-out modal)
export interface DrawerProps {
  open: boolean
  onClose: () => void
  title?: string
  children: ReactNode
  position?: 'left' | 'right'
  size?: 'sm' | 'md' | 'lg'
  showCloseButton?: boolean
}

export function Drawer({
  open,
  onClose,
  title,
  children,
  position = 'right',
  size = 'md',
  showCloseButton = true,
}: DrawerProps) {
  const sizeClasses = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
  }

  const positionClasses = {
    left: 'left-0',
    right: 'right-0',
  }

  const slideDirection = {
    left: {
      enter: '-translate-x-full',
      enterTo: 'translate-x-0',
      leave: 'translate-x-0',
      leaveTo: '-translate-x-full',
    },
    right: {
      enter: 'translate-x-full',
      enterTo: 'translate-x-0',
      leave: 'translate-x-0',
      leaveTo: 'translate-x-full',
    },
  }

  return (
    <Transition.Root show={open} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <Transition.Child
          as={Fragment}
          enter="ease-in-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in-out duration-300"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" />
        </Transition.Child>

        <div className="fixed inset-0 overflow-hidden">
          <div className="absolute inset-0 overflow-hidden">
            <div className={clsx(
              'pointer-events-none fixed inset-y-0 flex max-w-full',
              positionClasses[position]
            )}>
              <Transition.Child
                as={Fragment}
                enter="transform transition ease-in-out duration-300"
                enterFrom={slideDirection[position].enter}
                enterTo={slideDirection[position].enterTo}
                leave="transform transition ease-in-out duration-300"
                leaveFrom={slideDirection[position].leave}
                leaveTo={slideDirection[position].leaveTo}
              >
                <Dialog.Panel className={clsx(
                  'pointer-events-auto w-screen',
                  sizeClasses[size]
                )}>
                  <div className="flex h-full flex-col overflow-y-scroll bg-white shadow-xl dark:bg-gray-800">
                    {/* Header */}
                    {(title || showCloseButton) && (
                      <div className="px-4 py-6 sm:px-6">
                        <div className="flex items-start justify-between">
                          {title && (
                            <Dialog.Title className="text-lg font-medium text-gray-900 dark:text-white">
                              {title}
                            </Dialog.Title>
                          )}
                          {showCloseButton && (
                            <div className="ml-3 flex h-7 items-center">
                              <button
                                type="button"
                                className="rounded-md bg-white text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-brand-500 dark:bg-gray-800"
                                onClick={onClose}
                              >
                                <span className="sr-only">Close panel</span>
                                <XMarkIcon className="h-6 w-6" aria-hidden="true" />
                              </button>
                            </div>
                          )}
                        </div>
                      </div>
                    )}

                    {/* Content */}
                    <div className="relative flex-1 px-4 sm:px-6">
                      {children}
                    </div>
                  </div>
                </Dialog.Panel>
              </Transition.Child>
            </div>
          </div>
        </div>
      </Dialog>
    </Transition.Root>
  )
}
