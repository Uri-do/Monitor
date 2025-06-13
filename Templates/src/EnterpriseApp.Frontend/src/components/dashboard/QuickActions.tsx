import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { Button } from '@/components/ui/Button'
import { Icon, IconName } from '@/components/ui/Icon'
import { Modal, FormModal } from '@/components/ui/Modal'
import { Input, EmailInput } from '@/components/ui/Input'
import { Select } from '@/components/ui/Select'
import { ActionCard } from '@/components/ui/Card'
import { ConditionalRender } from '@/components/auth/ProtectedRoute'

interface QuickAction {
  id: string
  title: string
  description: string
  icon: IconName
  action: () => void
  requiredRole?: string
  requiredPermission?: string
  variant?: 'default' | 'primary' | 'success' | 'warning' | 'error'
}

export function QuickActions() {
  const navigate = useNavigate()
  const { hasRole, hasPermission } = useAuth()
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [showInviteModal, setShowInviteModal] = useState(false)
  const [showExportModal, setShowExportModal] = useState(false)

  const quickActions: QuickAction[] = [
    {
      id: 'create-entity',
      title: 'Create Domain Entity',
      description: 'Add a new domain entity to the system',
      icon: 'plus',
      action: () => setShowCreateModal(true),
      requiredPermission: 'domain-entity:write',
      variant: 'primary'
    },
    {
      id: 'invite-user',
      title: 'Invite User',
      description: 'Send an invitation to a new team member',
      icon: 'user-plus',
      action: () => setShowInviteModal(true),
      requiredRole: 'Admin',
      variant: 'success'
    },
    {
      id: 'export-data',
      title: 'Export Data',
      description: 'Generate and download data reports',
      icon: 'download',
      action: () => setShowExportModal(true),
      variant: 'default'
    },
    {
      id: 'view-analytics',
      title: 'View Analytics',
      description: 'Access detailed analytics and insights',
      icon: 'bar-chart',
      action: () => navigate('/statistics'),
      variant: 'default'
    },
    {
      id: 'system-health',
      title: 'System Health',
      description: 'Check system status and performance',
      icon: 'heart',
      action: () => navigate('/admin/system-health'),
      requiredRole: 'Admin',
      variant: 'default'
    },
    {
      id: 'manage-workers',
      title: 'Manage Workers',
      description: 'Control background job processing',
      icon: 'cog',
      action: () => navigate('/worker'),
      requiredRole: 'Admin',
      variant: 'warning'
    }
  ]

  const visibleActions = quickActions.filter(action => {
    if (action.requiredRole && !hasRole(action.requiredRole as any)) {
      return false
    }
    if (action.requiredPermission && !hasPermission(action.requiredPermission as any)) {
      return false
    }
    return true
  })

  return (
    <>
      <div className="space-y-3">
        {visibleActions.map((action) => (
          <ActionCard
            key={action.id}
            title={action.title}
            description={action.description}
            icon={<Icon name={action.icon} className="h-5 w-5" />}
            onClick={action.action}
            action={
              <Button
                size="sm"
                variant={action.variant === 'primary' ? 'default' : 'outline'}
                rightIcon="arrow-right"
              >
                Go
              </Button>
            }
          />
        ))}

        {/* Additional Quick Links */}
        <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
          <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">
            Quick Links
          </h4>
          <div className="grid grid-cols-2 gap-2">
            <Button
              variant="ghost"
              size="sm"
              leftIcon="settings"
              className="justify-start"
              onClick={() => navigate('/settings')}
            >
              Settings
            </Button>
            <Button
              variant="ghost"
              size="sm"
              leftIcon="user"
              className="justify-start"
              onClick={() => navigate('/profile')}
            >
              Profile
            </Button>
            <ConditionalRender roles={['Admin']}>
              <Button
                variant="ghost"
                size="sm"
                leftIcon="users"
                className="justify-start"
                onClick={() => navigate('/admin/users')}
              >
                Users
              </Button>
            </ConditionalRender>
            <Button
              variant="ghost"
              size="sm"
              leftIcon="help-circle"
              className="justify-start"
              onClick={() => window.open('/help', '_blank')}
            >
              Help
            </Button>
          </div>
        </div>
      </div>

      {/* Create Domain Entity Modal */}
      <CreateEntityModal
        open={showCreateModal}
        onClose={() => setShowCreateModal(false)}
      />

      {/* Invite User Modal */}
      <InviteUserModal
        open={showInviteModal}
        onClose={() => setShowInviteModal(false)}
      />

      {/* Export Data Modal */}
      <ExportDataModal
        open={showExportModal}
        onClose={() => setShowExportModal(false)}
      />
    </>
  )
}

