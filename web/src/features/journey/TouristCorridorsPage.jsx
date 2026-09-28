import React from 'react'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import { Link } from 'react-router-dom'
import { 
  Compass, 
  MapPin, 
  Clock, 
  ArrowRight, 
  Mountain, 
  Camera, 
  Sparkles, 
  Sun,
  ShieldCheck,
  Calendar
} from 'lucide-react'

const CORRIDORS = [
  {
    id: 'corridor-ella',
    title: 'Tea Country & Highland Express',
    routeNumber: 'EX-08',
    origin: 'Colombo (Bastian Hill)',
    destination: 'Ella Town Terminal',
    distanceKm: 215,
    avgDuration: '6h 00m',
    highlightTag: 'Most Popular Tourist Route',
    description:
      'Winds through Ratnapura gem valleys, steep Balangoda ascents, and the misty hills of Bandarawela before descending into Ella.',
    attractions: [
      { name: 'Nine Arch Bridge', category: 'Colonial Architecture', landmark: 'Demodara / Ella' },
      { name: "Little Adam's Peak", category: 'Panoramic Trekking', landmark: 'Passara Road' },
      { name: 'Ravana Waterfall', category: 'Scenic Cascade', landmark: 'Ella-Wellawaya Road' },
      { name: "Lipton's Seat", category: 'Tea Heritage Vista', landmark: 'Dambatenne / Haputale' },
    ],
    recommendedDeparture: '06:30 AM (Morning Vista Express)',
    fareLkr: 2400.0,
    accentColor: 'from-amber-500/20 to-emerald-500/10 border-amber-500/30',
  },
  {
    id: 'corridor-kandy',
    title: 'The Royal Kingdom Corridor',
    routeNumber: 'RT-01',
    origin: 'Colombo (Central Super)',
    destination: 'Kandy Goods Shed',
    distanceKm: 115,
    avgDuration: '3h 15m',
    highlightTag: 'UNESCO Cultural Hub',
    description:
      'The historic artery connecting the western maritime metropolis with the sacred hill capital, climbing past Kadugannawa Pass.',
    attractions: [
      { name: 'Temple of the Tooth Relic', category: 'World Heritage Sacred Site', landmark: 'Kandy Lake' },
      { name: 'Royal Botanical Gardens', category: 'Flora Sanctuary', landmark: 'Peradeniya' },
      { name: 'Kadugannawa Rock Tunnel', category: 'Historic Engineering', landmark: 'A1 Highway Pass' },
    ],
    recommendedDeparture: '07:00 AM (Intercity Super Line)',
    fareLkr: 1100.0,
    accentColor: 'from-blue-500/20 to-indigo-500/10 border-blue-500/30',
  },
  {
    id: 'corridor-galle',
    title: 'Southern Coastal Highway',
    routeNumber: 'EX-01',
    origin: 'Makumbura Multimodal Hub',
    destination: 'Galle Fort Terminal',
    distanceKm: 118,
    avgDuration: '1h 30m',
    highlightTag: 'Rapid Expressway',
    description:
      'High-speed transit along the Southern Expressway directly to the 17th-century colonial fortress and coastal waters.',
    attractions: [
      { name: 'Galle Dutch Fort', category: 'UNESCO Colonial Fortress', landmark: 'Ramparts & Lighthouse' },
      { name: 'Unawatuna Bay', category: 'Recreation & Coral Reefs', landmark: 'Matara Road' },
      { name: 'Martin Wickramasinghe Museum', category: 'Literary Heritage', landmark: 'Koggala' },
    ],
    recommendedDeparture: '08:15 AM (Express Coach)',
    fareLkr: 1250.0,
    accentColor: 'from-emerald-500/20 to-teal-500/10 border-emerald-500/30',
  },
  {
    id: 'corridor-jaffna',
    title: 'The Northern Heritage Corridor',
    routeNumber: 'RT-87',
    origin: 'Colombo Bastian Mawatha',
    destination: 'Jaffna Central Stand',
    distanceKm: 395,
    avgDuration: '8h 00m',
    highlightTag: 'Cross-Island Odyssey',
    description:
      'Spans the central plains through the ancient kingdom of Anuradhapura and across the historic Elephant Pass causeway into the Jaffna peninsula.',
    attractions: [
      { name: 'Nallur Kandaswamy Kovil', category: 'Historic Dravidian Temple', landmark: 'Point Pedro Road' },
      { name: 'Jaffna Star Fort', category: 'Coastal Fortification', landmark: 'Jaffna Lagoon' },
      { name: 'Elephant Pass Causeway', category: 'Geographic Isthmus', landmark: 'A9 Highway' },
    ],
    recommendedDeparture: '20:00 PM (Night Luxury Sleeper)',
    fareLkr: 3200.0,
    accentColor: 'from-purple-500/20 to-rose-500/10 border-purple-500/30',
  },
]

