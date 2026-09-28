import React, { useState, useEffect, useCallback } from 'react'
import { disruptionApi } from './disruptionApi'
import { useDisruptionStore } from '@/store/disruptionStore'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import {
  Bell, Radio, Send, AlertTriangle, Info, CheckCircle2,
  AlertCircle, Loader2, RefreshCw, Bus, Clock, Eye
} from 'lucide-react'

export function ServiceAlertBroadcastPage() {
  const {
    serviceAlerts,
    alertsLoading,
    setServiceAlerts,
    setAlertsLoading,
    services,
    setServices,
  } = useDisruptionStore()

  // Form State
  const [selectedServiceId, setSelectedServiceId] = useState('')
  const [title, setTitle] = useState('')
  const [message, setMessage] = useState('')
  const [isBroadcasting, setIsBroadcasting] = useState(false)

  // Toast
  const [actionMessage, setActionMessage] = useState(null)

  const showToast = (type, text) => {
    setActionMessage({ type, text })
    setTimeout(() => setActionMessage(null), 5000)
  }

  const loadAlerts = useCallback(async () => {
    setAlertsLoading(true)
    try {
      const [alertsRes, servicesRes] = await Promise.all([
        disruptionApi.getAlerts(),
        disruptionApi.getServices().catch(() => []),
      ])
      setServiceAlerts(alertsRes)
      if (servicesRes) setServices(servicesRes)
    } catch (err) {
      showToast('error', err.response?.data?.detail || 'Failed to fetch public service alerts.')
    } finally {
      setAlertsLoading(false)
    }
  }, [setServiceAlerts, setAlertsLoading, setServices])

  useEffect(() => {
    loadAlerts()
  }, [loadAlerts])

  const handleBroadcastAlert = async (e) => {
    e.preventDefault()
    if (!selectedServiceId) {
      showToast('error', 'Please select a transit service for this alert broadcast.')
      return
    }
    if (!title.trim() || !message.trim()) {
      showToast('error', 'Please fill in both the alert title and announcement message.')
      return
    }

    setIsBroadcasting(true)
    try {
      await disruptionApi.broadcastAlert({
        serviceId: selectedServiceId,
        title: title.trim(),
        message: message.trim(),
      })
      showToast('success', 'Public transit alert broadcasted live across web and mobile passenger channels!')
      setTitle('')
      setMessage('')
      setSelectedServiceId('')
      loadAlerts()
    } catch (err) {
      showToast('error', err.response?.data?.detail || 'Failed to broadcast public service alert.')
    } finally {
      setIsBroadcasting(false)
    }
  }

  const selectedServiceObj = services.find((s) => s.id === selectedServiceId)

  return (
    <div className="space-y-6">
      {/* Toast Alert */}
      {actionMessage && (
        <div
          className={`flex items-center gap-2 px-4 py-3 rounded-xl text-sm border animate-in fade-in slide-in-from-top-2 ${
            actionMessage.type === 'success'
              ? 'bg-emerald-950/70 border-emerald-700/60 text-emerald-200'
              : 'bg-rose-950/70 border-rose-700/60 text-rose-200'
          }`}
        >
          {actionMessage.type === 'success' ? <CheckCircle2 className="w-5 h-5 flex-shrink-0" /> : <AlertCircle className="w-5 h-5 flex-shrink-0" />}
          <span className="font-medium">{actionMessage.text}</span>
        </div>
      )}

      {/* Broadcast Header & Stats Banner */}
      <div className="p-4 bg-slate-900 border border-slate-800 rounded-2xl flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center text-sky-400 flex-shrink-0">
            <Radio className="w-6 h-6 animate-pulse" />
          </div>
          <div>
            <h4 className="text-sm font-bold text-white">Public Service Alert Broadcast Center (WEB-09)</h4>
            <p className="text-xs text-slate-400">
              Immediate omnichannel passenger push notifications and station ticker broadcasts under rule <strong>BR-NOTIF-001</strong>.
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={loadAlerts} disabled={alertsLoading}>
            <RefreshCw className={`w-4 h-4 mr-1.5 ${alertsLoading ? 'animate-spin' : ''}`} />
            Refresh Alerts
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Column: Broadcast Form */}
        <div className="lg:col-span-5 space-y-5">
          <Card>
            <CardHeader
              title="Broadcast New Notice"
              subtitle="Push instant announcements to public mobile and desktop passengers."
            />

            <form onSubmit={handleBroadcastAlert} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1.5">
                  Target Service *
                </label>
                <select
                  value={selectedServiceId}
                  onChange={(e) => setSelectedServiceId(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-sm text-white focus:outline-none focus:border-waypoint-blue"
                  required
                >
                  <option value="">-- Select affected transit corridor --</option>
                  {services.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.serviceCode} — {s.routeName}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1.5">
                  Notice Headline / Title *
                </label>
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="e.g. SRV-COL-KDY-0700 Cancelled due to Engine Failure"
                  maxLength={100}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-sm text-white focus:outline-none focus:border-waypoint-blue"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1.5">
                  Announcement Body *
                </label>
                <textarea
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  placeholder="Provide clear advice, transfer instructions, or alternative boarding bays for stranded passengers..."
                  rows="4"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-sm text-white focus:outline-none focus:border-waypoint-blue"
                  required
                />
              </div>

              {/* Passenger Banner Live Preview */}
              <div className="pt-2">
                <span className="text-[11px] font-semibold uppercase tracking-wider text-slate-400 flex items-center gap-1.5 mb-2">
                  <Eye className="w-3.5 h-3.5 text-sky-400" />
                  Passenger App Preview
                </span>
                <div className="bg-slate-950/80 border border-amber-500/30 rounded-xl p-3.5 space-y-1.5">
                  <div className="flex items-center gap-2">
                    <span className="w-2 h-2 rounded-full bg-amber-400 animate-ping" />
                    <span className="text-xs font-bold text-amber-300">
                      {title || 'Service Alert Headline'}
                    </span>
                  </div>
                  <p className="text-xs text-slate-300">
                    {message || 'Announcement details and passenger travel guidance will be rendered here.'}
                  </p>
                  <div className="text-[10px] text-slate-400 pt-1 border-t border-slate-800/60 flex items-center justify-between">
                    <span>Target: {selectedServiceObj ? selectedServiceObj.serviceCode : 'Any Service'}</span>
                    <span>Just now</span>
                  </div>
                </div>
              </div>

              <div className="pt-3 border-t border-slate-800">
                <Button type="submit" variant="primary" className="w-full" disabled={isBroadcasting}>
                  {isBroadcasting ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin mr-2" />
                      Broadcasting Notice...
                    </>
                  ) : (
                    <>
                      <Send className="w-4 h-4 mr-2" />
                      Publish Public Broadcast
                    </>
                  )}
                </Button>
              </div>
            </form>
          </Card>
        </div>

        {/* Right Column: Active Live Alerts Feed */}
        <div className="lg:col-span-7 space-y-5">
          <Card>
            <CardHeader
              title="Active Public Alerts Feed"
              subtitle={`Live feed of ${serviceAlerts.length} published operational alerts (AllowAnonymous).`}
            />

            {alertsLoading ? (
              <div className="text-center py-12 text-slate-400">
                <Loader2 className="w-6 h-6 animate-spin mx-auto mb-2 text-waypoint-blue" />
                Retrieving active alert feeds...
              </div>
            ) : serviceAlerts.length === 0 ? (
              <div className="text-center py-12 text-slate-400">
                <Info className="w-8 h-8 mx-auto mb-2 text-slate-500" />
                <p>No active public alerts currently broadcast.</p>
              </div>
            ) : (
              <div className="space-y-3.5">
                {serviceAlerts.map((alert) => (
                  <div
                    key={alert.id}
                    className="bg-slate-950 border border-slate-800/80 rounded-xl p-4 hover:border-slate-700 transition-all space-y-2"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex items-center gap-2">
                        <div className="w-7 h-7 rounded-lg bg-amber-500/10 border border-amber-500/20 flex items-center justify-center text-amber-400">
                          <Bell className="w-4 h-4" />
                        </div>
                        <div>
                          <h4 className="text-sm font-bold text-white">{alert.title}</h4>
                          <span className="text-[11px] font-mono text-sky-400">
                            Service: {alert.serviceCode || 'Network Wide'}
                          </span>
                        </div>
                      </div>
                      <span className="text-[10px] text-slate-400 flex items-center gap-1 font-mono">
                        <Clock className="w-3 h-3" />
                        {new Date(alert.postedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>

                    <p className="text-xs text-slate-300 pl-9 font-sans leading-relaxed">
                      {alert.message}
                    </p>

                    <div className="pl-9 pt-2 border-t border-slate-800/40 flex items-center justify-between text-[10px] text-slate-400">
                      <span className="flex items-center gap-1">
                        <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
                        Live in Passenger App & Transit Displays
                      </span>
                      <span className="font-mono">ID: {alert.id.substring(0, 8)}</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </div>
      </div>
    </div>
  )
}
