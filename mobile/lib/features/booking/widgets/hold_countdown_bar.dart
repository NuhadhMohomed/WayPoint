import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';

/// Sticky header bar that displays an animated ticking 10-minute hold progress bar.
/// Complies with Stitch Design System tokens and FR-BOOKING-001.
class HoldCountdownBar extends StatelessWidget {
  final int remainingSeconds;
  final int totalSeconds;
  final bool isExpired;

  const HoldCountdownBar({
    super.key,
    required this.remainingSeconds,
    required this.totalSeconds,
    this.isExpired = false,
  });

  @override
  Widget build(BuildContext context) {
    final progress = totalSeconds > 0
        ? (remainingSeconds / totalSeconds).clamp(0.0, 1.0)
        : 0.0;

    // Determine state color based on remaining time
    Color barColor;
    Color textColor;
    Color bgColor;
    IconData statusIcon;
    String statusMessage;

    if (isExpired || remainingSeconds <= 0) {
      barColor = AppTheme.errorColor;
      textColor = const Color(0xFFFFB4AB);
      bgColor = const Color(0xFF3B0909);
      statusIcon = Icons.timer_off_outlined;
      statusMessage = 'Seat hold expired. Seats have been released.';
    } else if (remainingSeconds <= 120) {
      // Under 2 minutes - Critical alert
      barColor = AppTheme.errorColor;
      textColor = const Color(0xFFFF8A80);
      bgColor = const Color(0xFF2A0D0D);
      statusIcon = Icons.warning_amber_rounded;
      statusMessage = 'Hurry! Seats release in less than 2 minutes';
    } else if (remainingSeconds <= 300) {
      // 2 to 5 minutes - Caution warning
      barColor = AppTheme.secondaryColor;
      textColor = const Color(0xFFFFD54F);
      bgColor = const Color(0xFF2A2000);
      statusIcon = Icons.access_time_filled;
      statusMessage = 'Temporary hold active. Complete checkout soon';
    } else {
      // More than 5 minutes - Safe
      barColor = const Color(0xFF22C55E); // Jungle green accent
      textColor = const Color(0xFF86EFAC);
      bgColor = const Color(0xFF05230F);
      statusIcon = Icons.lock_clock_outlined;
      statusMessage = 'Seats held exclusively for you';
    }

    final minutes = (remainingSeconds ~/ 60).toString().padLeft(2, '0');
    final seconds = (remainingSeconds % 60).toString().padLeft(2, '0');

    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: bgColor,
        border: Border(
          bottom: BorderSide(color: barColor.withOpacity(0.35), width: 1.5),
        ),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(6),
                  decoration: BoxDecoration(
                    color: barColor.withOpacity(0.2),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(statusIcon, color: barColor, size: 18),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        statusMessage,
                        style: TextStyle(
                          color: textColor,
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: 2),
                      Text(
                        isExpired
                            ? '00:00 — Expired'
                            : 'Expires in $minutes:$seconds min',
                        style: TextStyle(
                          color: textColor.withOpacity(0.75),
                          fontSize: 11,
                          fontFamily: 'monospace',
                        ),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: barColor.withOpacity(0.15),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: barColor.withOpacity(0.5), width: 1),
                  ),
                  child: Text(
                    isExpired ? '00:00' : '$minutes:$seconds',
                    style: TextStyle(
                      color: barColor,
                      fontSize: 16,
                      fontWeight: FontWeight.w800,
                      fontFamily: 'monospace',
                      letterSpacing: 1.0,
                    ),
                  ),
                ),
              ],
            ),
          ),
          LinearProgressIndicator(
            value: progress,
            backgroundColor: barColor.withOpacity(0.15),
            valueColor: AlwaysStoppedAnimation<Color>(barColor),
            minHeight: 3.5,
          ),
        ],
      ),
    );
  }
}
