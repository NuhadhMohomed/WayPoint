import 'dart:async';
import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../../../core/widgets/transit_badge.dart';
import '../models/disruption_models.dart';

class DisruptionAlertScreen extends StatefulWidget {
  final DisruptionAlertModel? disruption;
  final VoidCallback? onAccepted;
  final VoidCallback? onDeclined;

  const DisruptionAlertScreen({
    super.key,
    this.disruption,
    this.onAccepted,
    this.onDeclined,
  });

  @override
  State<DisruptionAlertScreen> createState() => _DisruptionAlertScreenState();
}

class _DisruptionAlertScreenState extends State<DisruptionAlertScreen> {
  late DisruptionAlertModel _disruption;
  late int _remainingSeconds;
  Timer? _countdownTimer;
  bool _isProcessing = false;
  bool _isAccepted = false;
  bool _isRefunded = false;

  @override
  void initState() {
    super.initState();
    _disruption =
        widget.disruption ?? DisruptionAlertModel.sampleColomboToElla();
    _remainingSeconds = _disruption.remainingSeconds;

    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) return;
      if (_remainingSeconds > 0) {
        setState(() {
          _remainingSeconds--;
        });
      } else {
        timer.cancel();
      }
    });
  }

  @override
  void dispose() {
    _countdownTimer?.cancel();
    super.dispose();
  }

  String _formatTimer(int totalSeconds) {
    final minutes = totalSeconds ~/ 60;
    final seconds = totalSeconds % 60;
    return '${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
  }

  void _handleAccept() async {
    setState(() => _isProcessing = true);
    await Future.delayed(const Duration(milliseconds: 700));
    if (!mounted) return;
    setState(() {
      _isProcessing = false;
      _isAccepted = true;
    });

    widget.onAccepted?.call();

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => _buildAcceptConfirmationSheet(ctx),
    );
  }

  void _handleDecline() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF1E293B),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.info_outline, color: AppTheme.errorColor, size: 24),
            SizedBox(width: 8),
            Text(
              'Confirm 100% Refund',
              style: TextStyle(
                  color: Colors.white,
                  fontSize: 18,
                  fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Under the WayPoint Passenger Guarantee, you are entitled to a full 100% refund of Rs. ${_disruption.originalFarePaid.toStringAsFixed(2)} with no cancellation fee.',
              style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 14),
            ),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFF0F172A),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: const Color(0xFF334155)),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Total Refund Amount:',
                      style: TextStyle(color: Colors.grey, fontSize: 13)),
                  Text(
                    'Rs. ${_disruption.originalFarePaid.toStringAsFixed(2)}',
                    style: const TextStyle(
                        color: Color(0xFF22C55E),
                        fontSize: 16,
                        fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('Back to Review',
                style: TextStyle(color: Colors.grey)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: AppTheme.errorColor,
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8)),
            ),
            onPressed: () {
              Navigator.of(ctx).pop();
              setState(() => _isRefunded = true);
              widget.onDeclined?.call();

              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  backgroundColor: AppTheme.tertiaryColor,
                  content: Text(
                    'Full refund of Rs. ${_disruption.originalFarePaid.toStringAsFixed(2)} initiated to original payment method.',
                  ),
                  duration: const Duration(seconds: 4),
                ),
              );
            },
            child: const Text('Confirm Refund',
                style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );
  }

  void _showSupportModal() {
    showModalBottomSheet(
      context: context,
      backgroundColor: const Color(0xFF1E293B),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: AppTheme.primaryColor.withValues(alpha: 0.2),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Icon(Icons.support_agent,
                      color: AppTheme.primaryColor, size: 28),
                ),
                const SizedBox(width: 12),
                const Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('WayPoint Transit Support',
                        style: TextStyle(
                            color: Colors.white,
                            fontSize: 18,
                            fontWeight: FontWeight.bold)),
                    Text('Disruption Incident Response Desk',
                        style: TextStyle(color: Colors.grey, fontSize: 13)),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 20),
            const Text(
              'Our operations dispatchers are active 24/7 along the Colombo–Ella corridor to assist delayed passengers.',
              style: TextStyle(color: Color(0xFFCBD5E1), fontSize: 14),
            ),
            const SizedBox(height: 16),
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading:
                  const Icon(Icons.phone_in_talk, color: Color(0xFF22C55E)),
              title: const Text('Emergency Transit Hotline',
                  style: TextStyle(
                      color: Colors.white, fontWeight: FontWeight.w600)),
              subtitle: const Text('+94 11 234 5678 (Toll Free)',
                  style: TextStyle(color: Colors.grey)),
              trailing: const Icon(Icons.chevron_right, color: Colors.grey),
              onTap: () => Navigator.of(ctx).pop(),
            ),
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.chat_bubble_outline,
                  color: AppTheme.secondaryColor),
              title: const Text('Live Dispatcher Chat',
                  style: TextStyle(
                      color: Colors.white, fontWeight: FontWeight.w600)),
              subtitle: const Text('Average response time: 2 mins',
                  style: TextStyle(color: Colors.grey)),
              trailing: const Icon(Icons.chevron_right, color: Colors.grey),
              onTap: () => Navigator.of(ctx).pop(),
            ),
            const SizedBox(height: 12),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton(
                style: OutlinedButton.styleFrom(
                  side: const BorderSide(color: Color(0xFF475569)),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10)),
                ),
                onPressed: () => Navigator.of(ctx).pop(),
                child:
                    const Text('Close', style: TextStyle(color: Colors.white)),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildAcceptConfirmationSheet(BuildContext ctx) {
    return Container(
      decoration: const BoxDecoration(
        color: Color(0xFF1E293B),
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Container(
            width: 56,
            height: 56,
            decoration: const BoxDecoration(
              color: Color(0xFF14532D),
              shape: BoxShape.circle,
            ),
            child: const Icon(Icons.check_circle,
                color: Color(0xFF4ADE80), size: 36),
          ),
          const SizedBox(height: 16),
          const Text(
            'Rebooking Confirmed!',
            style: TextStyle(
                color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 6),
          Text(
            'Your booking ${_disruption.bookingReference} has been successfully transferred to ${_disruption.replacementBusPlate}.',
            textAlign: TextAlign.center,
            style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 14),
          ),
          const SizedBox(height: 20),
          Container(
            padding: const EdgeInsets.all(16),
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
                    const Text('Replacement Service',
                        style: TextStyle(color: Colors.grey, fontSize: 13)),
                    Text(_disruption.replacementBusPlate,
                        style: const TextStyle(
                            color: Colors.white, fontWeight: FontWeight.bold)),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('New Departure',
                        style: TextStyle(color: Colors.grey, fontSize: 13)),
                    Text(
                        '${_disruption.replacementDepartureTime} (${_disruption.replacementOriginStop})',
                        style: const TextStyle(
                            color: AppTheme.secondaryColor,
                            fontWeight: FontWeight.bold)),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Assigned Seats',
                        style: TextStyle(color: Colors.grey, fontSize: 13)),
                    Text(_disruption.assignedSeats.join(', '),
                        style: const TextStyle(
                            color: Colors.white, fontWeight: FontWeight.bold)),
                  ],
                ),
                const SizedBox(height: 8),
                const Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Fare Adjustment',
                        style: TextStyle(color: Colors.grey, fontSize: 13)),
                    Text('Rs. 0.00 (Fully Covered)',
                        style: TextStyle(
                            color: Color(0xFF4ADE80),
                            fontWeight: FontWeight.bold)),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          WayPointButton(
            text: 'View Updated Digital Ticket',
            icon: Icons.qr_code,
            onPressed: () {
              Navigator.of(ctx).pop();
            },
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        backgroundColor: const Color(0xFF0F172A),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.white),
          onPressed: () => Navigator.of(context).maybePop(),
        ),
        title: Column(
          children: [
            const Text(
              'Service Disruption Notice',
              style: TextStyle(
                  color: Colors.white,
                  fontSize: 16,
                  fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 2),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: BoxDecoration(
                color: const Color(0xFF1E293B),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(
                'Ref: ${_disruption.bookingReference}',
                style: const TextStyle(
                    color: Color(0xFF94A3B8),
                    fontSize: 11,
                    fontWeight: FontWeight.w600),
              ),
            ),
          ],
        ),
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.support_agent, color: Color(0xFF60A5FA)),
            onPressed: _showSupportModal,
            tooltip: 'Contact Transit Support',
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Status Pills if already actioned
            if (_isAccepted) ...[
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFF14532D),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFF22C55E)),
                ),
                child: const Row(
                  children: [
                    Icon(Icons.check_circle,
                        color: Color(0xFF4ADE80), size: 20),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'You accepted the recommended replacement service.',
                        style: TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.w600,
                            fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
            ] else if (_isRefunded) ...[
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFF451A1A),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFFEF4444)),
                ),
                child: const Row(
                  children: [
                    Icon(Icons.cancel, color: Color(0xFFF87171), size: 20),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Booking cancelled. 100% refund initiated to payment method.',
                        style: TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.w600,
                            fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
            ],

            // Critical Disruption Alert Banner (matching Stitch MOB-09)
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFF2C2411),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                    color: const Color(0xFFF59E0B).withValues(alpha: 0.5)),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x1AF59E0B),
                    blurRadius: 16,
                    offset: Offset(0, 4),
                  ),
                ],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(Icons.warning_amber_rounded,
                          color: Color(0xFFF59E0B), size: 28),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text(
                          _disruption.disruptionTitle,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            height: 1.25,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  Padding(
                    padding: const EdgeInsets.only(left: 38),
                    child: Text(
                      _disruption.disruptionReason,
                      style: const TextStyle(
                        color: Color(0xFFE2E8F0),
                        fontSize: 13,
                        height: 1.4,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),

            // Passenger Compensation Pill
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              decoration: BoxDecoration(
                color: const Color(0xFF0F291E),
                borderRadius: BorderRadius.circular(999),
                border: Border.all(
                    color: const Color(0xFF16A34A).withValues(alpha: 0.4)),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.local_cafe,
                      color: Color(0xFF4ADE80), size: 18),
                  const SizedBox(width: 8),
                  Flexible(
                    child: Text(
                      _disruption.compensationNotice,
                      style: const TextStyle(
                        color: Color(0xFF4ADE80),
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // Section Header
            const Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'ITINERARY COMPARISON',
                  style: TextStyle(
                    color: Color(0xFF94A3B8),
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1.2,
                  ),
                ),
                TransitBadge(
                    status: TransitStatus.delayed,
                    customLabel: 'Schedule Shift: +45m'),
              ],
            ),
            const SizedBox(height: 12),

            // 1. Original Service Card (Crossed-out / dimmed)
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFF1E293B).withValues(alpha: 0.6),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: const Color(0xFF334155)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'ORIGINAL SERVICE',
                        style: TextStyle(
                          color: Color(0xFF94A3B8),
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      Icon(Icons.directions_bus,
                          color: Colors.grey.shade600, size: 18),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    '${_disruption.originalBusPlate} • ${_disruption.originalBusClass}',
                    style: const TextStyle(
                      color: Color(0xFF94A3B8),
                      fontSize: 14,
                      decoration: TextDecoration.lineThrough,
                      decorationColor: Colors.grey,
                    ),
                  ),
                  const SizedBox(height: 14),
                  // Timeline
                  _buildTimelineLeg(
                    time: _disruption.originalDepartureTime,
                    location: _disruption.originalOriginStop,
                    isStrikethrough: true,
                    isLast: false,
                  ),
                  _buildTimelineLeg(
                    time: _disruption.originalArrivalTime,
                    location: _disruption.originalDestinationStop,
                    isStrikethrough: true,
                    isLast: true,
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // 2. AI Recommended Replacement Card (Highlighted with Lanka Blue glow)
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: const Color(0xFF1E293B),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppTheme.primaryColor, width: 2),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x330056D2),
                    blurRadius: 20,
                    offset: Offset(0, 6),
                  ),
                ],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'RECOMMENDED REPLACEMENT',
                        style: TextStyle(
                          color: Color(0xFF60A5FA),
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppTheme.primaryColor.withValues(alpha: 0.2),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.auto_awesome,
                                color: Color(0xFF60A5FA), size: 13),
                            SizedBox(width: 4),
                            Text(
                              'Automated Remedy',
                              style: TextStyle(
                                  color: Color(0xFF60A5FA),
                                  fontSize: 11,
                                  fontWeight: FontWeight.bold),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Text(
                        _disruption.replacementBusPlate,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 2),
                        decoration: BoxDecoration(
                          color: const Color(0xFF334155),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          _disruption.replacementBusClass,
                          style: const TextStyle(
                              color: Color(0xFFCBD5E1),
                              fontSize: 11,
                              fontWeight: FontWeight.w600),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  // Timeline
                  _buildTimelineLeg(
                    time: _disruption.replacementDepartureTime,
                    location: _disruption.replacementOriginStop,
                    isStrikethrough: false,
                    isLast: false,
                    accentColor: AppTheme.primaryColor,
                  ),
                  _buildTimelineLeg(
                    time: _disruption.replacementArrivalTime,
                    location: _disruption.replacementDestinationStop,
                    isStrikethrough: false,
                    isLast: true,
                    accentColor: AppTheme.primaryColor,
                  ),
                  const SizedBox(height: 16),
                  const Divider(color: Color(0xFF334155)),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Assigned Seats',
                              style: TextStyle(
                                  color: Color(0xFF94A3B8), fontSize: 11)),
                          const SizedBox(height: 2),
                          Text(
                            _disruption.assignedSeats.join(', '),
                            style: const TextStyle(
                                color: Colors.white,
                                fontSize: 15,
                                fontWeight: FontWeight.bold),
                          ),
                        ],
                      ),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          const Text('Fare Difference',
                              style: TextStyle(
                                  color: Color(0xFF94A3B8), fontSize: 11)),
                          const SizedBox(height: 2),
                          Text(
                            _disruption.fareDifferenceLabel,
                            style: const TextStyle(
                                color: Color(0xFF4ADE80),
                                fontSize: 14,
                                fontWeight: FontWeight.bold),
                          ),
                        ],
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),

            // 15-Minute Seat Hold Countdown Note
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(
                color: const Color(0xFF1E293B),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFF334155)),
              ),
              child: Row(
                children: [
                  const Icon(Icons.schedule,
                      color: AppTheme.secondaryColor, size: 20),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'Your seats on the replacement coach are held for: ${_formatTimer(_remainingSeconds)}',
                      style: const TextStyle(
                          color: Color(0xFFCBD5E1), fontSize: 12),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // Action Buttons Dock
            if (!_isAccepted && !_isRefunded) ...[
              WayPointButton(
                text: 'Accept Replacement Bus',
                icon: Icons.check_circle,
                isLoading: _isProcessing,
                onPressed: _remainingSeconds > 0 ? _handleAccept : null,
              ),
              const SizedBox(height: 12),
              WayPointButton(
                text: 'Request 100% Refund',
                variant: WayPointButtonVariant.outline,
                onPressed: _handleDecline,
              ),
            ],
            const SizedBox(height: 32),
          ],
        ),
      ),
    );
  }

  Widget _buildTimelineLeg({
    required String time,
    required String location,
    required bool isStrikethrough,
    required bool isLast,
    Color accentColor = const Color(0xFF64748B),
  }) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Column(
          children: [
            Container(
              width: 10,
              height: 10,
              decoration: BoxDecoration(
                color: accentColor,
                shape: BoxShape.circle,
              ),
            ),
            if (!isLast)
              Container(
                width: 2,
                height: 28,
                color: accentColor.withValues(alpha: 0.4),
              ),
          ],
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  time,
                  style: TextStyle(
                    color: isStrikethrough ? Colors.grey : Colors.white,
                    fontWeight: FontWeight.bold,
                    fontSize: 14,
                    decoration:
                        isStrikethrough ? TextDecoration.lineThrough : null,
                  ),
                ),
                Text(
                  location,
                  style: TextStyle(
                    color:
                        isStrikethrough ? Colors.grey : const Color(0xFFCBD5E1),
                    fontSize: 13,
                    decoration:
                        isStrikethrough ? TextDecoration.lineThrough : null,
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}