// Create Domain Entity Modal
function CreateEntityModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const navigate = useNavigate()
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    type: 'standard'
  })

  const handleSubmit = () => {
    // In a real app, this would create the entity via API
    console.log('Creating entity:', formData)
    onClose()
    navigate('/domain-entities/create')
  }

  return (
    <FormModal
      open={open}
      onClose={onClose}
      title="Create Domain Entity"
      description="Create a new domain entity in the system"
      onSubmit={handleSubmit}
      submitText="Create Entity"
    >
      <div className="space-y-4">
        <Input
          label="Entity Name"
          placeholder="Enter entity name"
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
        />
        
        <Input
          label="Description"
          placeholder="Enter entity description"
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
        />

        <Select
          label="Entity Type"
          value={formData.type}
          onChange={(value) => setFormData({ ...formData, type: value })}
          options={[
            { value: 'standard', label: 'Standard Entity' },
            { value: 'aggregate', label: 'Aggregate Root' },
            { value: 'value-object', label: 'Value Object' },
          ]}
        />
      </div>
    </FormModal>
  )
}

// Invite User Modal
function InviteUserModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [formData, setFormData] = useState({
    email: '',
    role: 'User',
    message: ''
  })

  const handleSubmit = () => {
    // In a real app, this would send the invitation via API
    console.log('Sending invitation:', formData)
    onClose()
    // Reset form
    setFormData({ email: '', role: 'User', message: '' })
  }

  return (
    <FormModal
      open={open}
      onClose={onClose}
      title="Invite User"
      description="Send an invitation to join your team"
      onSubmit={handleSubmit}
      submitText="Send Invitation"
    >
      <div className="space-y-4">
        <EmailInput
          label="Email Address"
          placeholder="user@example.com"
          value={formData.email}
          onChange={(e) => setFormData({ ...formData, email: e.target.value })}
        />
        
        <Select
          label="Role"
          value={formData.role}
          onChange={(value) => setFormData({ ...formData, role: value })}
          options={[
            { value: 'User', label: 'User' },
            { value: 'Manager', label: 'Manager' },
            { value: 'Admin', label: 'Admin' },
          ]}
        />

        <Input
          label="Personal Message (Optional)"
          placeholder="Welcome to our team!"
          value={formData.message}
          onChange={(e) => setFormData({ ...formData, message: e.target.value })}
        />
      </div>
    </FormModal>
  )
}

// Export Data Modal
function ExportDataModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [formData, setFormData] = useState({
    dataType: 'domain-entities',
    format: 'csv',
    dateRange: '30-days'
  })

  const handleSubmit = () => {
    // In a real app, this would trigger the export job
    console.log('Starting export:', formData)
    onClose()
    // Show success message
    alert('Export job started! You will receive an email when it\'s complete.')
  }

  return (
    <FormModal
      open={open}
      onClose={onClose}
      title="Export Data"
      description="Generate and download data reports"
      onSubmit={handleSubmit}
      submitText="Start Export"
    >
      <div className="space-y-4">
        <Select
          label="Data Type"
          value={formData.dataType}
          onChange={(value) => setFormData({ ...formData, dataType: value })}
          options={[
            { value: 'domain-entities', label: 'Domain Entities' },
            { value: 'users', label: 'Users' },
            { value: 'activity-logs', label: 'Activity Logs' },
            { value: 'system-metrics', label: 'System Metrics' },
          ]}
        />
        
        <Select
          label="Format"
          value={formData.format}
          onChange={(value) => setFormData({ ...formData, format: value })}
          options={[
            { value: 'csv', label: 'CSV' },
            { value: 'excel', label: 'Excel (XLSX)' },
            { value: 'json', label: 'JSON' },
            { value: 'pdf', label: 'PDF Report' },
          ]}
        />

        <Select
          label="Date Range"
          value={formData.dateRange}
          onChange={(value) => setFormData({ ...formData, dateRange: value })}
          options={[
            { value: '7-days', label: 'Last 7 days' },
            { value: '30-days', label: 'Last 30 days' },
            { value: '90-days', label: 'Last 90 days' },
            { value: 'all-time', label: 'All time' },
          ]}
        />
      </div>
    </FormModal>
  )
}
