import { forwardRef } from 'react'
import { LucideIcon, LucideProps } from 'lucide-react'
import { clsx } from 'clsx'

// Import all icons we'll use throughout the app
import {
  // Navigation & Layout
  LayoutDashboard,
  Menu,
  X,
  ChevronDown,
  ChevronRight,
  ChevronLeft,
  ChevronUp,
  ArrowLeft,
  ArrowRight,
  Home,

  // Data & Content
  Database,
  List,
  Grid3X3,
  Table,
  FileText,
  File,
  Folder,
  Search,
  Filter,
  SortAsc,
  SortDesc,

  // Actions
  Plus,
  Edit,
  Trash2,
  Save,
  Download,
  Upload,
  Copy,
  Share,
  ExternalLink,
  MoreHorizontal,
  MoreVertical,

  // Status & Feedback
  Check,
  CheckCircle,
  X as XIcon,
  XCircle,
  AlertCircle,
  AlertTriangle,
  Info,
  HelpCircle,

  // User & Auth
  User,
  Users,
  UserCheck,
  UserPlus,
  UserMinus,
  Shield,
  Lock,
  Unlock,
  Eye,
  EyeOff,

  // System & Settings
  Settings,
  Cog,
  Sliders,
  Tool,
  Wrench,
  Power,
  Refresh,
  RotateCcw,

  // Communication
  Mail,
  MessageSquare,
  Bell,
  BellOff,
  Phone,

  // Media & UI
  Image,
  Play,
  Pause,
  Stop,
  Volume2,
  VolumeX,

  // Charts & Analytics
  BarChart3,
  LineChart,
  PieChart,
  TrendingUp,
  TrendingDown,
  Activity,

  // Time & Calendar
  Calendar,
  Clock,
  Timer,

  // Weather & Status
  Sun,
  Moon,
  Cloud,
  Zap,

  // Devices
  Monitor,
  Smartphone,
  Tablet,
  Laptop,

  // Misc
  Heart,
  Star,
  Bookmark,
  Tag,
  Flag,
  MapPin,
  Globe,
  Wifi,
  WifiOff,

} from 'lucide-react'

// Icon registry - maps string names to components
const iconRegistry = {
  // Navigation & Layout
  'layout-dashboard': LayoutDashboard,
  'menu': Menu,
  'x': X,
  'chevron-down': ChevronDown,
  'chevron-right': ChevronRight,
  'chevron-left': ChevronLeft,
  'chevron-up': ChevronUp,
  'arrow-left': ArrowLeft,
  'arrow-right': ArrowRight,
  'home': Home,

  // Data & Content
  'database': Database,
  'list': List,
  'grid': Grid3X3,
  'table': Table,
  'file-text': FileText,
  'file': File,
  'folder': Folder,
  'search': Search,
  'filter': Filter,
  'sort-asc': SortAsc,
  'sort-desc': SortDesc,

  // Actions
  'plus': Plus,
  'edit': Edit,
  'trash': Trash2,
  'save': Save,
  'download': Download,
  'upload': Upload,
  'copy': Copy,
  'share': Share,
  'external-link': ExternalLink,
  'more-horizontal': MoreHorizontal,
  'more-vertical': MoreVertical,

  // Status & Feedback
  'check': Check,
  'check-circle': CheckCircle,
  'x-icon': XIcon,
  'x-circle': XCircle,
  'alert-circle': AlertCircle,
  'alert-triangle': AlertTriangle,
  'info': Info,
  'help-circle': HelpCircle,

  // User & Auth
  'user': User,
  'users': Users,
  'user-check': UserCheck,
  'user-plus': UserPlus,
  'user-minus': UserMinus,
  'shield': Shield,
  'lock': Lock,
  'unlock': Unlock,
  'eye': Eye,
  'eye-off': EyeOff,

  // System & Settings
  'settings': Settings,
  'cog': Cog,
  'sliders': Sliders,
  'tool': Tool,
  'wrench': Wrench,
  'power': Power,
  'refresh': Refresh,
  'rotate-ccw': RotateCcw,

  // Communication
  'mail': Mail,
  'message-square': MessageSquare,
  'bell': Bell,
  'bell-off': BellOff,
  'phone': Phone,

  // Media & UI
  'image': Image,
  'play': Play,
  'pause': Pause,
  'stop': Stop,
  'volume': Volume2,
  'volume-off': VolumeX,

  // Charts & Analytics
  'bar-chart': BarChart3,
  'line-chart': LineChart,
  'pie-chart': PieChart,
  'trending-up': TrendingUp,
  'trending-down': TrendingDown,
  'activity': Activity,

  // Time & Calendar
  'calendar': Calendar,
  'clock': Clock,
  'timer': Timer,

  // Weather & Status
  'sun': Sun,
  'moon': Moon,
  'cloud': Cloud,
  'zap': Zap,

  // Devices
  'monitor': Monitor,
  'smartphone': Smartphone,
  'tablet': Tablet,
  'laptop': Laptop,

  // Misc
  'heart': Heart,
  'star': Star,
  'bookmark': Bookmark,
  'tag': Tag,
  'flag': Flag,
  'map-pin': MapPin,
  'globe': Globe,
  'wifi': Wifi,
  'wifi-off': WifiOff,
} as const

export type IconName = keyof typeof iconRegistry

export interface IconProps extends Omit<LucideProps, 'ref'> {
  name: IconName
}

export const Icon = forwardRef<SVGSVGElement, IconProps>(
  ({ name, className, ...props }, ref) => {
    const IconComponent = iconRegistry[name] as LucideIcon

    if (!IconComponent) {
      console.warn(`Icon "${name}" not found in registry`)
      return null
    }

    return (
      <IconComponent
        ref={ref}
        className={clsx('h-4 w-4', className)}
        {...props}
      />
    )
  }
)

Icon.displayName = 'Icon'

// Export individual icons for direct use
export {
  LayoutDashboard,
  Database,
  List,
  Plus,
  Edit,
  Trash2,
  Settings,
  Users,
  BarChart3,
  Calendar,
  Bell,
  Search,
  Menu,
  X,
  ChevronDown,
  ChevronRight,
  Check,
  AlertCircle,
  User,
  Shield,
  Activity,
  Heart,
  // Add more as needed
}

// Utility function to get all available icon names
export const getAvailableIcons = (): IconName[] => {
  return Object.keys(iconRegistry) as IconName[]
}

// Utility function to check if an icon exists
export const hasIcon = (name: string): name is IconName => {
  return name in iconRegistry
}