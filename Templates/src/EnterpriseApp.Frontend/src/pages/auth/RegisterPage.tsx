import { useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Helmet } from 'react-helmet-async'

import { useAuth } from '@/providers/AuthProvider'
import { Button } from '@/components/ui/Button'
import { Input, EmailInput, PasswordInput } from '@/components/ui/Input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/Card'
import { Alert } from '@/components/ui/Alert'
import { Icon } from '@/components/ui/Icon'

// Validation schema
const registerSchema = z.object({
  firstName: z
    .string()
    .min(1, 'First name is required')
    .min(2, 'First name must be at least 2 characters')
    .max(50, 'First name must be less than 50 characters'),
  lastName: z
    .string()
    .min(1, 'Last name is required')
    .min(2, 'Last name must be at least 2 characters')
    .max(50, 'Last name must be less than 50 characters'),
  email: z
    .string()
    .min(1, 'Email is required')
    .email('Please enter a valid email address'),
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
  acceptTerms: z
    .boolean()
    .refine(val => val === true, 'You must accept the terms and conditions'),
}).refine(data => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
})

type RegisterFormData = z.infer<typeof registerSchema>

export default function RegisterPage() {
  const { register: registerUser, isAuthenticated, isLoading, error, clearError } = useAuth()
  const [isSubmitting, setIsSubmitting] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
    setError,
    watch,
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      confirmPassword: '',
      acceptTerms: false,
    },
  })

  const password = watch('password')

  // Redirect if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  const onSubmit = async (data: RegisterFormData) => {
    setIsSubmitting(true)
    clearError()

    try {
      await registerUser(data)
    } catch (error: any) {
      // Handle specific field errors
      if (error.field) {
        setError(error.field as keyof RegisterFormData, {
          type: 'server',
          message: error.message,
        })
      }
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
        <title>Create Account - EnterpriseApp</title>
        <meta name="description" content="Create your EnterpriseApp account" />
      </Helmet>

      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          {/* Header */}
          <div className="text-center">
            <Icon name="user-plus" className="mx-auto h-12 w-12 text-brand-600 dark:text-brand-400" />
            <h2 className="mt-6 text-3xl font-bold text-gray-900 dark:text-white">
              Create your account
            </h2>
            <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
              Already have an account?{' '}
              <Link
                to="/login"
                className="font-medium text-brand-600 hover:text-brand-500 dark:text-brand-400 dark:hover:text-brand-300"
              >
                Sign in here
              </Link>
            </p>
          </div>

          {/* Register Form */}
          <Card>
            <CardHeader>
              <CardTitle>Get started</CardTitle>
              <CardDescription>
                Fill in your information to create your account
              </CardDescription>
            </CardHeader>
            <CardContent>
              {/* Global Error */}
              {error && (
                <Alert
                  variant="error"
                  title="Registration failed"
                  description={error}
                  dismissible
                  onDismiss={clearError}
                  className="mb-6"
                />
              )}

              <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                {/* Name Fields */}
                <div className="grid grid-cols-2 gap-4">
                  <Input
                    label="First name"
                    placeholder="John"
                    error={errors.firstName?.message}
                    {...register('firstName')}
                  />
                  <Input
                    label="Last name"
                    placeholder="Doe"
                    error={errors.lastName?.message}
                    {...register('lastName')}
                  />
                </div>

                {/* Email */}
                <EmailInput
                  label="Email address"
                  placeholder="john.doe@example.com"
                  error={errors.email?.message}
                  {...register('email')}
                />

                {/* Password */}
                <div>
                  <PasswordInput
                    label="Password"
                    placeholder="Create a strong password"
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
                  label="Confirm password"
                  placeholder="Confirm your password"
                  error={errors.confirmPassword?.message}
                  {...register('confirmPassword')}
                />

                {/* Terms and Conditions */}
                <div className="flex items-start">
                  <div className="flex items-center h-5">
                    <input
                      id="accept-terms"
                      type="checkbox"
                      className="h-4 w-4 text-brand-600 focus:ring-brand-500 border-gray-300 rounded dark:border-gray-600 dark:bg-gray-800"
                      {...register('acceptTerms')}
                    />
                  </div>
                  <div className="ml-3 text-sm">
                    <label htmlFor="accept-terms" className="text-gray-700 dark:text-gray-300">
                      I agree to the{' '}
                      <Link
                        to="/terms"
                        className="font-medium text-brand-600 hover:text-brand-500 dark:text-brand-400"
                        target="_blank"
                      >
                        Terms of Service
                      </Link>{' '}
                      and{' '}
                      <Link
                        to="/privacy"
                        className="font-medium text-brand-600 hover:text-brand-500 dark:text-brand-400"
                        target="_blank"
                      >
                        Privacy Policy
                      </Link>
                    </label>
                    {errors.acceptTerms && (
                      <p className="mt-1 text-sm text-red-600 dark:text-red-400">
                        {errors.acceptTerms.message}
                      </p>
                    )}
                  </div>
                </div>

                {/* Submit Button */}
                <Button
                  type="submit"
                  className="w-full"
                  loading={isSubmitting || isLoading}
                  disabled={isSubmitting || isLoading}
                >
                  Create account
                </Button>
              </form>

              {/* Divider */}
              <div className="mt-6">
                <div className="relative">
                  <div className="absolute inset-0 flex items-center">
                    <div className="w-full border-t border-gray-300 dark:border-gray-600" />
                  </div>
                  <div className="relative flex justify-center text-sm">
                    <span className="px-2 bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400">
                      Or sign up with
                    </span>
                  </div>
                </div>

                {/* Social Registration Buttons */}
                <div className="mt-6 grid grid-cols-2 gap-3">
                  <Button
                    variant="outline"
                    className="w-full"
                    leftIcon="globe"
                    disabled
                  >
                    Google
                  </Button>
                  <Button
                    variant="outline"
                    className="w-full"
                    leftIcon="globe"
                    disabled
                  >
                    Microsoft
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </>
  )
}
