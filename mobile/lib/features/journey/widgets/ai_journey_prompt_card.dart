import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../../core/theme/app_theme.dart';

class AiJourneyPromptCard extends StatefulWidget {
  final bool isSearching;
  final ValueChanged<String> onSubmit;

  const AiJourneyPromptCard({
    super.key,
    required this.isSearching,
    required this.onSubmit,
  });

  @override
  State<AiJourneyPromptCard> createState() => _AiJourneyPromptCardState();
}

class _AiJourneyPromptCardState extends State<AiJourneyPromptCard> {
  final _controller = TextEditingController();

  final List<Map<String, String>> _presets = [
    {
      'label': 'Scenic route to Ella',
      'prompt': 'Scenic route to Ella with AC',
    },
    {
      'label': 'Fastest to Kandy',
      'prompt': 'Fastest to Kandy before noon',
    },
    {
      'label': 'Luxury to Galle',
      'prompt': 'Luxury AC bus to Galle',
    },
    {
      'label': 'Comfort & legroom',
      'prompt': 'Semi-luxury with extra legroom',
    },
  ];

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _applyPreset(String prompt) {
    HapticFeedback.selectionClick();
    setState(() {
      _controller.text = prompt;
    });
  }

  void _submit() {
    final text = _controller.text.trim();
    if (text.isNotEmpty && !widget.isSearching) {
      widget.onSubmit(text);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final cardBg = isDark ? const Color(0xFF1E293B) : Colors.white;
    final borderColor = isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0);
    final textMuted = isDark ? const Color(0xFF94A3B8) : const Color(0xFF64748B);

    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardBg,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: borderColor),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.04),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header badge
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: AppTheme.primaryColor.withOpacity(0.12),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: const Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      Icons.auto_awesome,
                      size: 14,
                      color: AppTheme.primaryColor,
                    ),
                    SizedBox(width: 6),
                    Text(
                      'AI Trip Planner',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.primaryColor,
                      ),
                    ),
                  ],
                ),
              ),
              const Spacer(),
              Text(
                'Natural Language Search',
                style: TextStyle(
                  fontSize: 11,
                  color: textMuted,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),

          // Input field
          TextField(
            controller: _controller,
            maxLines: 2,
            minLines: 1,
            textInputAction: TextInputAction.search,
            onSubmitted: (_) => _submit(),
            decoration: InputDecoration(
              hintText: "Ask AI: 'Morning bus to Ella with AC for 2 under Rs. 5,000'",
              hintStyle: TextStyle(fontSize: 13, color: textMuted),
              filled: true,
              fillColor: isDark ? const Color(0xFF0F172A) : const Color(0xFFF8FAFC),
              contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: BorderSide(color: borderColor),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: BorderSide(color: borderColor),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: AppTheme.primaryColor, width: 1.5),
              ),
            ),
          ),
          const SizedBox(height: 10),

          // Presets chips scroll
          SizedBox(
            height: 32,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: _presets.length,
              separatorBuilder: (_, __) => const SizedBox(width: 8),
              itemBuilder: (context, index) {
                final preset = _presets[index];
                return ActionChip(
                  label: Text(
                    preset['label']!,
                    style: const TextStyle(fontSize: 11),
                  ),
                  backgroundColor: isDark ? const Color(0xFF0F172A) : const Color(0xFFF1F5F9),
                  side: BorderSide(color: borderColor),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                  padding: const EdgeInsets.symmetric(horizontal: 4),
                  onPressed: () => _applyPreset(preset['prompt']!),
                );
              },
            ),
          ),
          const SizedBox(height: 14),

          // Action button
          SizedBox(
            width: double.infinity,
            height: 44,
            child: ElevatedButton.icon(
              onPressed: widget.isSearching ? null : _submit,
              icon: widget.isSearching
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.auto_awesome, size: 18),
              label: Text(
                widget.isSearching ? 'Analyzing with AI...' : 'Ask AI',
                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.primaryColor,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                elevation: 0,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
