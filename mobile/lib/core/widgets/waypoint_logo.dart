import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

enum WayPointLogoVariant {
  iconOnly,
  horizontal,
  stacked,
}

class WayPointLogo extends StatelessWidget {
  final double size;
  final bool showText;
  final bool isDark;
  final WayPointLogoVariant? variant;

  const WayPointLogo({
    super.key,
    this.size = 48,
    this.showText = false,
    this.isDark = true,
    this.variant,
  });

  @override
  Widget build(BuildContext context) {
    final effectiveVariant = variant ?? (showText ? WayPointLogoVariant.horizontal : WayPointLogoVariant.iconOnly);

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

    if (effectiveVariant == WayPointLogoVariant.iconOnly) {
      return iconWidget;
    }

    final titleStyle = TextStyle(
      fontSize: size * 0.42,
      fontWeight: FontWeight.bold,
      letterSpacing: -0.5,
      color: isDark ? AppTheme.textPrimaryDark : AppTheme.textPrimaryLight,
    );

    final subtitleStyle = TextStyle(
      fontSize: size * 0.22,
      fontWeight: FontWeight.w600,
      color: isDark ? AppTheme.textMutedDark : AppTheme.textMutedLight,
      letterSpacing: 0.2,
    );

    if (effectiveVariant == WayPointLogoVariant.stacked) {
      return Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          iconWidget,
          SizedBox(height: size * 0.2),
          Text('WayPoint', style: titleStyle, textAlign: TextAlign.center),
          const SizedBox(height: 2),
          Text('Sri Lanka Transit', style: subtitleStyle, textAlign: TextAlign.center),
        ],
      );
    }

    // Default: WayPointLogoVariant.horizontal
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
            Text('WayPoint', style: titleStyle),
            Text('Sri Lanka Transit', style: subtitleStyle),
          ],
        ),
      ],
    );
  }
}
