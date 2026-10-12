import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

class ShimmerLoadingCard extends StatefulWidget {
  final double height;
  final double width;
  final double borderRadius;

  const ShimmerLoadingCard({
    super.key,
    required this.height,
    this.width = double.infinity,
    this.borderRadius = 16,
  });

  @override
  State<ShimmerLoadingCard> createState() => _ShimmerLoadingCardState();
}

class _ShimmerLoadingCardState extends State<ShimmerLoadingCard>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _animation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1500),
    )..repeat(reverse: true);

    _animation = Tween<double>(begin: 0.35, end: 0.85).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final baseColor = isDark ? AppTheme.darkCardBackground : AppTheme.lightSurfaceSubdued;

    return AnimatedBuilder(
      animation: _animation,
      builder: (context, child) {
        return Container(
          width: widget.width,
          height: widget.height,
          decoration: BoxDecoration(
            color: baseColor.withOpacity(_animation.value),
            borderRadius: BorderRadius.circular(widget.borderRadius),
            border: Border.all(
              color: isDark ? AppTheme.darkBorderColor : AppTheme.lightBorderColor,
              width: 1,
            ),
          ),
        );
      },
    );
  }
}
