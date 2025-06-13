import { Fragment } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Dialog, Transition } from '@headlessui/react'
import { 
  XMarkIcon,
  ChevronDownIcon,
  ChevronRightIcon
} from '@heroicons/react/24/outline'
import { motion, AnimatePresence } from 'framer-motion'
import { clsx } from 'clsx'

import { navigationItems } from '@/routes'
import { useAuth } from '@/hooks/useAuth'
import { Icon } from '@/components/ui/Icon'
import { Logo } from '@/components/ui/Logo'
import { UserMenu } from './UserMenu'
import { useState } from 'react'

interface SidebarProps {
  open: boolean
  onClose: () => void
}

export function Sidebar({ open, onClose }: SidebarProps) {
  const location = useLocation()
  const { user, hasRole, hasPermission } = useAuth()
  const [expandedItems, setExpandedItems] = useState<string[]>([])

  const toggleExpanded = (itemName: string) => {
    setExpandedItems(prev => 
      prev.includes(itemName) 
        ? prev.filter(name => name !== itemName)
        : [...prev, itemName]
    )
  }

  const isItemVisible = (item: any) => {
    if (item.requiredRole && !hasRole(item.requiredRole)) {
      return false
    }
    if (item.requiredPermission && !hasPermission(item.requiredPermission)) {
      return false
    }
    return true
  }

  const isItemActive = (href: string) => {
    if (href === '/') {
      return location.pathname === '/'
    }
    return location.pathname.startsWith(href)
  }

  const SidebarContent = () => (
    <div className="flex h-full flex-col">
      {/* Logo */}
      <div className="flex h-16 shrink-0 items-center px-6">
        <Logo className="h-8 w-auto" />
        <span className="ml-3 text-xl font-semibold text-gray-900 dark:text-white">
          EnterpriseApp
        </span>
      </div>

      {/* Navigation */}
      <nav className="flex flex-1 flex-col px-6 pb-4">
        <ul role="list" className="flex flex-1 flex-col gap-y-7">
          <li>
            <ul role="list" className="-mx-2 space-y-1">
              {navigationItems.map((item) => {
                if (!isItemVisible(item)) return null

                const isActive = isItemActive(item.href)
                const isExpanded = expandedItems.includes(item.name)
                const hasChildren = item.children && item.children.length > 0

                return (
                  <li key={item.name}>
                    <div>
                      {hasChildren ? (
                        <button
                          onClick={() => toggleExpanded(item.name)}
                          className={clsx(
                            'group flex w-full items-center gap-x-3 rounded-md p-2 text-left text-sm font-semibold leading-6 transition-colors',
                            isActive
                              ? 'bg-brand-50 text-brand-600 dark:bg-brand-900/20 dark:text-brand-400'
                              : 'text-gray-700 hover:bg-gray-50 hover:text-brand-600 dark:text-gray-300 dark:hover:bg-gray-800 dark:hover:text-brand-400'
                          )}
                        >
                          <Icon
                            name={item.icon}
                            className={clsx(
                              'h-6 w-6 shrink-0',
                              isActive
                                ? 'text-brand-600 dark:text-brand-400'
                                : 'text-gray-400 group-hover:text-brand-600 dark:group-hover:text-brand-400'
                            )}
                          />
                          <span className="flex-1">{item.name}</span>
                          {isExpanded ? (
                            <ChevronDownIcon className="h-4 w-4" />
                          ) : (
                            <ChevronRightIcon className="h-4 w-4" />
                          )}
                        </button>
                      ) : (
                        <Link
                          to={item.href}
                          className={clsx(
                            'group flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 transition-colors',
                            isActive
                              ? 'bg-brand-50 text-brand-600 dark:bg-brand-900/20 dark:text-brand-400'
                              : 'text-gray-700 hover:bg-gray-50 hover:text-brand-600 dark:text-gray-300 dark:hover:bg-gray-800 dark:hover:text-brand-400'
                          )}
                        >
                          <Icon
                            name={item.icon}
                            className={clsx(
                              'h-6 w-6 shrink-0',
                              isActive
                                ? 'text-brand-600 dark:text-brand-400'
                                : 'text-gray-400 group-hover:text-brand-600 dark:group-hover:text-brand-400'
                            )}
                          />
                          {item.name}
                        </Link>
                      )}
                    </div>

                    {/* Submenu */}
                    <AnimatePresence>
                      {hasChildren && isExpanded && (
                        <motion.div
                          initial={{ height: 0, opacity: 0 }}
                          animate={{ height: 'auto', opacity: 1 }}
                          exit={{ height: 0, opacity: 0 }}
                          transition={{ duration: 0.2 }}
                          className="overflow-hidden"
                        >
                          <ul className="mt-1 space-y-1 pl-6">
                            {item.children?.map((child) => {
                              if (!isItemVisible(child)) return null

                              const isChildActive = isItemActive(child.href)

                              return (
                                <li key={child.name}>
                                  <Link
                                    to={child.href}
                                    className={clsx(
                                      'group flex gap-x-3 rounded-md p-2 text-sm leading-6 transition-colors',
                                      isChildActive
                                        ? 'bg-brand-50 text-brand-600 dark:bg-brand-900/20 dark:text-brand-400'
                                        : 'text-gray-600 hover:bg-gray-50 hover:text-brand-600 dark:text-gray-400 dark:hover:bg-gray-800 dark:hover:text-brand-400'
                                    )}
                                  >
                                    <Icon
                                      name={child.icon}
                                      className={clsx(
                                        'h-5 w-5 shrink-0',
                                        isChildActive
                                          ? 'text-brand-600 dark:text-brand-400'
                                          : 'text-gray-400 group-hover:text-brand-600 dark:group-hover:text-brand-400'
                                      )}
                                    />
                                    {child.name}
                                  </Link>
                                </li>
                              )
                            })}
                          </ul>
                        </motion.div>
                      )}
                    </AnimatePresence>
                  </li>
                )
              })}
            </ul>
          </li>

          {/* User menu at bottom */}
          <li className="mt-auto">
            <UserMenu />
          </li>
        </ul>
      </nav>
    </div>
  )

  return (
    <>
      {/* Mobile sidebar */}
      <Transition.Root show={open} as={Fragment}>
        <Dialog as="div" className="relative z-50 lg:hidden" onClose={onClose}>
          <Transition.Child
            as={Fragment}
            enter="transition-opacity ease-linear duration-300"
            enterFrom="opacity-0"
            enterTo="opacity-100"
            leave="transition-opacity ease-linear duration-300"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <div className="fixed inset-0 bg-gray-900/80" />
          </Transition.Child>

          <div className="fixed inset-0 flex">
            <Transition.Child
              as={Fragment}
              enter="transition ease-in-out duration-300 transform"
              enterFrom="-translate-x-full"
              enterTo="translate-x-0"
              leave="transition ease-in-out duration-300 transform"
              leaveFrom="translate-x-0"
              leaveTo="-translate-x-full"
            >
              <Dialog.Panel className="relative mr-16 flex w-full max-w-xs flex-1">
                <Transition.Child
                  as={Fragment}
                  enter="ease-in-out duration-300"
                  enterFrom="opacity-0"
                  enterTo="opacity-100"
                  leave="ease-in-out duration-300"
                  leaveFrom="opacity-100"
                  leaveTo="opacity-0"
                >
                  <div className="absolute left-full top-0 flex w-16 justify-center pt-5">
                    <button
                      type="button"
                      className="-m-2.5 p-2.5"
                      onClick={onClose}
                    >
                      <span className="sr-only">Close sidebar</span>
                      <XMarkIcon className="h-6 w-6 text-white" aria-hidden="true" />
                    </button>
                  </div>
                </Transition.Child>

                <div className="flex grow flex-col gap-y-5 overflow-y-auto bg-white dark:bg-gray-900 px-6 pb-4 ring-1 ring-white/10">
                  <SidebarContent />
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </Dialog>
      </Transition.Root>

      {/* Desktop sidebar */}
      <div className="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-72 lg:flex-col">
        <div className="flex grow flex-col gap-y-5 overflow-y-auto bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700">
          <SidebarContent />
        </div>
      </div>
    </>
  )
}
