import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';

class AiInsightsBanner extends StatelessWidget {
  final String agentReasoning;
  final bool isAiFallback;

  const AiInsightsBanner({
    super.key,
    required this.agentReasoning,
    this.isAiFallback = false,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final cardBg = isDark ? const Color(0xFF1E293B) : Colors.white;
    final borderColor = isAiFallback
        ? const Color(0xFFF59E0B)
        : AppTheme.primaryColor.withValues(alpha: 0.3);

    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: cardBg,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: borderColor, width: 1.2),
        boxShadow: [
          BoxShadow(
            color: (isAiFallback ? const Color(0xFFF59E0B) : AppTheme.primaryColor)
                .withValues(alpha: 0.06),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                isAiFallback ? Icons.cloud_off_rounded : Icons.auto_awesome,
                size: 16,
                color: isAiFallback ? const Color(0xFFF59E0B) : AppTheme.primaryColor,
              ),
              const SizedBox(width: 8),
              Text(
                isAiFallback ? 'AI Offline — Safe Fallback' : 'AI Recommendation Insights',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.bold,
                  color: isAiFallback ? const Color(0xFFD97706) : AppTheme.primaryColor,
                ),
              ),
              const Spacer(),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  color: (isAiFallback ? const Color(0xFFF59E0B) : AppTheme.primaryColor)
                      .withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  isAiFallback ? 'Fallback Mode' : 'AI Verified',
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                    color: isAiFallback ? const Color(0xFFD97706) : AppTheme.primaryColor,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            isAiFallback
                ? (agentReasoning.isNotEmpty
                    ? agentReasoning
                    : 'Showing verified direct and connecting routes matching your request corridor (AI service currently offline).')
                : agentReasoning,
            style: TextStyle(
              fontSize: 12,
              height: 1.4,
              color: isDark ? const Color(0xFFE2E8F0) : const Color(0xFF334155),
            ),
          ),
          if (isAiFallback && agentReasoning.isNotEmpty) ...[
            const SizedBox(height: 6),
            Text(
              'Showing verified direct and connecting routes matching your request corridor (AI service currently offline).',
              style: TextStyle(
                fontSize: 11,
                fontStyle: FontStyle.italic,
                color: isDark ? const Color(0xFF94A3B8) : const Color(0xFF64748B),
              ),
            ),
          ],
        ],
      ),
    );
  }
}
