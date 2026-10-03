import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/transit_badge.dart';
import '../../../core/widgets/waypoint_card.dart';
import '../../fleet/data/fleet_api_service.dart';
import '../../fleet/screens/seat_picker_screen.dart';
import '../models/journey_models.dart';

/// MOB-04: Journey Comparison Cards & Safe Buffer
/// Stitch Screen ID: aa124497024b48a3adc01888fed1a5a3
/// Component 1: Journey Planning & Route Catalogue (Sethum)
class JourneyComparisonScreen extends StatelessWidget {
  final String originCity;
  final String destinationCity;
  final DateTime travelDate;
  final List<JourneyCandidateModel> candidates;

  const JourneyComparisonScreen({
    super.key,
    required this.originCity,
    required this.destinationCity,
    required this.travelDate,
    required this.candidates,
  });

  @override
  Widget build(BuildContext context) {
    final currencyFormat = NumberFormat.currency(
      locale: 'en_LK',
      symbol: 'Rs. ',
      decimalDigits: 2,
    );
    final dateFormat = DateFormat('EEE, dd MMM yyyy');

    return Scaffold(
      backgroundColor: AppTheme.darkBackground,
      appBar: AppBar(
        backgroundColor: AppTheme.darkBackground,
        foregroundColor: Colors.white,
        title: Column(
          children: [
            Text(
              '$originCity → $destinationCity',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            Text(
              dateFormat.format(travelDate),
              style: const TextStyle(fontSize: 11, color: Color(0xFF94A3B8)),
            ),
          ],
        ),
      ),
      body: candidates.isEmpty
          ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.directions_bus_outlined, size: 64, color: Color(0xFF64748B)),
                  const SizedBox(height: 16),
                  const Text(
                    'No Transit Options Found',
                    style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Try adjusting your search date or filters for $originCity to $destinationCity.',
                    style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            )
          : ListView.separated(
              padding: const EdgeInsets.all(20),
              itemCount: candidates.length,
              separatorBuilder: (_, __) => const SizedBox(height: 16),
              itemBuilder: (context, index) {
                final candidate = candidates[index];
                final durationHours = candidate.durationMinutes ~/ 60;
                final durationMins = candidate.durationMinutes % 60;
                final formattedDep = DateFormat('hh:mm a').format(candidate.departureTime);
                final formattedArr = DateFormat('hh:mm a').format(candidate.arrivalTime);

                // Transfer window safety indicator (BR-TRANSFER-001)
                final isSafeTransfer = candidate.transferBufferMinutes != null &&
                    candidate.transferBufferMinutes! >= 20;

                return WayPointCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Header Row: Route Number & Match Score Badge
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Row(
                            children: [
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                decoration: BoxDecoration(
                                  color: const Color(0xFF0F172A),
                                  borderRadius: BorderRadius.circular(6),
                                  border: Border.all(color: const Color(0xFF334155)),
                                ),
                                child: Text(
                                  candidate.routeNumber,
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 12,
                                    fontWeight: FontWeight.bold,
                                    fontFamily: 'monospace',
                                  ),
                                ),
                              ),
                              const SizedBox(width: 8),
                              if (candidate.isConnecting)
                                const TransitBadge(
                                  status: TransitStatus.delayed,
                                  customLabel: 'Connecting',
                                )
                              else
                                const TransitBadge(
                                  status: TransitStatus.express,
                                  customLabel: 'Direct Express',
                                ),
                            ],
                          ),
                          Text(
                            '${(candidate.matchScore * 100).toInt()}% Match',
                            style: const TextStyle(
                              color: Color(0xFF818CF8),
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),

                      // Origin & Destination Timeline
                      Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  formattedDep,
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 17,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  candidate.origin,
                                  style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ],
                            ),
                          ),
                          Column(
                            children: [
                              Text(
                                '${durationHours}h ${durationMins}m',
                                style: const TextStyle(
                                  color: Color(0xFF94A3B8),
                                  fontSize: 11,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Row(
                                children: [
                                  Container(width: 24, height: 1, color: const Color(0xFF475569)),
                                  const Icon(Icons.directions_bus, size: 14, color: AppTheme.primaryColor),
                                  Container(width: 24, height: 1, color: const Color(0xFF475569)),
                                ],
                              ),
                            ],
                          ),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              children: [
                                Text(
                                  formattedArr,
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 17,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  candidate.destination,
                                  style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                                  overflow: TextOverflow.ellipsis,
                                  textAlign: TextAlign.right,
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),

                      // Connecting Transfer Hub & Window (BR-TRANSFER-001)
                      if (candidate.isConnecting && candidate.transferBufferMinutes != null) ...[
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                          decoration: BoxDecoration(
                            color: isSafeTransfer
                                ? const Color(0xFF064E3B).withAlpha(102)
                                : const Color(0xFF7F1D1D).withAlpha(102),
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(
                              color: isSafeTransfer ? const Color(0xFF059669) : const Color(0xFFDC2626),
                            ),
                          ),
                          child: Row(
                            children: [
                              Icon(
                                isSafeTransfer ? Icons.check_circle : Icons.warning_amber_rounded,
                                size: 16,
                                color: isSafeTransfer ? const Color(0xFF34D399) : const Color(0xFFF87171),
                              ),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  isSafeTransfer
                                      ? 'Safe ${candidate.transferBufferMinutes}m transfer window (>= 20 min rule)'
                                      : 'Transfer window (${candidate.transferBufferMinutes}m) is below 20 min safety threshold',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.w600,
                                    color: isSafeTransfer ? const Color(0xFF34D399) : const Color(0xFFF87171),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 14),
                      ],

                      // Bus Class & Seats left
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            candidate.busClass,
                            style: const TextStyle(
                              color: Color(0xFFCBD5E1),
                              fontSize: 12,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          Text(
                            '${candidate.availableSeats} Seats Available',
                            style: const TextStyle(
                              color: Color(0xFF34D399),
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),

                      // Price and Select CTA
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'TOTAL TICKET FARE',
                                style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold),
                              ),
                              Text(
                                currencyFormat.format(candidate.totalFare),
                                style: const TextStyle(
                                  color: Color(0xFF22C55E),
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                          ElevatedButton.icon(
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppTheme.primaryColor,
                              foregroundColor: Colors.white,
                              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                            ),
                            icon: const Icon(Icons.event_seat, size: 16),
                            label: const Text(
                              'Pick Seats',
                              style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold),
                            ),
                            onPressed: () {
                              // Handoff to Component 2 (Nuhadh) - MOB-05 SeatPickerScreen
                              Navigator.of(context).push(
                                MaterialPageRoute(
                                  builder: (_) => SeatPickerScreen(
                                    serviceId: candidate.serviceId,
                                    apiService: FleetApiService(),
                                  ),
                                ),
                              );
                            },
                          ),
                        ],
                      ),
                    ],
                  ),
                );
              },
            ),
    );
  }
}