export function TouristCorridorsPage() {
  return (
    <div className="space-y-6">
      {/* Intro Header */}
      <div className="p-6 rounded-2xl bg-gradient-to-r from-indigo-950/60 via-slate-950 to-slate-900 border border-indigo-500/20 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-2">
            <Sparkles className="w-4 h-4 text-waypoint-amber" />
            <span className="text-xs font-semibold text-waypoint-amber uppercase tracking-wider">
              Scenic Travel Showcase (WEB-03)
            </span>
          </div>
          <h2 className="text-xl font-bold font-display text-white">
            Sri Lankan Intercity Tourist Corridors
          </h2>
          <p className="text-xs text-slate-400 mt-1 max-w-2xl">
            WayPoint curates high-demand travel corridors with scheduled departures, stop sequence milestones, and attraction markers designed for international and local travelers.
          </p>
        </div>

        <Link to="/routes/scheduler">
          <Button size="sm" className="flex items-center gap-2 whitespace-nowrap">
            <Calendar className="w-4 h-4" />
            View Service Timetables
          </Button>
        </Link>
      </div>

      {/* Corridor Cards */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {CORRIDORS.map((corridor) => (
          <Card
            key={corridor.id}
            className={`p-6 border bg-gradient-to-br ${corridor.accentColor} bg-slate-900/90 relative overflow-hidden flex flex-col justify-between`}
          >
            <div>
              <div className="flex items-center justify-between mb-3">
                <span className="px-2.5 py-1 text-xs font-mono font-bold rounded bg-slate-950 text-white border border-slate-800">
                  {corridor.routeNumber}
                </span>
                <span className="text-[11px] font-semibold text-waypoint-amber px-2.5 py-0.5 rounded-full bg-amber-500/10 border border-amber-500/20">
                  {corridor.highlightTag}
                </span>
              </div>

              <h3 className="text-lg font-bold font-display text-white mb-1">
                {corridor.title}
              </h3>

              <div className="flex items-center gap-2 text-sm text-slate-300 font-semibold mb-3">
                <span>{corridor.origin}</span>
                <ArrowRight className="w-4 h-4 text-slate-500" />
                <span>{corridor.destination}</span>
              </div>

              <p className="text-xs text-slate-400 leading-relaxed mb-4">
                {corridor.description}
              </p>

              {/* Transit Specs */}
              <div className="grid grid-cols-3 gap-2 p-3 rounded-xl bg-slate-950/80 border border-slate-800/80 mb-4 text-center">
                <div>
                  <div className="text-[10px] text-slate-500 uppercase font-medium">Distance</div>
                  <div className="text-xs font-bold text-white font-mono mt-0.5">{corridor.distanceKm} km</div>
                </div>
                <div>
                  <div className="text-[10px] text-slate-500 uppercase font-medium">Transit Time</div>
                  <div className="text-xs font-bold text-white font-mono mt-0.5">{corridor.avgDuration}</div>
                </div>
                <div>
                  <div className="text-[10px] text-slate-500 uppercase font-medium">From Rate</div>
                  <div className="text-xs font-bold text-emerald-400 font-mono mt-0.5">Rs. {corridor.fareLkr}</div>
                </div>
              </div>

              {/* Highlights List */}
              <div className="space-y-2 mb-4">
                <div className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
                  <Camera className="w-3.5 h-3.5 text-indigo-400" />
                  Key Attraction Stops:
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  {corridor.attractions.map((att, idx) => (
                    <div
                      key={idx}
                      className="p-2 rounded-lg bg-slate-950/50 border border-slate-800/60 text-xs"
                    >
                      <div className="font-semibold text-slate-200">{att.name}</div>
                      <div className="text-[10px] text-slate-500">{att.landmark}</div>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {/* Bottom Actions */}
            <div className="pt-4 border-t border-slate-800/80 flex items-center justify-between">
              <div className="text-[11px] text-slate-400 flex items-center gap-1">
                <Clock className="w-3 h-3 text-slate-500" />
                <span>Prime departure: <strong className="text-slate-200">{corridor.recommendedDeparture}</strong></span>
              </div>
              <Link to="/routes/catalog">
                <Button variant="outline" size="sm" className="text-xs">
                  Inspect Route
                </Button>
              </Link>
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}

