import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:intl/intl.dart';
import '../bloc/booking_history_bloc.dart';

/// MOB-08: Booking History & Tiered Refund Modal.
///
/// Shows all bookings with status badges. Confirmed bookings have a
/// "Cancel Booking" action that opens a bottom sheet showing the
/// tiered refund policy (>24h=90%, 12-24h=50%, <12h=0%).
class BookingHistoryScreen extends StatelessWidget {
  const BookingHistoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => BookingHistoryBloc()..add(const LoadBookingHistory()),
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Booking History'),
          backgroundColor: const Color(0xFF0056D2),
          foregroundColor: Colors.white,
        ),
        body: BlocConsumer<BookingHistoryBloc, BookingHistoryState>(
          listener: (context, state) {
            if (state is CancellationResult) {
              showDialog(
                context: context,
                builder: (_) => AlertDialog(
                  icon: const Icon(Icons.check_circle, color: Colors.green, size: 48),
                  title: const Text('Booking Cancelled'),
                  content: Text(
                    'Refund: ${(state.refundPercentage * 100).toInt()}%\n'
                    'Amount: Rs. ${state.refundAmount.toStringAsFixed(2)}',
                  ),
                  actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('OK'))],
                ),
              );
            }
          },
          builder: (context, state) {
            if (state is BookingHistoryLoading) {
              return const Center(child: CircularProgressIndicator(color: Color(0xFF0056D2)));
            }

            List<Map<String, dynamic>> bookings = [];
            if (state is BookingHistoryLoaded) {
              bookings = state.bookings;
            } else if (state is CancellationProcessing) {
              bookings = state.bookings;
            } else if (state is CancellationResult) {
              bookings = state.bookings;
            }

            if (bookings.isEmpty) {
              return const Center(child: Text('No bookings yet'));
            }

            return Stack(
              children: [
                ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: bookings.length,
                  itemBuilder: (context, index) => _BookingCard(booking: bookings[index]),
                ),
                if (state is CancellationProcessing)
                  Container(
                    color: Colors.black.withValues(alpha: 0.3),
                    child: const Center(child: CircularProgressIndicator(color: Colors.white)),
                  ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _BookingCard extends StatelessWidget {
  final Map<String, dynamic> booking;
  const _BookingCard({required this.booking});

  @override
  Widget build(BuildContext context) {
    final departure = DateTime.parse(booking['departureTime'] as String);
    final isConfirmed = booking['status'] == 'Confirmed';

    final statusColors = {
      'Confirmed': Colors.green,
      'Completed': const Color(0xFF0056D2),
      'Cancelled': Colors.red,
    };

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(booking['route'] ?? booking['serviceCode'],
                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: (statusColors[booking['status']] ?? Colors.grey).withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    booking['status'],
                    style: TextStyle(
                      color: statusColors[booking['status']] ?? Colors.grey,
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            _InfoRow(icon: Icons.calendar_today, text: DateFormat('MMM dd, yyyy • hh:mm a').format(departure)),
            _InfoRow(icon: Icons.event_seat, text: 'Seats: ${(booking['seats'] as List).join(', ')}'),
            _InfoRow(icon: Icons.attach_money, text: 'Rs. ${(booking['amount'] as num).toStringAsFixed(2)}'),

            if (isConfirmed) ...[
              const Divider(height: 24),
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  onPressed: () => _showRefundModal(context, booking),
                  icon: const Icon(Icons.cancel_outlined, size: 18),
                  label: const Text('Cancel Booking'),
                  style: TextButton.styleFrom(foregroundColor: Colors.red),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  void _showRefundModal(BuildContext context, Map<String, dynamic> booking) {
    final departure = DateTime.parse(booking['departureTime'] as String);
    final hoursUntil = departure.difference(DateTime.now()).inHours;
    final amount = (booking['amount'] as num).toDouble();

    double percentage;
    if (hoursUntil > 24) {
      percentage = 0.9;
    } else if (hoursUntil >= 12) {
      percentage = 0.5;
    } else {
      percentage = 0.0;
    }

    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
      builder: (_) => Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Cancel Booking', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            const Text('Refund Policy:', style: TextStyle(fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            _PolicyRow(label: '> 24 hours prior', refund: '90% refund', active: hoursUntil > 24),
            _PolicyRow(label: '12 – 24 hours prior', refund: '50% refund', active: hoursUntil >= 12 && hoursUntil <= 24),
            _PolicyRow(label: '< 12 hours prior', refund: 'Non-refundable', active: hoursUntil < 12),
            const Divider(height: 32),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Estimated Refund', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                Text(
                  'Rs. ${(amount * percentage).toStringAsFixed(2)}',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 18,
                    color: percentage > 0 ? Colors.green : Colors.red,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: () => Navigator.pop(context),
                    child: const Text('Keep Booking'),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: ElevatedButton(
                    onPressed: () {
                      Navigator.pop(context);
                      context.read<BookingHistoryBloc>().add(CancelBooking(booking));
                    },
                    style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
                    child: const Text('Confirm Cancel', style: TextStyle(color: Colors.white)),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String text;
  const _InfoRow({required this.icon, required this.text});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        children: [
          Icon(icon, size: 16, color: Colors.grey.shade600),
          const SizedBox(width: 8),
          Text(text, style: TextStyle(color: Colors.grey.shade700)),
        ],
      ),
    );
  }
}

class _PolicyRow extends StatelessWidget {
  final String label;
  final String refund;
  final bool active;
  const _PolicyRow({required this.label, required this.refund, required this.active});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: active ? Colors.black : Colors.grey)),
          Row(
            children: [
              if (active) const Icon(Icons.arrow_forward, size: 14, color: Color(0xFF0056D2)),
              const SizedBox(width: 4),
              Text(refund, style: TextStyle(
                fontWeight: active ? FontWeight.bold : FontWeight.normal,
                color: active ? Colors.black : Colors.grey,
              )),
            ],
          ),
        ],
      ),
    );
  }
}
