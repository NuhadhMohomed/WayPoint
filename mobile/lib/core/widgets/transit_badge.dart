import 'package:flutter/material.dart';

enum TransitStatus { available, held, booked, disrupted, delayed, express, luxury }

class TransitBadge extends StatelessWidget {
  final TransitStatus status;
  final String? customLabel;

  const TransitBadge({
    super.key,
    required this.status,
    this.customLabel,
  });

  @override
  Widget build(BuildContext context) {
    Color bg;
    Color fg;
    String label;

    switch (status) {
      case TransitStatus.available:
        bg = const Color(0xFFE8F5E9);
        fg = const Color(0xFF005312);
        label = 'Available';
        break;
      case TransitStatus.held:
        bg = const Color(0xFFFFF8E1);
        fg = const Color(0xFF7E5700);
        label = 'Held';
        break;
      case TransitStatus.booked:
        bg = const Color(0xFFECEFF1);
        fg = const Color(0xFF455A64);
        label = 'Booked';
        break;
      case TransitStatus.disrupted:
        bg = const Color(0xFFFFEBEE);
        fg = const Color(0xFFBA1A1A);
        label = 'Disrupted';
        break;
      case TransitStatus.delayed:
        bg = const Color(0xFFFFF3E0);
        fg = const Color(0xFFE65100);
        label = 'Delayed';
        break;
      case TransitStatus.express:
        bg = const Color(0xFFE0F2FE);
        fg = const Color(0xFF0369A1);
        label = 'Express';
        break;
      case TransitStatus.luxury:
        bg = const Color(0xFFEEF2FF);
        fg = const Color(0xFF4338CA);
        label = 'Luxury';
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 6,
            height: 6,
            decoration: BoxDecoration(
              color: fg,
              shape: BoxShape.circle,
            ),
          ),
          const SizedBox(width: 6),
          Text(
            customLabel ?? label,
            style: TextStyle(
              color: fg,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
