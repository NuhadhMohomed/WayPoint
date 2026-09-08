import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

enum WayPointButtonVariant { primary, secondary, outline, danger }

class WayPointButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  final WayPointButtonVariant variant;
  final bool isLoading;
  final IconData? icon;

  const WayPointButton({
    super.key,
    required this.text,
    this.onPressed,
    this.variant = WayPointButtonVariant.primary,
    this.isLoading = false,
    this.icon,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (variant == WayPointButtonVariant.secondary) {
      return ElevatedButton(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppTheme.secondaryColor,
          foregroundColor: const Color(0xFF191C1D),
          minimumSize: const Size.fromHeight(48),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
          textStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 16),
        ),
        onPressed: isLoading ? null : onPressed,
        child: _buildChild(const Color(0xFF191C1D)),
      );
    }

    if (variant == WayPointButtonVariant.outline) {
      return OutlinedButton(
        style: OutlinedButton.styleFrom(
          side: const BorderSide(color: Color(0xFFC3C6D6)),
          foregroundColor: AppTheme.primaryColor,
          minimumSize: const Size.fromHeight(48),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
          textStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 16),
        ),
        onPressed: isLoading ? null : onPressed,
        child: _buildChild(AppTheme.primaryColor),
      );
    }

    if (variant == WayPointButtonVariant.danger) {
      return ElevatedButton(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppTheme.errorColor,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(48),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
          textStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 16),
        ),
        onPressed: isLoading ? null : onPressed,
        child: _buildChild(Colors.white),
      );
    }

    return ElevatedButton(
      style: theme.elevatedButtonTheme.style,
      onPressed: isLoading ? null : onPressed,
      child: _buildChild(Colors.white),
    );
  }

  Widget _buildChild(Color spinnerColor) {
    if (isLoading) {
      return SizedBox(
        width: 24,
        height: 24,
        child: CircularProgressIndicator(
          strokeWidth: 2.5,
          valueColor: AlwaysStoppedAnimation<Color>(spinnerColor),
        ),
      );
    }

    if (icon != null) {
      return Row(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 20),
          const SizedBox(width: 8),
          Text(text),
        ],
      );
    }

    return Text(text);
  }
}
