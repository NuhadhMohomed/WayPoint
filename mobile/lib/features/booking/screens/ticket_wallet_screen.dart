import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/transit_badge.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../models/booking_models.dart';
import '../widgets/qr_ticket_pass_card.dart';

/// MOB-07: Digital QR Ticket Wallet & HMAC Pass
/// Stitch Screen ID: ce33fd7d93b94ddf8f262655cc1ff1b1
/// Component 3: Booking, Ticketing & Passenger Options (Mithila)
class TicketWalletScreen extends StatefulWidget {
  final List<DigitalTicketPass>? initialTickets;

  const TicketWalletScreen({
    super.key,
    this.initialTickets,
  });

  @override
  State<TicketWalletScreen> createState() => _TicketWalletScreenState();
}

class _TicketWalletScreenState extends State<TicketWalletScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  late List<DigitalTicketPass> _tickets;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _tickets = widget.initialTickets ?? [
      DigitalTicketPass.sampleColomboToElla(),
      DigitalTicketPass.sampleColomboToKandy(),
    ];
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  List<DigitalTicketPass> get _activeTickets =>
      _tickets.where((t) => t.isUpcoming).toList();

  List<DigitalTicketPass> get _pastTickets =>
      _tickets.where((t) => !t.isUpcoming).toList();

  void _showCancellationModal(DigitalTicketPass ticket) {
    // Determine departure offset hours to calculate tiered refund percentage
    final hoursUntilDeparture =
        ticket.departureTime.difference(DateTime.now()).inHours;
    double refundPercent;
    if (hoursUntilDeparture > 24) {
      refundPercent = 0.90; // 90% refund
    } else if (hoursUntilDeparture >= 12) {
      refundPercent = 0.50; // 50% refund
    } else {
      refundPercent = 0.0; // Non-refundable
    }
    final refundAmount = ticket.totalFare * refundPercent;

    showModalBottomSheet(
      context: context,
      backgroundColor: const Color(0xFF1E293B),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Cancel Booking & Refund',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close, color: Color(0xFF94A3B8)),
                  onPressed: () => Navigator.of(ctx).pop(),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Text(
              'Trip: ${ticket.routeTitle} (${ticket.serviceCode})',
              style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 13),
            ),
            const SizedBox(height: 16),

            // Tiered Refund Policy Breakdown (BR-REFUND-001)
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: const Color(0xFF0F172A),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFF334155)),
              ),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('Original Fare', style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13)),
                      Text('Rs. ${ticket.totalFare.toStringAsFixed(2)}', style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.bold)),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Departure Offset ($hoursUntilDeparture hrs remaining)', style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13)),
                      Text('${(refundPercent * 100).toInt()}% Refund Policy', style: const TextStyle(color: AppTheme.secondaryColor, fontSize: 13, fontWeight: FontWeight.bold)),
                    ],
                  ),
                  const Divider(color: Color(0xFF334155), height: 20),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('Estimated Refund', style: TextStyle(color: Colors.white, fontSize: 14, fontWeight: FontWeight.bold)),
                      Text(
                        'Rs. ${refundAmount.toStringAsFixed(2)}',
                        style: const TextStyle(color: Color(0xFF22C55E), fontSize: 16, fontWeight: FontWeight.w800),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            WayPointButton(
              text: 'Confirm Cancellation (MOB-08 Policy)',
              variant: WayPointButtonVariant.danger,
              icon: Icons.cancel_outlined,
              onPressed: () {
                Navigator.of(ctx).pop();
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    backgroundColor: AppTheme.errorColor,
                    content: Text(
                      'Booking ${ticket.bookingReference} cancelled. Refund of Rs. ${refundAmount.toStringAsFixed(2)} initiated.',
                    ),
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A), // Slate 900
      appBar: AppBar(
        backgroundColor: const Color(0xFF0F172A),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new, color: Colors.white, size: 20),
          onPressed: () => Navigator.of(context).pop(),
        ),
        title: const Text(
          'Digital Ticket Wallet',
          style: TextStyle(
            color: Colors.white,
            fontSize: 18,
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: const [
          Padding(
            padding: EdgeInsets.only(right: 16),
            child: Center(
              child: TransitBadge(
                status: TransitStatus.luxury,
                customLabel: 'MOB-07',
              ),
            ),
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: AppTheme.primaryColor,
          indicatorWeight: 3,
          labelColor: Colors.white,
          unselectedLabelColor: const Color(0xFF64748B),
          tabs: [
            Tab(text: 'Active Passes (${_activeTickets.length})'),
            Tab(text: 'Past Trips (${_pastTickets.length})'),
          ],
        ),
      ),
      body: Column(
        children: [
          // Offline Sync Status Banner
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            color: const Color(0xFF05230F),
            child: const Row(
              children: [
                Icon(Icons.cloud_done_rounded, color: Color(0xFF22C55E), size: 16),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Offline Ready — Passes are cryptographically signed & stored on device.',
                    style: TextStyle(color: Color(0xFF86EFAC), fontSize: 11, fontWeight: FontWeight.w500),
                  ),
                ),
              ],
            ),
          ),

          // Tab Views with Tickets
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                // 1. Active Passes Tab
                _buildTicketsList(_activeTickets, isUpcoming: true),

                // 2. Past Trips Tab
                _buildTicketsList(_pastTickets, isUpcoming: false),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTicketsList(List<DigitalTicketPass> tickets, {required bool isUpcoming}) {
    if (tickets.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              isUpcoming ? Icons.confirmation_number_outlined : Icons.history_rounded,
              color: const Color(0xFF334155),
              size: 64,
            ),
            const SizedBox(height: 16),
            Text(
              isUpcoming ? 'No Active Tickets' : 'No Travel History',
              style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 6),
            Text(
              isUpcoming
                  ? 'Reserve your seat to view digital boarding passes here.'
                  : 'Completed trips will be archived here automatically.',
              style: const TextStyle(color: Color(0xFF64748B), fontSize: 13),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: tickets.length,
      itemBuilder: (context, index) {
        final ticket = tickets[index];
        return Padding(
          padding: const EdgeInsets.only(bottom: 20),
          child: QrTicketPassCard(
            ticket: ticket,
            onCancelTap: isUpcoming ? () => _showCancellationModal(ticket) : null,
          ),
        );
      },
    );
  }
}
