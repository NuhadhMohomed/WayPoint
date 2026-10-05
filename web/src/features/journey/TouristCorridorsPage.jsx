import React from 'react'
import { Card } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { TransitBadge } from '../../components/ui/TransitBadge'
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
  Calendar,
  Waves
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
  },
  {
    id: 'corridor-jaffna',
    title: 'Northern Peninsula Line',
    routeNumber: 'RT-87',
    origin: 'Colombo Bastian Mawatha',
    destination: 'Jaffna Central Stand',
    distanceKm: 395,
    avgDuration: '8h 00m',
    highlightTag: 'Heritage & Coastline',
    description:
      'Connects Colombo to the historic Jaffna peninsula via Anuradhapura, crossing Elephant Pass into the northern heartland.',
    attractions: [
      { name: 'Nallur Kandaswamy Kovil', category: 'Historic Hindu Temple', landmark: 'Nallur' },
      { name: 'Jaffna Fort', category: 'Dutch Colonial Fortress', landmark: 'Jaffna Town' },
      { name: 'Elephant Pass Causeway', category: 'Historic Gateway', landmark: 'Peninsula Neck' },
    ],
    recommendedDeparture: '20:00 PM (Night Cruiser Luxury)',
    fareLkr: 3200.0,
  },
]

export function TouristCorridorsPage() {
  return (
    <div className="space-y-6">
      {/* Intro Banner */}
      <div className="p-6 rounded-2xl bg-gradient-to-r from-slate-900 to-slate-950 border border-slate-800 shadow-xl flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-full bg-amber-500/10 border border-amber-500/30 text-amber-300 text-xs font-semibold mb-2">
            <Sun className="w-3.5 h-3.5" />
            Curated Sri Lankan Tourist Corridors
          </div>
          <h2 className="text-xl font-black text-white tracking-tight font-display">
            Scenic Highway & Rail Connections
          </h2>
          <p className="text-xs text-slate-400 mt-1 max-w-2xl leading-relaxed">
            Standardized intercity tourist corridors with verified intermediate sightseeing waypoints, luxury coach timetables, and direct booking integrations.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Link to="/routes/scheduler">
            <Button variant="primary" size="sm" className="font-bold gap-1.5 shadow-lg shadow-waypoint-primary/20">
              <Calendar className="w-3.5 h-3.5" />
              View Departure Schedules
            </Button>
          </Link>
        </div>
      </div>

      {/* Corridors Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
        {CORRIDORS.map((corridor) => (
          <Card
            key={corridor.id}
            className="p-6 flex flex-col justify-between hover:border-slate-700 transition-all shadow-lg group relative overflow-hidden"
          >
            <div>
              {/* Header */}
              <div className="flex items-start justify-between gap-3 mb-3">
                <div className="flex items-center gap-2">
                  <span className="px-2.5 py-1 rounded-lg bg-waypoint-primary/10 text-waypoint-primary border border-waypoint-primary/30 font-mono font-bold text-xs">
                    {corridor.routeNumber}
                  </span>
                  <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-500/10 text-amber-300 border border-amber-500/30">
                    {corridor.highlightTag}
                  </span>
                </div>
                <div className="text-xs font-mono font-semibold text-waypoint-primary">
                  LKR {corridor.fareLkr.toLocaleString()}
                </div>
              </div>

              <h3 className="text-lg font-bold text-white mb-1.5 font-display">
                {corridor.title}
              </h3>

              <div className="flex items-center gap-2 text-xs font-semibold text-slate-300 mb-2">
                <span>{corridor.origin}</span>
                <ArrowRight className="w-3.5 h-3.5 text-waypoint-primary flex-shrink-0" />
                <span>{corridor.destination}</span>
              </div>

              <p className="text-xs text-slate-400 leading-relaxed mb-4">
                {corridor.description}
              </p>

              {/* Waypoints & Attractions */}
              <div className="mb-4">
                <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 block mb-2">
                  Sightseeing Highlights & Waypoints:
                </span>
                <div className="grid grid-cols-2 gap-2">
                  {corridor.attractions.map((att, idx) => (
                    <div
                      key={idx}
                      className="p-2 rounded-lg bg-slate-950/80 border border-slate-800 text-[11px]"
                    >
                      <div className="font-semibold text-slate-200 truncate">{att.name}</div>
                      <div className="text-[10px] text-slate-500 truncate">{att.category}</div>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {/* Footer metrics */}
            <div className="pt-4 border-t border-slate-800/80 flex items-center justify-between text-xs text-slate-400">
              <div className="flex items-center gap-3 font-mono">
                <span className="flex items-center gap-1">
                  <MapPin className="w-3.5 h-3.5 text-slate-500" />
                  {corridor.distanceKm} km
                </span>
                <span>•</span>
                <span className="flex items-center gap-1">
                  <Clock className="w-3.5 h-3.5 text-slate-500" />
                  {corridor.avgDuration}
                </span>
              </div>

              <Link
                to="/routes/scheduler"
                className="text-waypoint-primary font-bold text-xs hover:underline flex items-center gap-1"
              >
                Book Seats <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}
