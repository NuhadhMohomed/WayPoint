import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import 'package:qr_flutter/qr_flutter.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/transit_badge.dart';
import '../models/booking_models.dart';

/// Authentic digital boarding pass card widget with scannable QR code and perforated divider.
/// Complies with Google Stitch Design System tokens and FR-BOOKING-003.
class QrTicketPassCard extends StatelessWidget {
  final DigitalTicketPass ticket;
  final VoidCallback? onCancelTap;
  final VoidCallback? onQrTap;

  const QrTicketPassCard({
    super.key,
    required this.ticket,
    this.onCancelTap,
    this.onQrTap,
  });

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('EEE, dd MMM yyyy');
    final timeFormat = DateFormat('hh:mm a');
    final currencyFormat = NumberFormat.currency(locale: 'en_LK', symbol: 'Rs. ', decimalDigits: 2);

    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF1E293B), // Slate 800
        borderRadius: BorderRadius.circular(20),
        boxShadow: const [
          BoxShadow(
            color: Color(0x26000000),
            blurRadius: 24,
            offset: Offset(0, 8),
          ),
        ],
        border: Border.all(color: const Color(0xFF334155), width: 1),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // 1. Header Section: Service code, bus class & status badge
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 20, 20, 16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: AppTheme.primaryColor.withValues(alpha: 0.18),
                        borderRadius: BorderRadius.circular(6),
                        border: Border.all(color: AppTheme.primaryColor.withValues(alpha: 0.5)),
                      ),
                      child: Text(
                        ticket.serviceCode,
                        style: const TextStyle(
                          color: Color(0xFF93C5FD),
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                          fontFamily: 'monospace',
                        ),
                      ),
                    ),
                    TransitBadge(
                      status: ticket.isBoarded ? TransitStatus.booked : TransitStatus.available,
                      customLabel: ticket.isBoarded ? 'BOARDED' : 'CONFIRMED',
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                Text(
                  ticket.routeTitle,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  '${ticket.busRegistration} • ${ticket.busClass}',
                  style: const TextStyle(
                    color: Color(0xFF94A3B8),
                    fontSize: 12,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),

          // 2. High-contrast QR Code Section
          GestureDetector(
            onTap: onQrTap ?? () => _showFullscreenQr(context),
            child: Container(
              margin: const EdgeInsets.symmetric(horizontal: 20),
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(16),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x1F000000),
                    blurRadius: 12,
                    offset: Offset(0, 4),
                  ),
                ],
              ),
              child: Column(
                children: [
                  QrImageView(
                    data: ticket.qrCodePayload,
                    version: QrVersions.auto,
                    size: 190.0,
                    backgroundColor: Colors.white,
                    eyeStyle: const QrEyeStyle(
                      eyeShape: QrEyeShape.square,
                      color: Color(0xFF0F172A),
                    ),
                    dataModuleStyle: const QrDataModuleStyle(
                      dataModuleShape: QrDataModuleShape.square,
                      color: Color(0xFF0F172A),
                    ),
                  ),
                  const SizedBox(height: 10),
                  const Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.zoom_in_rounded, size: 16, color: Color(0xFF475569)),
                      SizedBox(width: 6),
                      Text(
                        'Tap to enlarge for conductor scanning',
                        style: TextStyle(
                          color: Color(0xFF475569),
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),

          const SizedBox(height: 16),

          // 3. Perforated Ticket Divider with Cutout Punches
          _buildPerforatedDivider(),

          // 4. Passenger & Transit Boarding Details
          Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              children: [
                // Origin and Destination Timeline
                Row(
                  children: [
                    Column(
                      children: [
                        const Icon(Icons.radio_button_checked, size: 16, color: Color(0xFF22C55E)),
                        Container(width: 1.5, height: 32, color: const Color(0xFF334155)),
                        const Icon(Icons.location_on, size: 16, color: AppTheme.secondaryColor),
                      ],
                    ),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                ticket.originCity,
                                style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.bold),
                              ),
                              Text(
                                timeFormat.format(ticket.departureTime),
                                style: const TextStyle(color: Color(0xFF22C55E), fontSize: 13, fontWeight: FontWeight.bold),
                              ),
                            ],
                          ),
                          const SizedBox(height: 18),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                ticket.destinationCity,
                                style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.bold),
                              ),
                              Text(
                                timeFormat.format(ticket.arrivalTime),
                                style: const TextStyle(color: AppTheme.secondaryColor, fontSize: 13, fontWeight: FontWeight.bold),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                // Boarding Landmark
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: const Color(0xFF0F172A),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: const Color(0xFF334155)),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.directions_bus_outlined, color: Color(0xFF94A3B8), size: 16),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          'Boarding: ${ticket.boardingPointName}',
                          style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 11),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),

                // Booking Reference & Seats
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'BOOKING REFERENCE',
                          style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold, letterSpacing: 0.5),
                        ),
                        const SizedBox(height: 4),
                        GestureDetector(
                          onTap: () {
                            Clipboard.setData(ClipboardData(text: ticket.bookingReference));
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(
                                content: Text('Copied reference ${ticket.bookingReference} to clipboard'),
                                behavior: SnackBarBehavior.floating,
                                duration: const Duration(seconds: 2),
                              ),
                            );
                          },
                          child: Row(
                            children: [
                              Text(
                                ticket.bookingReference,
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 16,
                                  fontWeight: FontWeight.w800,
                                  fontFamily: 'monospace',
                                ),
                              ),
                              const SizedBox(width: 6),
                              const Icon(Icons.copy_rounded, size: 14, color: AppTheme.secondaryColor),
                            ],
                          ),
                        ),
                      ],
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        const Text(
                          'ASSIGNED SEATS',
                          style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold, letterSpacing: 0.5),
                        ),
                        const SizedBox(height: 4),
                        Row(
                          children: ticket.seatNumbers.map((s) {
                            return Container(
                              margin: const EdgeInsets.only(left: 4),
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                              decoration: BoxDecoration(
                                color: AppTheme.primaryColor,
                                borderRadius: BorderRadius.circular(4),
                              ),
                              child: Text(
                                s,
                                style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.bold),
                              ),
                            );
                          }).toList(),
                        ),
                      ],
                    ),
                  ],
                ),
                const SizedBox(height: 14),

                // Passenger & Fare
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('PASSENGER', style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold)),
                        const SizedBox(height: 2),
                        Text(ticket.passengerName, style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w600)),
                      ],
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        const Text('TRAVEL DATE', style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold)),
                        const SizedBox(height: 2),
                        Text(dateFormat.format(ticket.departureTime), style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w600)),
                      ],
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        const Text('PAID TOTAL', style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold)),
                        const SizedBox(height: 2),
                        Text(currencyFormat.format(ticket.totalFare), style: const TextStyle(color: Color(0xFF22C55E), fontSize: 13, fontWeight: FontWeight.w800)),
                      ],
                    ),
                  ],
                ),

                const Divider(color: Color(0xFF334155), height: 28),

                // Cryptographic HMAC Security Footnote
                Row(
                  children: [
                    const Icon(Icons.verified_outlined, size: 14, color: Color(0xFF22C55E)),
                    const SizedBox(width: 6),
                    const Expanded(
                      child: Text(
                        'HMAC-SHA256 Signed • Offline Boarding Guaranteed',
                        style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.w500),
                      ),
                    ),
                    if (ticket.isUpcoming && onCancelTap != null)
                      GestureDetector(
                        onTap: onCancelTap,
                        child: const Text(
                          'Cancel / Refund',
                          style: TextStyle(
                            color: AppTheme.errorColor,
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            decoration: TextDecoration.underline,
                          ),
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// Builds an authentic perforated ticket divider with half-circle notches on both sides.
  Widget _buildPerforatedDivider() {
    return Row(
      children: [
        // Left notch
        Container(
          width: 14,
          height: 24,
          decoration: const BoxDecoration(
            color: Color(0xFF0F172A), // Matches background
            borderRadius: BorderRadius.horizontal(right: Radius.circular(14)),
          ),
        ),
        // Dashed line
        Expanded(
          child: LayoutBuilder(
            builder: (context, constraints) {
              const dashWidth = 6.0;
              const dashSpace = 4.0;
              final dashCount = (constraints.constrainWidth() / (dashWidth + dashSpace)).floor();
              return Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: List.generate(dashCount, (_) {
                  return const SizedBox(
                    width: dashWidth,
                    height: 1.2,
                    child: DecoratedBox(decoration: BoxDecoration(color: Color(0xFF475569))),
                  );
                }),
              );
            },
          ),
        ),
        // Right notch
        Container(
          width: 14,
          height: 24,
          decoration: const BoxDecoration(
            color: Color(0xFF0F172A), // Matches background
            borderRadius: BorderRadius.horizontal(left: Radius.circular(14)),
          ),
        ),
      ],
    );
  }

  /// Displays the QR code in a high-brightness modal for effortless conductor scanning.
  void _showFullscreenQr(BuildContext context) {
    showDialog(
      context: context,
      builder: (ctx) => Dialog(
        backgroundColor: Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        ticket.bookingReference,
                        style: const TextStyle(
                          color: Color(0xFF0F172A),
                          fontWeight: FontWeight.w800,
                          fontSize: 18,
                          fontFamily: 'monospace',
                        ),
                      ),
                      Text(
                        'Service: ${ticket.serviceCode}',
                        style: const TextStyle(color: Color(0xFF64748B), fontSize: 12),
                      ),
                    ],
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, color: Color(0xFF64748B)),
                    onPressed: () => Navigator.of(ctx).pop(),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              QrImageView(
                data: ticket.qrCodePayload,
                version: QrVersions.auto,
                size: 260.0,
                backgroundColor: Colors.white,
                eyeStyle: const QrEyeStyle(eyeShape: QrEyeShape.square, color: Colors.black),
                dataModuleStyle: const QrDataModuleStyle(dataModuleShape: QrDataModuleShape.square, color: Colors.black),
              ),
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: const Color(0xFFF1F5F9),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(Icons.qr_code_scanner, size: 16, color: Color(0xFF0F172A)),
                    const SizedBox(width: 8),
                    Text(
                      'Ready for Conductor Scanner (${ticket.seatNumbers.join(", ")})',
                      style: const TextStyle(color: Color(0xFF0F172A), fontSize: 12, fontWeight: FontWeight.bold),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
