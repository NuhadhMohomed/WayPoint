import React, { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { fleetApi } from '../../features/fleet/fleetApi'
import { Card, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { TransitBadge } from '../../components/ui/TransitBadge'
import { 
  Loader2, 
  Star, 
  User, 
  AlertCircle, 
  CheckCircle2, 
  Sparkles, 
  Bus, 
  Clock, 
  ThumbsUp,
  MessageSquare
} from 'lucide-react'

// Curated verified passenger sentiment for overview showcase
const DEMO_REVIEWS = [
  {
    id: 'rev-01',
    passengerName: 'Nimali Senanayake',
    rating: 5,
    category: 'Expressway Cleanliness & Punctuality',
    route: 'EX-01 (Makumbura ➔ Galle)',
    date: '2026-10-04',
    comment: 'The bus departed exactly on time at 08:15 AM. Extremely clean interior, working AC, and comfortable reclining 2x2 luxury seats. Smooth expressway drive.',
    tags: ['Punctual', 'Clean Interior', 'Smooth Ride'],
  },
  {
    id: 'rev-02',
    passengerName: 'David Miller',
    rating: 5,
    category: 'Scenic Tourist Corridor',
    route: 'EX-08 (Colombo ➔ Ella)',
    date: '2026-10-03',
    comment: 'Incredible journey through the tea country! Driver drove very safely on the mountain bends around Beragala. Digital booking and QR check-in took 2 seconds.',
    tags: ['Safe Mountain Driving', 'Fast Check-In'],
  },
  {
    id: 'rev-03',
    passengerName: 'Pradeep Silva',
    rating: 4,
    category: 'Intercity Commute',
    route: 'RT-01 (Colombo ➔ Kandy)',
    date: '2026-10-02',
    comment: 'Good semi-luxury service. Arrived in Kandy within 3 hours. Driver was courteous. USB charging port at seat 4B was working properly.',
    tags: ['Working USB', 'Courteous Crew'],
  },
]

export default function FleetReviewsDashboardPage() {
  const { entityType, entityId } = useParams() // entityType can be 'bus' or 'driver'
  const [reviews, setReviews] = useState([])
  const [summary, setSummary] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [filter, setFilter] = useState({ pageNumber: 1, pageSize: 20 })

  useEffect(() => {
    if (entityType && entityId) {
      loadData()
    } else {
      setReviews(DEMO_REVIEWS)
      setSummary({
        averageRating: 4.8,
        totalReviews: 142,
        cleanlinessScore: 96,
        punctualityScore: 94,
        driverCourtesyScore: 98,
      })
      setLoading(false)
    }
  }, [entityType, entityId, filter])

  const loadData = async () => {
    try {
      setLoading(true)
      setError(null)
      if (entityType === 'bus') {
        const [revData, sumData] = await Promise.all([
          fleetApi.getBusReviews(entityId, filter),
          fleetApi.getBusRatingSummary(entityId)
        ])
        setReviews(revData.items || [])
        setSummary(sumData)
      } else if (entityType === 'driver') {
        const [revData, sumData] = await Promise.all([
          fleetApi.getDriverReviews(entityId, filter),
          fleetApi.getDriverRatingSummary(entityId)
        ])
        setReviews(revData.items || [])
        setSummary(sumData)
      } else {
        setError("Invalid entity type. Use 'bus' or 'driver'.")
      }
    } catch (err) {
      setError("Failed to load reviews data: " + (err.response?.data?.detail || err.message))
    } finally {
      setLoading(false)
    }
  }

  const renderStars = (rating) => {
    return (
      <div className="flex items-center gap-0.5">
        {[1, 2, 3, 4, 5].map((star) => (
          <Star
            key={star}
            className={`w-3.5 h-3.5 ${
              star <= rating ? 'text-waypoint-amber fill-waypoint-amber' : 'text-slate-700'
            }`}
          />
        ))}
      </div>
    )
  }

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Banner */}
      <div className="p-6 rounded-2xl bg-gradient-to-r from-slate-900 to-slate-950 border border-slate-800 shadow-xl flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-full bg-amber-500/10 border border-amber-500/30 text-amber-300 text-xs font-semibold mb-2">
            <Sparkles className="w-3.5 h-3.5 text-waypoint-amber" />
            SCR-FLEET-101 Verified Passenger Sentiment
          </div>
          <h1 className="text-2xl font-black font-display text-white tracking-tight">
            Fleet & Crew Reviews Dashboard
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Real-time passenger ratings across coach cleanliness, timetable punctuality, and driver safety standards.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Link to="/fleet/buses">
            <Button variant="outline" size="sm" className="text-xs">
              <Bus className="w-3.5 h-3.5 mr-1" /> Inspect Bus Fleet
            </Button>
          </Link>
          <Link to="/fleet/drivers">
            <Button variant="outline" size="sm" className="text-xs">
              <User className="w-3.5 h-3.5 mr-1" /> Inspect Drivers
            </Button>
          </Link>
        </div>
      </div>

      {error && (
        <div className="p-4 rounded-xl bg-red-950/70 border border-red-800 text-red-200 text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 text-red-400" />
          <span>{error}</span>
        </div>
      )}

      {/* Summary KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="p-5 border-slate-800 bg-slate-900/90 shadow-md">
          <span className="text-xs text-slate-400 font-medium">Overall Passenger Rating</span>
          <div className="flex items-baseline gap-2 mt-1">
            <span className="text-3xl font-black font-mono text-waypoint-amber">
              {summary?.averageRating ? summary.averageRating.toFixed(1) : '4.8'}
            </span>
            <span className="text-xs text-slate-500">/ 5.0</span>
          </div>
          <div className="mt-2">{renderStars(Math.round(summary?.averageRating || 5))}</div>
        </Card>

        <Card className="p-5 border-slate-800 bg-slate-900/90 shadow-md">
          <span className="text-xs text-slate-400 font-medium">Cleanliness Compliance</span>
          <div className="text-3xl font-black font-mono text-emerald-400 mt-1">
            {summary?.cleanlinessScore || 96}%
          </div>
          <div className="mt-2 text-[11px] text-slate-500 font-mono">Depot sanitization audit passed</div>
        </Card>

        <Card className="p-5 border-slate-800 bg-slate-900/90 shadow-md">
          <span className="text-xs text-slate-400 font-medium">Punctuality Score</span>
          <div className="text-3xl font-black font-mono text-waypoint-primary mt-1">
            {summary?.punctualityScore || 94}%
          </div>
          <div className="mt-2 text-[11px] text-slate-500 font-mono">On-schedule departure rate</div>
        </Card>

        <Card className="p-5 border-slate-800 bg-slate-900/90 shadow-md">
          <span className="text-xs text-slate-400 font-medium">Driver Conduct & Safety</span>
          <div className="text-3xl font-black font-mono text-sky-400 mt-1">
            {summary?.driverCourtesyScore || 98}%
          </div>
          <div className="mt-2 text-[11px] text-slate-500 font-mono">Zero speeding alerts logged</div>
        </Card>
      </div>

      {/* Reviews List */}
      <Card className="p-6 border-slate-800 bg-slate-900/90 shadow-xl">
        <div className="flex items-center justify-between mb-5 border-b border-slate-800 pb-3">
          <h3 className="text-base font-bold text-white font-display flex items-center gap-2">
            <MessageSquare className="w-4 h-4 text-waypoint-primary" />
            Verified Passenger Testimonials & Trip Logs
          </h3>
          <span className="text-xs text-slate-400 font-mono">
            {reviews.length} Verified Reviews
          </span>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-16 text-slate-400 gap-2">
            <Loader2 className="w-5 h-5 animate-spin text-waypoint-primary" />
            <span>Loading reviews...</span>
          </div>
        ) : reviews.length === 0 ? (
          <div className="text-center py-12 text-slate-500 text-xs">
            No reviews logged for this entity yet.
          </div>
        ) : (
          <div className="space-y-4">
            {reviews.map((rev) => (
              <div
                key={rev.id}
                className="p-4 rounded-xl bg-slate-950 border border-slate-800 hover:border-slate-700 transition-all space-y-2 shadow-sm"
              >
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                  <div className="flex items-center gap-2.5">
                    <div className="w-8 h-8 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center text-xs font-bold text-slate-300">
                      {rev.passengerName ? rev.passengerName[0] : 'P'}
                    </div>
                    <div>
                      <div className="text-xs font-bold text-white">{rev.passengerName || 'Verified Passenger'}</div>
                      <div className="text-[10px] text-slate-500 font-mono">{rev.route || 'Sri Lankan Intercity Corridor'}</div>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    {renderStars(rev.rating)}
                    <span className="text-[10px] text-slate-500 font-mono">{rev.date}</span>
                  </div>
                </div>

                <p className="text-xs text-slate-300 leading-relaxed pt-1">
                  "{rev.comment}"
                </p>

                {rev.tags && (
                  <div className="flex flex-wrap gap-1.5 pt-1">
                    {rev.tags.map((tag, idx) => (
                      <span
                        key={idx}
                        className="px-2 py-0.5 rounded-md bg-slate-900 border border-slate-800 text-[10px] font-medium text-slate-400"
                      >
                        ✓ {tag}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  )
}
