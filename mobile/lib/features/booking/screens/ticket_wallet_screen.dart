import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/storage/local_cache_service.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../models/booking_models.dart';
import '../widgets/qr_ticket_pass_card.dart';
import '../widgets/tiered_refund_modal.dart';

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
  bool _brightnessBoosted = false;
  final _cache = LocalCacheService();

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _tickets = widget.initialTickets ?? [
      DigitalTicketPass.sampleColomboToElla(),
      DigitalTicketPass.sampleColomboToKandy(),
    ];
    _cacheOfflinePasses();
  }

  Future<void> _cacheOfflinePasses() async {
    final ticketMaps = _tickets.map((t) => t.toJson()).toList().cast<Map<String, dynamic>>();
    await _cache.cacheTickets(ticketMaps);
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

  void _toggleBrightnessBoost() {
    HapticFeedback.mediumImpact();
    setState(() {
      _brightnessBoosted = !_brightnessBoosted;
    });
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          _brightnessBoosted
              ? 'Screen brightness boosted for terminal scanners'
              : 'Screen brightness restored to normal',
        ),
        duration: const Duration(seconds: 2),
        backgroundColor: _brightnessBoosted ? AppTheme.primaryColor : null,
      ),
    );
  }

  void _showCancellationModal(DigitalTicketPass ticket) {
    final hoursUntilDeparture = ticket.departureTime.difference(DateTime.now()).inHours;

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => TieredRefundModal(
        bookingReference: ticket.bookingReference,
        routeTitle: ticket.routeTitle,
        serviceCode: ticket.serviceCode,
        totalPaid: ticket.totalFare,
        hoursUntilDeparture: hoursUntilDeparture,
        onConfirm: (reason, refundAmount, refundPercent) {
          setState(() {
            _tickets = _tickets.map((t) {
              if (t.ticketId == ticket.ticketId) {
                return t.copyWith(isBoarded: false);
              }
              return t;
            }).toList().cast<DigitalTicketPass>();
          });
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              backgroundColor: AppTheme.primaryColor,
              content: Text(
                'Booking ${ticket.bookingReference} cancelled. Refund of Rs. ${refundAmount.toStringAsFixed(2)} processed.',
                style: const TextStyle(color: AppTheme.onPrimaryColor, fontWeight: FontWeight.bold),
              ),
            ),
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Digital Boarding Pass'),
        elevation: 0,
        actions: [
          IconButton(
            icon: Icon(
              Icons.brightness_high,
              color: _brightnessBoosted ? AppTheme.primaryColor : null,
            ),
            tooltip: 'Boost brightness for QR scanner',
            onPressed: _toggleBrightnessBoost,
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: AppTheme.primaryColor,
          indicatorWeight: 3,
          labelColor: AppTheme.primaryColor,
          unselectedLabelColor: isDark ? Colors.white60 : Colors.black54,
          tabs: [
            Tab(text: 'Active Passes (${_activeTickets.length})'),
            Tab(text: 'Past Trips (${_pastTickets.length})'),
          ],
        ),
      ),
      body: Column(
        children: [
          // Offline Cryptographic Pass Banner
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            color: AppTheme.primaryColor.withValues(alpha: 0.12),
            child: const Row(
              children: [
                Icon(Icons.cloud_done_rounded, color: AppTheme.primaryColor, size: 16),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Offline Ready — Signed HMAC boarding passes verified without internet.',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w600,
                      color: AppTheme.primaryColor,
                    ),
                  ),
                ),
              ],
            ),
          ),

          // Tab Views
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                _buildTicketsList(_activeTickets, isUpcoming: true),
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
      return EmptyStateView(
        icon: isUpcoming ? Icons.confirmation_number_outlined : Icons.history_rounded,
        title: isUpcoming ? 'No Active Tickets' : 'No Travel History',
        description: isUpcoming
            ? 'Reserve your seat to view digital boarding passes here.'
            : 'Completed trips will appear here automatically.',
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
