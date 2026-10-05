import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/transit_badge.dart';
import '../../../core/widgets/waypoint_card.dart';
import '../../fleet/data/fleet_api_service.dart';
import '../../booking/screens/seat_picker_screen.dart';
import '../models/journey_models.dart';
import '../widgets/ai_insights_banner.dart';

class JourneyComparisonScreen extends StatelessWidget {
  final String originCity;
  final String destinationCity;
  final DateTime travelDate;
  final List<JourneyCandidateModel> candidates;
  final String? agentReasoning;
  final bool isAiFallback;

  const JourneyComparisonScreen({
    super.key,
    required this.originCity,
    required this.destinationCity,
    required this.travelDate,
    required this.candidates,
    this.agentReasoning,
    this.isAiFallback = false,
  });

  @override
  Widget build(BuildContext context) {
    final currencyFormat = NumberFormat.currency(
      locale: 'en_LK',
      symbol: 'Rs. ',
      decimalDigits: 2,
    );
    final dateFormat = DateFormat('EEE, dd MMM yyyy');
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final textMuted = isDark ? const Color(0xFF94A3B8) : const Color(0xFF64748B);
    final showAiBanner = (agentReasoning != null && agentReasoning!.isNotEmpty) || isAiFallback;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          children: [
            Text(
              '$originCity → $destinationCity',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            Text(
              dateFormat.format(travelDate),
              style: TextStyle(fontSize: 11, color: textMuted),
            ),
          ],
        ),
      ),
      body: candidates.isEmpty
          ? EmptyStateView(
              icon: Icons.directions_bus_outlined,
              title: 'No Transit Options Found',
              description: 'Try adjusting your search date or filters for $originCity to $destinationCity.',
              actionLabel: 'Modify Search',
              onAction: () => Navigator.of(context).pop(),
            )
          : ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: (showAiBanner ? 1 : 0) + candidates.length,
              separatorBuilder: (_, __) => const SizedBox(height: 14),
              itemBuilder: (context, index) {
                if (showAiBanner && index == 0) {
                  return AiInsightsBanner(
                    agentReasoning: agentReasoning ?? '',
                    isAiFallback: isAiFallback,
                  );
                }
                final candidateIndex = showAiBanner ? index - 1 : index;
                final candidate = candidates[candidateIndex];
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
                                  color: isDark ? const Color(0xFF0F172A) : const Color(0xFFF1F5F9),
                                  borderRadius: BorderRadius.circular(6),
                                  border: Border.all(
                                    color: isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1),
                                  ),
                                ),
                                child: Text(
                                  candidate.routeNumber,
                                  style: TextStyle(
                                    color: isDark ? Colors.white : Colors.black87,
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
                              color: AppTheme.primaryColor,
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),

                      // Departure & Arrival Timeline
                      Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  formattedDep,
                                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  candidate.origin,
                                  style: TextStyle(color: textMuted, fontSize: 13),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ],
                            ),
                          ),
                          Column(
                            children: [
                              Text(
                                '${durationHours}h ${durationMins}m',
                                style: TextStyle(color: textMuted, fontSize: 11, fontWeight: FontWeight.w600),
                              ),
                              const SizedBox(height: 4),
                              Row(
                                children: [
                                  Container(width: 24, height: 1, color: AppTheme.primaryColor),
                                  const Padding(
                                    padding: EdgeInsets.symmetric(horizontal: 4),
                                    child: Icon(Icons.directions_bus, size: 14, color: AppTheme.primaryColor),
                                  ),
                                  Container(width: 24, height: 1, color: AppTheme.primaryColor),
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
                                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  candidate.destination,
                                  style: TextStyle(color: textMuted, fontSize: 13),
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
                                ? const Color(0xFF064E3B).withOpacity(0.2)
                                : const Color(0xFF7F1D1D).withOpacity(0.2),
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
                                      ? 'Safe ${candidate.transferBufferMinutes}m transfer window (≥ 20 min guarantee)'
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
                            style: TextStyle(
                              color: isDark ? const Color(0xFFCBD5E1) : const Color(0xFF475569),
                              fontSize: 12,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          Text(
                            '${candidate.availableSeats} Seats Available',
                            style: const TextStyle(
                              color: AppTheme.primaryColor,
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
                              Text(
                                'TOTAL TICKET FARE',
                                style: TextStyle(color: textMuted, fontSize: 10, fontWeight: FontWeight.bold),
                              ),
                              Text(
                                currencyFormat.format(candidate.totalFare),
                                style: const TextStyle(
                                  color: AppTheme.primaryColor,
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                          ElevatedButton.icon(
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppTheme.primaryColor,
                              foregroundColor: AppTheme.onPrimaryColor,
                              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                            ),
                            icon: const Icon(Icons.event_seat, size: 16),
                            label: const Text(
                              'Pick Seats',
                              style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold),
                            ),
                            onPressed: () {
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
