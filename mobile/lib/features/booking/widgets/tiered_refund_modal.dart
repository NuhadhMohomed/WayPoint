import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/waypoint_button.dart';

/// Interactive modal widget implementing BR-REFUND-001 tiered cancellation policy.
class TieredRefundModal extends StatefulWidget {
  final String bookingReference;
  final String routeTitle;
  final String serviceCode;
  final double totalPaid;
  final int hoursUntilDeparture;
  final void Function(String reason, double refundAmount, double refundPercent) onConfirm;

  const TieredRefundModal({
    super.key,
    required this.bookingReference,
    required this.routeTitle,
    required this.serviceCode,
    required this.totalPaid,
    required this.hoursUntilDeparture,
    required this.onConfirm,
  });

  @override
  State<TieredRefundModal> createState() => _TieredRefundModalState();
}

class _TieredRefundModalState extends State<TieredRefundModal> {
  String _selectedReason = 'Change of travel plans';
  bool _isCancelling = false;

  final List<String> _reasons = const [
    'Change of travel plans',
    'Medical or family emergency',
    'Alternative transit arranged',
    'Trip postponed to later date',
    'Other reasons',
  ];

  @override
  Widget build(BuildContext context) {
    final currencyFormat = NumberFormat.currency(locale: 'en_LK', symbol: 'Rs. ', decimalDigits: 2);

    // Determine refund tier (BR-REFUND-001)
    double refundPercent;
    String tierTitle;
    String tierDescription;
    Color tierColor;

    if (widget.hoursUntilDeparture > 24) {
      refundPercent = 0.90;
      tierTitle = 'Tier 1: 90% Refund Eligible';
      tierDescription = 'Cancellation requested > 24h prior to departure (10% platform fee retained).';
      tierColor = const Color(0xFF22C55E);
    } else if (widget.hoursUntilDeparture >= 12) {
      refundPercent = 0.50;
      tierTitle = 'Tier 2: 50% Refund Eligible';
      tierDescription = 'Cancellation requested 12–24h prior to departure (50% late cancellation penalty).';
      tierColor = AppTheme.secondaryColor;
    } else {
      refundPercent = 0.0;
      tierTitle = 'Tier 3: 0% Non-Refundable';
      tierDescription = 'Under 12h before departure: bus operator seats are locked and non-refundable.';
      tierColor = AppTheme.errorColor;
    }

    final refundAmount = widget.totalPaid * refundPercent;
    final deductionAmount = widget.totalPaid - refundAmount;

    return Padding(
      padding: EdgeInsets.only(
        left: 20,
        right: 20,
        top: 20,
        bottom: MediaQuery.of(context).viewInsets.bottom + 24,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Modal Header
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Cancel Booking & Refund',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  Text(
                    'Ref: ${widget.bookingReference} • ${widget.serviceCode}',
                    style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
                  ),
                ],
              ),
              IconButton(
                icon: const Icon(Icons.close, color: Color(0xFF94A3B8)),
                onPressed: () => Navigator.of(context).pop(),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Policy Progress / Visual Tier Timeline
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: const Color(0xFF0F172A),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: tierColor.withOpacity(0.5), width: 1.5),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(6),
                      decoration: BoxDecoration(
                        color: tierColor.withOpacity(0.2),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(Icons.shield_outlined, color: tierColor, size: 16),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            tierTitle,
                            style: TextStyle(color: tierColor, fontWeight: FontWeight.bold, fontSize: 13),
                          ),
                          Text(
                            '${widget.hoursUntilDeparture} hours remaining until scheduled departure',
                            style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 11),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                Text(
                  tierDescription,
                  style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 11, height: 1.3),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),

          // Itemized Rupee Calculation
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: const Color(0xFF0F172A),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: const Color(0xFF334155)),
            ),
            child: Column(
              children: [
                _buildRow('Original Fare Paid', currencyFormat.format(widget.totalPaid)),
                const SizedBox(height: 8),
                _buildRow(
                  'Cancellation Fee / Deduction (${((1 - refundPercent) * 100).toInt()}%)',
                  '- ${currencyFormat.format(deductionAmount)}',
                  valueColor: const Color(0xFFEF4444),
                ),
                const Divider(color: Color(0xFF334155), height: 20),
                _buildRow(
                  'Net Refund to Original Card',
                  currencyFormat.format(refundAmount),
                  isTotal: true,
                  valueColor: tierColor,
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),

          // Reason for Cancellation Dropdown
          const Text(
            'Reason for Cancellation',
            style: TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14),
            decoration: BoxDecoration(
              color: const Color(0xFF0F172A),
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: const Color(0xFF334155)),
            ),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<String>(
                value: _selectedReason,
                isExpanded: true,
                dropdownColor: const Color(0xFF1E293B),
                style: const TextStyle(color: Colors.white, fontSize: 13),
                items: _reasons.map((r) {
                  return DropdownMenuItem<String>(
                    value: r,
                    child: Text(r),
                  );
                }).toList(),
                onChanged: (val) {
                  if (val != null) {
                    setState(() {
                      _selectedReason = val;
                    });
                  }
                },
              ),
            ),
          ),
          const SizedBox(height: 24),

          // Actions
          Row(
            children: [
              Expanded(
                child: WayPointButton(
                  text: 'Keep Booking',
                  variant: WayPointButtonVariant.outline,
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                flex: 2,
                child: WayPointButton(
                  text: 'Confirm Cancellation',
                  variant: WayPointButtonVariant.danger,
                  icon: Icons.cancel_outlined,
                  isLoading: _isCancelling,
                  onPressed: () async {
                    setState(() {
                      _isCancelling = true;
                    });
                    await Future.delayed(const Duration(milliseconds: 1000));
                    if (!mounted) return;
                    setState(() {
                      _isCancelling = false;
                    });
                    if (!context.mounted) return;
                    Navigator.of(context).pop();
                    widget.onConfirm(_selectedReason, refundAmount, refundPercent);
                  },
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildRow(String label, String value, {bool isTotal = false, Color? valueColor}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: TextStyle(
            color: isTotal ? Colors.white : const Color(0xFF94A3B8),
            fontSize: isTotal ? 14 : 12,
            fontWeight: isTotal ? FontWeight.bold : FontWeight.normal,
          ),
        ),
        Text(
          value,
          style: TextStyle(
            color: valueColor ?? (isTotal ? Colors.white : const Color(0xFFE2E8F0)),
            fontSize: isTotal ? 15 : 12,
            fontWeight: isTotal ? FontWeight.w800 : FontWeight.w600,
            fontFamily: 'monospace',
          ),
        ),
      ],
    );
  }
}
