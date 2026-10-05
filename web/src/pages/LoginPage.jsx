import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '../api/client'
import { useAuthStore } from '../store/authStore'
import { Button, Input } from '../components/ui'
import { 
  Compass, 
  KeyRound, 
  Mail, 
  AlertCircle, 
  ArrowRight, 
  ShieldCheck, 
  Eye, 
  EyeOff,
  Radio
} from 'lucide-react'

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  const { setAuth } = useAuthStore()
  const navigate = useNavigate()

  const handleLogin = async (e) => {
    e.preventDefault()
    setLoading(true)
    setError(null)

    try {
      const data = await authApi.login({ email, password })
      setAuth(data.token, data.user)
      navigate('/')
    } catch (err) {
      setError(err.response?.data?.detail || 'Invalid email or password. Please verify your credentials.')
    } finally {
      setLoading(false)
    }
  }

  const fillDemoAccount = (demoEmail) => {
    setEmail(demoEmail)
    setPassword('Password123!')
  }

  return (
    <div className="min-h-screen bg-slate-950 flex flex-col justify-center items-center px-4 sm:px-6 lg:px-8 text-slate-100 antialiased relative overflow-hidden">
      {/* Background ambient gradient */}
      <div className="absolute top-1/4 left-1/2 -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-waypoint-primary/10 rounded-full blur-3xl pointer-events-none" />

      <div className="w-full max-w-md space-y-6 relative z-10">
        {/* Brand Header */}
        <div className="text-center">
          <div className="mx-auto w-14 h-14 rounded-2xl bg-waypoint-primary flex items-center justify-center text-waypoint-onPrimary shadow-2xl shadow-waypoint-primary/20 mb-3 border border-waypoint-primary/40">
            <Compass className="w-8 h-8 font-bold animate-pulse" />
          </div>
          <div className="flex items-center justify-center gap-2 mb-1">
            <h2 className="text-2xl font-black tracking-tight text-white font-display">
              WayPoint
            </h2>
            <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-waypoint-primary/20 text-waypoint-primary border border-waypoint-primary/40">
              NOC
            </span>
          </div>
          <p className="text-xs text-slate-400">
            National Intercity Transit Operations & Dispatch Desk
          </p>
        </div>

        {/* Login Form Card */}
        <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-6 sm:p-8 shadow-2xl backdrop-blur-md">
          {error && (
            <div
              role="alert"
              className="mb-5 p-3.5 rounded-xl bg-red-950/60 border border-red-800 text-red-200 text-xs flex items-center gap-2.5 animate-in fade-in"
            >
              <AlertCircle className="w-4 h-4 flex-shrink-0 text-red-400" />
              <span className="leading-snug">{error}</span>
            </div>
          )}

          <form onSubmit={handleLogin} className="space-y-4">
            <div>
              <label htmlFor="login-email" className="block text-xs font-semibold text-slate-300 mb-1.5">
                Operator Email Address
              </label>
              <div className="relative">
                <Mail className="w-4 h-4 text-slate-500 absolute left-3.5 top-3" />
                <input
                  id="login-email"
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="officer@waypoint.lk"
                  className="w-full pl-10 pr-4 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-waypoint-primary focus:border-transparent transition-all font-mono"
                />
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label htmlFor="login-password" className="block text-xs font-semibold text-slate-300">
                  Secure Password
                </label>
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="text-[11px] text-slate-400 hover:text-waypoint-primary flex items-center gap-1 transition-colors"
                >
                  {showPassword ? (
                    <>
                      <EyeOff className="w-3.5 h-3.5" /> Hide
                    </>
                  ) : (
                    <>
                      <Eye className="w-3.5 h-3.5" /> Show
                    </>
                  )}
                </button>
              </div>
              <div className="relative">
                <KeyRound className="w-4 h-4 text-slate-500 absolute left-3.5 top-3" />
                <input
                  id="login-password"
                  type={showPassword ? 'text' : 'password'}
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••••••"
                  className="w-full pl-10 pr-4 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-waypoint-primary focus:border-transparent transition-all font-mono"
                />
              </div>
            </div>

            <div className="pt-2">
              <Button
                type="submit"
                variant="primary"
                size="md"
                isLoading={loading}
                className="w-full font-bold shadow-lg shadow-waypoint-primary/20"
              >
                Authenticate & Access NOC <ArrowRight className="w-4 h-4 ml-1" />
              </Button>
            </div>
          </form>

          {/* Quick Demo Fill Accounts */}
          <div className="mt-6 pt-5 border-t border-slate-800">
            <span className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-2.5 flex items-center gap-1.5">
              <Radio className="w-3.5 h-3.5 text-waypoint-primary animate-pulse" />
              1-Click Seeded Roles (Password: Password123!)
            </span>
            <div className="grid grid-cols-2 gap-2 text-xs">
              <button
                type="button"
                onClick={() => fillDemoAccount('manager@waypoint.lk')}
                className="p-2.5 rounded-xl bg-slate-950 border border-slate-800 hover:border-amber-500/50 hover:bg-slate-800/80 text-left transition-all group"
              >
                <div className="font-bold text-amber-400 group-hover:text-amber-300">Manager</div>
                <div className="text-[10px] text-slate-500 font-mono truncate">manager@waypoint.lk</div>
              </button>
              <button
                type="button"
                onClick={() => fillDemoAccount('operator@waypoint.lk')}
                className="p-2.5 rounded-xl bg-slate-950 border border-slate-800 hover:border-waypoint-primary/50 hover:bg-slate-800/80 text-left transition-all group"
              >
                <div className="font-bold text-waypoint-primary group-hover:text-emerald-300">Operator</div>
                <div className="text-[10px] text-slate-500 font-mono truncate">operator@waypoint.lk</div>
              </button>
              <button
                type="button"
                onClick={() => fillDemoAccount('admin@waypoint.lk')}
                className="p-2.5 rounded-xl bg-slate-950 border border-slate-800 hover:border-indigo-500/50 hover:bg-slate-800/80 text-left transition-all group"
              >
                <div className="font-bold text-indigo-400 group-hover:text-indigo-300">Admin</div>
                <div className="text-[10px] text-slate-500 font-mono truncate">admin@waypoint.lk</div>
              </button>
              <button
                type="button"
                onClick={() => fillDemoAccount('passenger@waypoint.lk')}
                className="p-2.5 rounded-xl bg-slate-950 border border-slate-800 hover:border-sky-500/50 hover:bg-slate-800/80 text-left transition-all group"
              >
                <div className="font-bold text-sky-400 group-hover:text-sky-300">Passenger</div>
                <div className="text-[10px] text-slate-500 font-mono truncate">passenger@waypoint.lk</div>
              </button>
            </div>
          </div>
        </div>

        {/* Footer */}
        <p className="text-center text-xs text-slate-500">
          New transit passenger?{' '}
          <Link to="/register" className="text-waypoint-primary hover:underline font-semibold">
            Create an Account
          </Link>
        </p>
      </div>
    </div>
  )
}
