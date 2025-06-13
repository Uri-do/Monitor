import { useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Helmet } from 'react-helmet-async'

import { useAuth } from '@/providers/AuthProvider'
import { Button } from '@/components/ui/Button'
import { EmailInput } from '@/components/ui/Input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/Card'
import { Alert } from '@/components/ui/Alert'
import { Icon } from '@/components/ui/Icon'

// Validation schema
const forgotPasswordSchema = z.object({
  email: z
    .string()
    .min(1, 'Email is required')
    .email('Please enter a valid email address'),
})

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>

export default function ForgotPasswordPage() {
  const { forgotPassword, isAuthenticated } = useAuth()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isSuccess, setIsSuccess] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
    getValues,
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      email: '',
    },
  })

  // Redirect if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setIsSubmitting(true)
    setError(null)

    try {
      await forgotPassword(data.email)
      setIsSuccess(true)
    } catch (error: any) {
      setError(error.message || 'Failed to send reset email')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleResend = async () => {
    const email = getValues('email')
    if (email) {
      setIsSubmitting(true)
      setError(null)

      try {
        await forgotPassword(email)
        // Success message will be shown via toast
      } catch (error: any) {
        setError(error.message || 'Failed to resend email')
      } finally {
        setIsSubmitting(false)
      }
    }
  }

  return (
    <>
      <Helmet>
        <title>Forgot Password - EnterpriseApp</title>
        <meta name="description" content="Reset your EnterpriseApp password" />
      </Helmet>

      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          {/* Header */}
          <div className="text-center">
            <Icon name="lock" className="mx-auto h-12 w-12 text-brand-600 dark:text-brand-400" />
            <h2 className="mt-6 text-3xl font-bold text-gray-900 dark:text-white">
              Forgot your password?
            </h2>
            <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
              No worries! Enter your email and we'll send you reset instructions.
            </p>
          </div>

          {/* Form */}
          <Card>
            {!isSuccess ? (
              <>
                <CardHeader>
                  <CardTitle>Reset password</CardTitle>
                  <CardDescription>
                    Enter your email address and we'll send you a link to reset your password
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  {/* Error Alert */}
                  {error && (
                    <Alert
                      variant="error"
                      title="Error"
                      description={error}
                      dismissible
                      onDismiss={() => setError(null)}
                      className="mb-6"
                    />
                  )}

                  <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                    {/* Email */}
                    <EmailInput
                      label="Email address"
                      placeholder="Enter your email address"
                      description="We'll send password reset instructions to this email"
                      error={errors.email?.message}
                      {...register('email')}
                    />

                    {/* Submit Button */}
                    <Button
                      type="submit"
                      className="w-full"
                      loading={isSubmitting}
                      disabled={isSubmitting}
                    >
                      Send reset instructions
                    </Button>
                  </form>

                  {/* Back to Login */}
                  <div className="mt-6 text-center">
                    <Link
                      to="/login"
                      className="inline-flex items-center text-sm font-medium text-brand-600 hover:text-brand-500 dark:text-brand-400 dark:hover:text-brand-300"
                    >
                      <Icon name="arrow-left" className="mr-2 h-4 w-4" />
                      Back to sign in
                    </Link>
                  </div>
                </CardContent>
              </>
            ) : (
              <>
                <CardHeader>
                  <div className="text-center">
                    <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100 dark:bg-green-900">
                      <Icon name="check" className="h-6 w-6 text-green-600 dark:text-green-400" />
                    </div>
                    <CardTitle className="mt-4">Check your email</CardTitle>
                    <CardDescription className="mt-2">
                      We've sent password reset instructions to{' '}
                      <span className="font-medium text-gray-900 dark:text-gray-100">
                        {getValues('email')}
                      </span>
                    </CardDescription>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {/* Instructions */}
                    <Alert
                      variant="info"
                      title="What's next?"
                      description="Click the link in the email to reset your password. The link will expire in 1 hour for security reasons."
                    />

                    {/* Resend Button */}
                    <div className="text-center">
                      <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
                        Didn't receive the email? Check your spam folder or
                      </p>
                      <Button
                        variant="outline"
                        onClick={handleResend}
                        loading={isSubmitting}
                        disabled={isSubmitting}
                      >
                        Resend email
                      </Button>
                    </div>

                    {/* Back to Login */}
                    <div className="text-center pt-4 border-t border-gray-200 dark:border-gray-700">
                      <Link
                        to="/login"
                        className="inline-flex items-center text-sm font-medium text-brand-600 hover:text-brand-500 dark:text-brand-400 dark:hover:text-brand-300"
                      >
                        <Icon name="arrow-left" className="mr-2 h-4 w-4" />
                        Back to sign in
                      </Link>
                    </div>
                  </div>
                </CardContent>
              </>
            )}
          </Card>

          {/* Help */}
          <div className="text-center">
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Still having trouble?{' '}
              <Link
                to="/contact"
                className="font-medium text-brand-600 hover:text-brand-500 dark:text-brand-400"
              >
                Contact support
              </Link>
            </p>
          </div>
        </div>
      </div>
    </>
  )
}
