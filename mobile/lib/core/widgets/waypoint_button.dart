import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../theme/app_theme.dart';

enum WayPointButtonVariant { primary, secondary, outline, danger }

class WayPointButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  final WayPointButtonVariant variant;
  final bool isLoading;
  final IconData? icon;
  final double? width;
  final double height;

  const WayPointButton({
    super.key,
    required this.text,
    this.onPressed,
    this.variant = WayPointButtonVariant.primary,
    this.isLoading = false,
    this.icon,
    this.width,
    this.height = 48,
  });

  @override
  Widget build(BuildContext context) {
    Color bg;
    Color fg;
    BorderSide side = BorderSide.none;

    switch (variant) {
      case WayPointButtonVariant.secondary:
        bg = AppTheme.secondaryColor;
        fg = const Color(0xFF191C1D);
        break;
      case WayPointButtonVariant.outline:
        final isDark = Theme.of(context).brightness == Brightness.dark;
        bg = Colors.transparent;
        fg = isDark ? AppTheme.primaryColor : const Color(0xFF0F172A);
        side = BorderSide(color: isDark ? AppTheme.darkBorderColor : AppTheme.lightBorderColor, width: 1.5);
        break;
      case WayPointButtonVariant.danger:
        bg = AppTheme.errorColor;
        fg = Colors.white;
        break;
      case WayPointButtonVariant.primary:
        bg = AppTheme.primaryColor;
        fg = AppTheme.onPrimaryColor;
        break;
    }

    final effectiveOnPressed = (isLoading || onPressed == null)
        ? null
        : () {
            HapticFeedback.lightImpact();
            onPressed!();
          };

    Widget childContent;
    if (isLoading) {
      childContent = SizedBox(
        width: 22,
        height: 22,
        child: CircularProgressIndicator(
          strokeWidth: 2.5,
          valueColor: AlwaysStoppedAnimation<Color>(fg),
        ),
      );
    } else if (icon != null) {
      childContent = Row(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 20, color: fg),
          const SizedBox(width: 8),
          Text(
            text,
            style: TextStyle(
              color: fg,
              fontWeight: FontWeight.bold,
              fontSize: 15,
            ),
          ),
        ],
      );
    } else {
      childContent = Text(
        text,
        style: TextStyle(
          color: fg,
          fontWeight: FontWeight.bold,
          fontSize: 15,
        ),
      );
    }

    return SizedBox(
      width: width ?? double.infinity,
      height: height,
      child: ElevatedButton(
        style: ElevatedButton.styleFrom(
          backgroundColor: bg,
          foregroundColor: fg,
          disabledBackgroundColor: bg.withValues(alpha: 0.5),
          disabledForegroundColor: fg.withValues(alpha: 0.5),
          elevation: 0,
          side: side,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          padding: const EdgeInsets.symmetric(horizontal: 16),
        ),
        onPressed: effectiveOnPressed,
        child: childContent,
      ),
    );
  }
}
