import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/transit_badge.dart';

/// Reusable card component to display disruption alerts across mobile screens.
class DisruptionAlertCard extends StatelessWidget {
  final String title;
  final String message;
  final String? serviceNumber;
  final String? delayNotice;
  final VoidCallback? onTap;

  const DisruptionAlertCard({
    super.key,
    required this.title,
    required this.message,
    this.serviceNumber,
    this.delayNotice,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 2,
      margin: const EdgeInsets.symmetric(vertical: 8),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: AppTheme.errorColor.withValues(alpha: 0.3), width: 1),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(6),
                    decoration: BoxDecoration(
                      color: AppTheme.errorColor.withValues(alpha: 0.12),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.warning_amber_rounded,
                      color: AppTheme.errorColor,
                      size: 20,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      title,
                      style: const TextStyle(
                        fontFamily: 'Plus Jakarta Sans',
                        fontWeight: FontWeight.w700,
                        fontSize: 15,
                        color: AppTheme.onSurfaceText,
                      ),
                    ),
                  ),
                  if (serviceNumber != null)
                    TransitBadge(
                      status: TransitStatus.disrupted,
                      customLabel: serviceNumber!,
                    ),
                ],
              ),
              const SizedBox(height: 10),
              Text(
                message,
                style: const TextStyle(
                  fontFamily: 'Inter',
                  fontSize: 13,
                  color: AppTheme.onSurfaceMuted,
                  height: 1.4,
                ),
              ),
              if (delayNotice != null) ...[
                const SizedBox(height: 10),
                Row(
                  children: [
                    const Icon(
                      Icons.access_time_filled_rounded,
                      size: 14,
                      color: AppTheme.errorColor,
                    ),
                    const SizedBox(width: 6),
                    Text(
                      delayNotice!,
                      style: const TextStyle(
                        fontFamily: 'Inter',
                        fontWeight: FontWeight.w600,
                        fontSize: 12,
                        color: AppTheme.errorColor,
                      ),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
