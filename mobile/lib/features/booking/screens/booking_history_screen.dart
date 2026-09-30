import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../models/booking_models.dart';
import '../services/booking_api_service.dart';
import '../widgets/tiered_refund_modal.dart';
import 'ticket_wallet_screen.dart';

/// MOB-08: Booking History & Tiered Refund Modal
/// Stitch Screen ID: e7403487c674488db9f16886e3f42c26
/// Component 3: Booking, Ticketing & Passenger Options (Mithila)
class BookingHistoryScreen extends StatefulWidget {
  final List<HistoricalBookingItem>? initialBookings;

  const BookingHistoryScreen({
    super.key,
    this.initialBookings,
  });

  @override
  State<BookingHistoryScreen> createState() => _BookingHistoryScreenState();
}

class _BookingHistoryScreenState extends State<BookingHistoryScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  late List<HistoricalBookingItem> _bookings;
  String _searchQuery = '';
  final TextEditingController _searchController = TextEditingController();

  final currencyFormat = NumberFormat.currency(locale: 'en_LK', symbol: 'Rs. ', decimalDigits: 2);
  final dateFormat = DateFormat('EEE, dd MMM yyyy • hh:mm a');
  final shortDateFormat = DateFormat('dd MMM, hh:mm a');

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 4, vsync: this);
    _tabController.addListener(() {
      if (!_tabController.indexIsChanging) {
        setState(() {});
      }
    });
    _bookings = widget.initialBookings != null
        ? List.from(widget.initialBookings!)
        : HistoricalBookingItem.sampleBookings();

    if (widget.initialBookings == null) {
      _loadLiveBookings();
    }
  }

  Future<void> _loadLiveBookings() async {
    final live = await BookingApiService().fetchBookingHistory();
    if (mounted && live.isNotEmpty) {
      setState(() {
        _bookings = live;
      });
    }
  }

  @override
  void dispose() {
    _tabController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  List<HistoricalBookingItem> get _filteredBookings {
    List<HistoricalBookingItem> list = _bookings;

    // Apply tab filter
    switch (_tabController.index) {
      case 1: // Upcoming
        list = list.where((b) => b.isUpcoming).toList();
        break;
      case 2: // Completed
        list = list.where((b) => b.isCompleted).toList();
        break;
      case 3: // Cancelled
        list = list.where((b) => b.isCancelled).toList();
        break;
      default: // All
        break;
    }

    // Apply search filter
    if (_searchQuery.trim().isNotEmpty) {
      final q = _searchQuery.toLowerCase();
      list = list.where((b) {
        return b.bookingReference.toLowerCase().contains(q) ||
            b.routeTitle.toLowerCase().contains(q) ||
            b.originCity.toLowerCase().contains(q) ||
            b.destinationCity.toLowerCase().contains(q) ||
            b.serviceCode.toLowerCase().contains(q);
      }).toList();
    }

    return list;
  }

  int get _upcomingCount => _bookings.where((b) => b.isUpcoming).length;
  int get _completedCount => _bookings.where((b) => b.isCompleted).length;
  int get _cancelledCount => _bookings.where((b) => b.isCancelled).length;

  void _openRefundModal(HistoricalBookingItem booking) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: const Color(0xFF1E293B),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => TieredRefundModal(
        bookingReference: booking.bookingReference,
        routeTitle: booking.routeTitle,
        serviceCode: booking.serviceCode,
        totalPaid: booking.totalPaid,
        hoursUntilDeparture: booking.hoursUntilDeparture,
        onConfirm: (reason, refundAmount, refundPercent) {
          // Trigger live backend refund API
          BookingApiService().cancelBookingAndRefund(
            bookingReference: booking.bookingReference,
            reason: reason,
          );

          setState(() {
            final index = _bookings.indexWhere((b) => b.bookingId == booking.bookingId);
            if (index != -1) {
              _bookings[index] = booking.copyWith(
                status: 'Cancelled',
                refundAmount: refundAmount,
                refundPercentage: refundPercent,
                cancellationReason: reason,
              );
            }
          });

          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Row(
                children: [
                  const Icon(Icons.check_circle, color: Colors.white, size: 20),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'Booking ${booking.bookingReference} cancelled. '
                      'Refund of ${currencyFormat.format(refundAmount)} (${(refundPercent * 100).toInt()}%) processed to your card.',
                      style: const TextStyle(fontSize: 13),
                    ),
                  ),
                ],
              ),
              backgroundColor: const Color(0xFF005312),
              duration: const Duration(seconds: 4),
            ),
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0A0F1D),
      appBar: AppBar(
        backgroundColor: const Color(0xFF0F172A),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.white),
          onPressed: () => Navigator.of(context).pop(),
        ),
        title: const Text(
          'Booking History & Refunds',
          style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.confirmation_number_outlined, color: Color(0xFF818CF8)),
            tooltip: 'Open Ticket Wallet',
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => const TicketWalletScreen(),
                ),
              );
            },
          ),
        ],
      ),
      body: Column(
        children: [
          // Top Summary Stats Bar
          _buildSummaryStats(),

          // Search Field
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: TextField(
              controller: _searchController,
              style: const TextStyle(color: Colors.white, fontSize: 14),
              onChanged: (val) => setState(() => _searchQuery = val),
              decoration: InputDecoration(
                hintText: 'Search by Ref (e.g. WP-7B92K1), city, or route...',
                hintStyle: const TextStyle(color: Color(0xFF64748B), fontSize: 13),
                prefixIcon: const Icon(Icons.search, color: Color(0xFF94A3B8), size: 20),
                suffixIcon: _searchQuery.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear, color: Color(0xFF94A3B8), size: 18),
                        onPressed: () {
                          _searchController.clear();
                          setState(() => _searchQuery = '');
                        },
                      )
                    : null,
                filled: true,
                fillColor: const Color(0xFF1E293B),
                contentPadding: const EdgeInsets.symmetric(vertical: 0, horizontal: 16),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide.none,
                ),
              ),
            ),
          ),

          // Segmented Tabs
          Container(
            color: const Color(0xFF0F172A),
            child: TabBar(
              controller: _tabController,
              isScrollable: true,
              indicatorColor: AppTheme.primaryColor,
              indicatorWeight: 3,
              labelColor: AppTheme.primaryColor,
              unselectedLabelColor: const Color(0xFF94A3B8),
              labelStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.bold),
              tabs: [
                Tab(text: 'All (${_bookings.length})'),
                Tab(text: 'Upcoming ($_upcomingCount)'),
                Tab(text: 'Completed ($_completedCount)'),
                Tab(text: 'Cancelled ($_cancelledCount)'),
              ],
            ),
          ),

          // Booking Cards List
          Expanded(
            child: _filteredBookings.isEmpty
                ? _buildEmptyState()
                : ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _filteredBookings.length,
                    itemBuilder: (context, index) {
                      final booking = _filteredBookings[index];
                      return _buildBookingCard(booking);
                    },
                  ),
          ),
        ],
      ),
    );
  }

  Widget _buildSummaryStats() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      color: const Color(0xFF0F172A),
      child: Row(
        children: [
          Expanded(
            child: _buildStatItem(
              'Upcoming',
              '$_upcomingCount trips',
              const Color(0xFF22C55E),
              Icons.schedule,
            ),
          ),
          Container(height: 32, width: 1, color: const Color(0xFF334155)),
          Expanded(
            child: _buildStatItem(
              'Completed',
              '$_completedCount trips',
              const Color(0xFF818CF8),
              Icons.task_alt,
            ),
          ),
          Container(height: 32, width: 1, color: const Color(0xFF334155)),
          Expanded(
            child: _buildStatItem(
              'Cancelled',
              '$_cancelledCount trips',
              const Color(0xFFEF4444),
              Icons.cancel_outlined,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStatItem(String label, String value, Color color, IconData icon) {
    return Column(
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 14, color: color),
            const SizedBox(width: 4),
            Text(
              label,
              style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 11),
            ),
          ],
        ),
        const SizedBox(height: 4),
        Text(
          value,
          style: TextStyle(color: color, fontSize: 13, fontWeight: FontWeight.bold),
        ),
      ],
    );
  }

  Widget _buildBookingCard(HistoricalBookingItem booking) {
    final isUpcoming = booking.isUpcoming;
    final isCancelled = booking.isCancelled;

    Color statusColor;
    String statusLabel;
    IconData statusIcon;

    if (isCancelled) {
      statusColor = const Color(0xFFEF4444);
      statusLabel = 'CANCELLED';
      statusIcon = Icons.cancel;
    } else if (isUpcoming) {
      statusColor = const Color(0xFF22C55E);
      statusLabel = 'CONFIRMED';
      statusIcon = Icons.check_circle;
    } else {
      statusColor = const Color(0xFF818CF8);
      statusLabel = 'COMPLETED';
      statusIcon = Icons.task_alt;
    }

    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: const Color(0xFF1E293B),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isUpcoming ? AppTheme.primaryColor.withOpacity(0.4) : const Color(0xFF334155),
          width: isUpcoming ? 1.5 : 1.0,
        ),
        boxShadow: const [
          BoxShadow(
            color: Color(0x1A000000),
            blurRadius: 10,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Card Header: Reference + Copy + Status
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    Text(
                      booking.bookingReference,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 1.1,
                        fontFamily: 'monospace',
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.copy, size: 14, color: Color(0xFF94A3B8)),
                      tooltip: 'Copy Reference',
                      onPressed: () {
                        Clipboard.setData(ClipboardData(text: booking.bookingReference));
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(
                            content: Text('Copied ${booking.bookingReference} to clipboard'),
                            duration: const Duration(seconds: 1),
                          ),
                        );
                      },
                    ),
                  ],
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: statusColor.withOpacity(0.15),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: statusColor.withOpacity(0.4)),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(statusIcon, color: statusColor, size: 12),
                      const SizedBox(width: 4),
                      Text(
                        statusLabel,
                        style: TextStyle(
                          color: statusColor,
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 0.5,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const Divider(color: Color(0xFF334155), height: 1),

          // Route Details & Timings
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  booking.routeTitle,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Service: ${booking.serviceCode} • Booked on ${shortDateFormat.format(booking.bookedAt)}',
                  style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
                ),
                const SizedBox(height: 14),

                // Corridor Path (Origin -> Destination)
                Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('DEPARTURE', style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 2),
                          Text(
                            booking.originCity,
                            style: const TextStyle(color: Color(0xFFE2E8F0), fontSize: 12, fontWeight: FontWeight.w600),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                          Text(
                            dateFormat.format(booking.departureTime),
                            style: const TextStyle(color: AppTheme.secondaryColor, fontSize: 11, fontWeight: FontWeight.w500),
                          ),
                        ],
                      ),
                    ),
                    const Padding(
                      padding: EdgeInsets.symmetric(horizontal: 8),
                      child: Icon(Icons.arrow_forward, color: Color(0xFF64748B), size: 16),
                    ),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          const Text('ARRIVAL', style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 2),
                          Text(
                            booking.destinationCity,
                            style: const TextStyle(color: Color(0xFFE2E8F0), fontSize: 12, fontWeight: FontWeight.w600),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                          Text(
                            dateFormat.format(booking.arrivalTime),
                            style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 11),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),

                // Seat Chips & Total Paid
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        const Text(
                          'Seats: ',
                          style: TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
                        ),
                        ...booking.seatNumbers.map(
                          (s) => Container(
                            margin: const EdgeInsets.only(right: 6),
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                            decoration: BoxDecoration(
                              color: const Color(0xFF0F172A),
                              borderRadius: BorderRadius.circular(6),
                              border: Border.all(color: const Color(0xFF475569)),
                            ),
                            child: Text(
                              s,
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 11,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        const Text(
                          'Total Paid',
                          style: TextStyle(color: Color(0xFF64748B), fontSize: 10),
                        ),
                        Text(
                          currencyFormat.format(booking.totalPaid),
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            fontFamily: 'monospace',
                          ),
                        ),
                      ],
                    ),
                  ],
                ),

                // If Cancelled: Show Refund Banner & Reason
                if (isCancelled) ...[
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFF450A0A),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: const Color(0xFF991B1B)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            const Row(
                              children: [
                                Icon(Icons.currency_exchange, color: Color(0xFFFCA5A5), size: 14),
                                SizedBox(width: 6),
                                Text(
                                  'Refund Processed',
                                  style: TextStyle(color: Color(0xFFFCA5A5), fontSize: 12, fontWeight: FontWeight.bold),
                                ),
                              ],
                            ),
                            Text(
                              booking.refundAmount != null
                                  ? '${currencyFormat.format(booking.refundAmount!)} (${((booking.refundPercentage ?? 0) * 100).toInt()}%)'
                                  : '0% Non-refundable',
                              style: const TextStyle(
                                color: Color(0xFFF87171),
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                fontFamily: 'monospace',
                              ),
                            ),
                          ],
                        ),
                        if (booking.cancellationReason != null) ...[
                          const SizedBox(height: 4),
                          Text(
                            'Reason: ${booking.cancellationReason}',
                            style: const TextStyle(color: Color(0xFFFECACA), fontSize: 11),
                          ),
                        ],
                      ],
                    ),
                  ),
                ],

                // If Upcoming: Show Policy notice & Action Buttons
                if (isUpcoming) ...[
                  const SizedBox(height: 14),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                    decoration: BoxDecoration(
                      color: const Color(0xFF0F172A),
                      borderRadius: BorderRadius.circular(6),
                      border: Border.all(color: const Color(0xFF334155)),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.shield_outlined, color: AppTheme.secondaryColor, size: 14),
                        const SizedBox(width: 6),
                        Expanded(
                          child: Text(
                            'Departs in ${booking.hoursUntilDeparture}h • Eligible for ${(booking.refundTierPercentage * 100).toInt()}% refund under BR-REFUND-001',
                            style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 11),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 14),
                  Row(
                    children: [
                      Expanded(
                        child: WayPointButton(
                          text: 'View Pass',
                          variant: WayPointButtonVariant.outline,
                          icon: Icons.qr_code,
                          onPressed: () {
                            Navigator.of(context).push(
                              MaterialPageRoute(
                                builder: (_) => const TicketWalletScreen(),
                              ),
                            );
                          },
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: WayPointButton(
                          text: 'Cancel & Refund',
                          variant: WayPointButtonVariant.danger,
                          icon: Icons.cancel_outlined,
                          onPressed: () => _openRefundModal(booking),
                        ),
                      ),
                    ],
                  ),
                ],

                // If Completed: Action Buttons
                if (!isUpcoming && !isCancelled) ...[
                  const SizedBox(height: 14),
                  Row(
                    children: [
                      Expanded(
                        child: WayPointButton(
                          text: 'Download Receipt',
                          variant: WayPointButtonVariant.outline,
                          icon: Icons.receipt_long,
                          onPressed: () {
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(
                                content: Text('PDF Tax Receipt downloaded for ${booking.bookingReference}'),
                                backgroundColor: const Color(0xFF0F172A),
                              ),
                            );
                          },
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.history_toggle_off, size: 64, color: Color(0xFF475569)),
          const SizedBox(height: 16),
          const Text(
            'No Bookings Found',
            style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Text(
            _searchQuery.isNotEmpty
                ? 'No matching results for "$_searchQuery"'
                : 'No bookings in this category yet.',
            style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
          ),
        ],
      ),
    );
  }
}
