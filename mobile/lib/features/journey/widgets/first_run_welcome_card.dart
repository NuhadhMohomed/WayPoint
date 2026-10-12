import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';

class FirstRunWelcomeCard extends StatelessWidget {
  final Function(String origin, String dest) onSelectCorridor;
  final VoidCallback onDismiss;

  const FirstRunWelcomeCard({
    super.key,
    required this.onSelectCorridor,
    required this.onDismiss,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      margin: const EdgeInsets.only(bottom: 16.0),
      padding: const EdgeInsets.all(16.0),
      decoration: BoxDecoration(
        color: isDark ? AppTheme.darkCardBackground : Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: AppTheme.primaryColor.withOpacity(0.35),
          width: 1.5,
        ),
        boxShadow: [
          BoxShadow(
            color: AppTheme.primaryColor.withOpacity(0.08),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header with Dismiss Button
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: AppTheme.primaryColor.withOpacity(0.15),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.directions_bus_rounded,
                      size: 20,
                      color: AppTheme.primaryColor,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Text(
                    'Welcome to WayPoint! 🚌',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      letterSpacing: -0.3,
                      color: isDark ? AppTheme.textPrimaryDark : AppTheme.textPrimaryLight,
                    ),
                  ),
                ],
              ),
              IconButton(
                icon: Icon(
                  Icons.close_rounded,
                  size: 20,
                  color: isDark ? AppTheme.textMutedDark : AppTheme.textMutedLight,
                ),
                padding: EdgeInsets.zero,
                constraints: const BoxConstraints(),
                onPressed: onDismiss,
              ),
            ],
          ),
          const SizedBox(height: 10),
          Text(
            'Search intercity express buses or tap a popular corridor below to get started.',
            style: TextStyle(
              fontSize: 13,
              height: 1.4,
              color: isDark ? AppTheme.textMutedDark : AppTheme.textMutedLight,
            ),
          ),
          const SizedBox(height: 14),

          // Corridor Quick Action Chips
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _buildCorridorChip(
                context: context,
                label: 'Colombo ⇄ Galle',
                origin: 'Colombo',
                dest: 'Galle',
                isDark: isDark,
              ),
              _buildCorridorChip(
                context: context,
                label: 'Colombo ⇄ Kandy',
                origin: 'Colombo',
                dest: 'Kandy',
                isDark: isDark,
              ),
              _buildCorridorChip(
                context: context,
                label: 'Colombo ⇄ Jaffna',
                origin: 'Colombo',
                dest: 'Jaffna',
                isDark: isDark,
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildCorridorChip({
    required BuildContext context,
    required String label,
    required String origin,
    required String dest,
    required bool isDark,
  }) {
    return ActionChip(
      avatar: const Icon(Icons.flash_on_rounded, size: 14, color: AppTheme.primaryColor),
      label: Text(
        label,
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w600,
          color: isDark ? AppTheme.textPrimaryDark : AppTheme.textPrimaryLight,
        ),
      ),
      backgroundColor: isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
      side: BorderSide(
        color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
        width: 1,
      ),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      onPressed: () => onSelectCorridor(origin, dest),
    );
  }
}
