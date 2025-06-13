import { useState, useEffect } from 'react'
import { Link, Navigate, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Helmet } from 'react-helmet-async'

import { useAuth } from '@/providers/AuthProvider'
import { Button } from '@/components/ui/Button'
import { PasswordInput } from '@/components/ui/Input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/Card'
import { Alert } from '@/components/ui/Alert'
import { Icon } from '@/components/ui/Icon'

// Validation schema
const resetPasswordSchema = z.object({
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
    .regex(/\d/, 'Password must contain at least one number')
    .regex(/[!@#$%^&*(),.?":{}|<>]/, 'Password must contain at least one special character'),
  confirmPassword: z
    .string()
    .min(1, 'Please confirm your password'),
}).refine(data => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
})

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>

export default function ResetPasswordPage() {
  const { resetPassword, isAuthenticated } = useAuth()
  const [searchParams] = useSearchParams()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isSuccess, setIsSuccess] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const token = searchParams.get('token')

  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      password: '',
      confirmPassword: '',
    },
  })

  const password = watch('password')

  // Redirect if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  // Redirect if no token
  useEffect(() => {
    if (!token) {
      setError('Invalid or missing reset token. Please request a new password reset.')
    }
  }, [token])

  const onSubmit = async (data: ResetPasswordFormData) => {
    if (!token) {
      setError('Invalid reset token')
      return
    }

    setIsSubmitting(true)
    setError(null)

    try {
      await resetPassword({
        token,
        password: data.password,
        confirmPassword: data.confirmPassword,
      })
      setIsSuccess(true)
    } catch (error: any) {
      setError(error.message || 'Failed to reset password')
    } finally {
      setIsSubmitting(false)
    }
  }

  // Password strength indicator
  const getPasswordStrength = (password: string) => {
    let score = 0
    if (password.length >= 8) score++
    if (/[A-Z]/.test(password)) score++
    if (/[a-z]/.test(password)) score++
    if (/\d/.test(password)) score++
    if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) score++

    return {
      score,
      label: ['Very Weak', 'Weak', 'Fair', 'Good', 'Strong'][score] || 'Very Weak',
      color: ['bg-red-500', 'bg-orange-500', 'bg-yellow-500', 'bg-blue-500', 'bg-green-500'][score] || 'bg-red-500',
    }
  }

  const passwordStrength = password ? getPasswordStrength(password) : null

  return (
    <>
      <Helmet>
        <title>Reset Password - EnterpriseApp</title>
        <meta name="description" content="Reset your EnterpriseApp password" />
      </Helmet>

      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          {/* Header */}
          <div className="text-center">
            <Icon name="lock" className="mx-auto h-12 w-12 text-brand-600 dark:text-brand-400" />
            <h2 className="mt-6 text-3xl font-bold text-gray-900 dark:text-white">
              Reset your password
            </h2>
            <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
              Enter your new password below
            </p>
          </div>

          {/* Form */}
          <Card>
            {!isSuccess ? (
              <>
                <CardHeader>
                  <CardTitle>Create new password</CardTitle>
                  <CardDescription>
                    Your new password must be different from your previous password
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

                  {token ? (
                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                      {/* New Password */}
                      <div>
                        <PasswordInput
                          label="New password"
                          placeholder="Enter your new password"
                          error={errors.password?.message}
                          {...register('password')}
                        />
                        
                        {/* Password Strength Indicator */}
                        {password && passwordStrength && (
                          <div className="mt-2">
                            <div className="flex items-center justify-between text-xs text-gray-600 dark:text-gray-400 mb-1">
                              <span>Password strength</span>
                              <span className={
                                passwordStrength.score >= 4 ? 'text-green-600' :
                                passwordStrength.score >= 3 ? 'text-blue-600' :
                                passwordStrength.score >= 2 ? 'text-yellow-600' :
                                'text-red-600'
                              }>
                                {passwordStrength.label}
                              </span>
                            </div>
                            <div className="w-full bg-gray-200 rounded-full h-2 dark:bg-gray-700">
                              <div
                                className={`h-2 rounded-full transition-all duration-300 ${passwordStrength.color}`}
                                style={{ width: `${(passwordStrength.score / 5) * 100}%` }}
                              />
                            </div>
                          </div>
                        )}
                      </div>

                      {/* Confirm Password */}
                      <PasswordInput
                        label="Confirm new password"
                        placeholder="Confirm your new password"
                        error={errors.confirmPassword?.message}
                        {...register('confirmPassword')}
                      />

                      {/* Password Requirements */}
                      <div className="text-sm text-gray-600 dark:text-gray-400">
                        <p className="font-medium mb-2">Password must contain:</p>
                        <ul className="space-y-1 text-xs">
                          <li className="flex items-center">
                            <Icon 
                              name={password && password.length >= 8 ? "check" : "x"} 
                              className={`mr-2 h-3 w-3 ${password && password.length >= 8 ? 'text-green-500' : 'text-gray-400'}`} 
                            />
                            At least 8 characters
                          </li>
                          <li className="flex items-center">
                            <Icon 
                              name={password && /[A-Z]/.test(password) ? "check" : "x"} 
                              className={`mr-2 h-3 w-3 ${password && /[A-Z]/.test(password) ? 'text-green-500' : 'text-gray-400'}`} 
                            />
                            One uppercase letter
                          </li>
                          <li className="flex items-center">
                            <Icon 
                              name={password && /[a-z]/.test(password) ? "check" : "x"} 
                              className={`mr-2 h-3 w-3 ${password && /[a-z]/.test(password) ? 'text-green-500' : 'text-gray-400'}`} 
                            />
                            One lowercase letter
                          </li>
                          <li className="flex items-center">
                            <Icon 
                              name={password && /\d/.test(password) ? "check" : "x"} 
                              className={`mr-2 h-3 w-3 ${password && /\d/.test(password) ? 'text-green-500' : 'text-gray-400'}`} 
                            />
                            One number
                          </li>
                          <li className="flex items-center">
                            <Icon 
                              name={password && /[!@#$%^&*(),.?":{}|<>]/.test(password) ? "check" : "x"} 
                              className={`mr-2 h-3 w-3 ${password && /[!@#$%^&*(),.?":{}|<>]/.test(password) ? 'text-green-500' : 'text-gray-400'}`} 
                            />
                            One special character
                          </li>
                        </ul>
                      </div>

                      {/* Submit Button */}
                      <Button
                        type="submit"
                        className="w-full"
                        loading={isSubmitting}
                        disabled={isSubmitting}
                      >
                        Reset password
                      </Button>
                    </form>
                  ) : (
                    <div className="text-center py-8">
                      <Alert
                        variant="error"
                        title="Invalid Reset Link"
                        description="This password reset link is invalid or has expired. Please request a new one."
                      />
                      <div className="mt-6">
                        <Link to="/forgot-password">
                          <Button variant="outline">
                            Request new reset link
                          </Button>
                        </Link>
                      </div>
                    </div>
                  )}

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
                    <CardTitle className="mt-4">Password reset successful</CardTitle>
                    <CardDescription className="mt-2">
                      Your password has been successfully reset. You can now sign in with your new password.
                    </CardDescription>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="text-center">
                    <Link to="/login">
                      <Button className="w-full">
                        Continue to sign in
                      </Button>
                    </Link>
                  </div>
                </CardContent>
              </>
            )}
          </Card>
        </div>
      </div>
    </>
  )
}
