import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

class WayPointLogo extends StatelessWidget {
  final double size;
  final bool showText;
  final bool isDark;

  const WayPointLogo({
    super.key,
    this.size = 48,
    this.showText = false,
    this.isDark = true,
  });

  @override
  Widget build(BuildContext context) {
    final iconWidget = Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: AppTheme.primaryColor,
        borderRadius: BorderRadius.circular(size * 0.28),
        boxShadow: [
          BoxShadow(
            color: AppTheme.primaryColor.withOpacity(0.35),
            blurRadius: size * 0.3,
            offset: Offset(0, size * 0.1),
          ),
        ],
      ),
      child: Center(
        child: Icon(
          Icons.alt_route_rounded,
          color: AppTheme.onPrimaryColor,
          size: size * 0.62,
        ),
      ),
    );

    if (!showText) {
      return iconWidget;
    }

    return Row(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        iconWidget,
        const SizedBox(width: 12),
        Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'WayPoint',
              style: TextStyle(
                fontSize: size * 0.5,
                fontWeight: FontWeight.bold,
                letterSpacing: -0.5,
                color: isDark ? AppTheme.textPrimaryDark : AppTheme.textPrimaryLight,
              ),
            ),
            Text(
              'Sri Lanka Transit',
              style: TextStyle(
                fontSize: size * 0.24,
                fontWeight: FontWeight.w600,
                color: isDark ? AppTheme.textMutedDark : AppTheme.textMutedLight,
                letterSpacing: 0.2,
              ),
            ),
          ],
        ),
      ],
    );
  }
}
