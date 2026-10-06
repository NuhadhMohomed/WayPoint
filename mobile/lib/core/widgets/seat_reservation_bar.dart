import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../theme/app_theme.dart';
import 'waypoint_button.dart';

class SeatReservationBar extends StatelessWidget {
  final int remainingSeconds;
  final List<String> selectedSeats;
  final double totalAmount;
  final VoidCallback onContinue;
  final bool isLoading;

  const SeatReservationBar({
    super.key,
    required this.remainingSeconds,
    required this.selectedSeats,
    required this.totalAmount,
    required this.onContinue,
    this.isLoading = false,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final mins = (remainingSeconds ~/ 60).toString().padLeft(2, '0');
    final secs = (remainingSeconds % 60).toString().padLeft(2, '0');
    final timeStr = '$mins:$secs';

    Color timerColor;
    if (remainingSeconds > 120) {
      timerColor = AppTheme.primaryColor;
    } else if (remainingSeconds > 30) {
      timerColor = AppTheme.accentAmber;
    } else {
      timerColor = AppTheme.errorColor;
    }

    final currencyFormat = NumberFormat.currency(
      locale: 'en_LK',
      symbol: 'Rs. ',
      decimalDigits: 0,
    );

    return Container(
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF1E293B) : Colors.white,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: isDark ? 0.4 : 0.08),
            offset: const Offset(0, -4),
            blurRadius: 16,
          ),
        ],
        border: Border(
          top: BorderSide(
            color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
            width: 1,
          ),
        ),
      ),
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
      child: SafeArea(
        top: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Top Hold Timer Bar
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    Icon(Icons.timer_outlined, size: 16, color: timerColor),
                    const SizedBox(width: 6),
                    Text(
                      'Hold Expires: $timeStr',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        color: timerColor,
                      ),
                    ),
                  ],
                ),
                Text(
                  'Seats ${selectedSeats.join(', ')}',
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),

            // Price and Action Button
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        'Total Payable',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                          color: isDark ? Colors.grey[400] : Colors.grey[600],
                        ),
                      ),
                      Text(
                        currencyFormat.format(totalAmount),
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: AppTheme.primaryColor,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: WayPointButton(
                    text: 'Continue',
                    icon: Icons.arrow_forward,
                    isLoading: isLoading,
                    onPressed: selectedSeats.isEmpty ? null : onContinue,
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
