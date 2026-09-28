import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../models/journey_models.dart';

/// MOB-03: Preference Filter Sheet & Sliders
/// Stitch Screen ID: fb4b74ea904c435b93f05e9dc324e989
/// Component 1: Journey Planning & Route Catalogue (Sethum)
class PreferenceFilterSheet extends StatefulWidget {
  final JourneySearchPreferences initialPreferences;
  final ValueChanged<JourneySearchPreferences> onApply;

  const PreferenceFilterSheet({
    super.key,
    required this.initialPreferences,
    required this.onApply,
  });

  @override
  State<PreferenceFilterSheet> createState() => _PreferenceFilterSheetState();
}

class _PreferenceFilterSheetState extends State<PreferenceFilterSheet> {
  late bool _directOnly;
  late bool _requireAc;
  late double _maxFare;
  late String _departureWindow;

  final _currencyFormat = NumberFormat.currency(
    locale: 'en_LK',
    symbol: 'Rs. ',
    decimalDigits: 0,
  );

  @override
  void initState() {
    super.initState();
    _directOnly = widget.initialPreferences.directOnly;
    _requireAc = widget.initialPreferences.requireAc;
    _maxFare = widget.initialPreferences.maxFare;
    _departureWindow = widget.initialPreferences.departureWindow;
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: Color(0xFF1E293B), // Slate 800
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Drag handle
          Center(
            child: Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: Colors.white24,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Header
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Filter & Travel Preferences',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                ),
              ),
              TextButton(
                onPressed: () {
                  setState(() {
                    _directOnly = false;
                    _requireAc = true;
                    _maxFare = 4000.0;
                    _departureWindow = 'Any';
                  });
                },
                child: const Text('Reset', style: TextStyle(color: Color(0xFF818CF8))),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Direct Only Switch
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text(
              'Direct Corridors Only',
              style: TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
            ),
            subtitle: const Text(
              'Exclude connecting services via intermediate hubs',
              style: TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
            ),
            value: _directOnly,
            onChanged: (val) => setState(() => _directOnly = val),
          ),

          const Divider(color: Color(0xFF334155), height: 24),

          // Air Conditioned Switch
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text(
              'Air-Conditioned Bus (AC)',
              style: TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
            ),
            subtitle: const Text(
              'Filter for luxury super line and expressway air-conditioned coaches',
              style: TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
            ),
            value: _requireAc,
            onChanged: (val) => setState(() => _requireAc = val),
          ),

          const Divider(color: Color(0xFF334155), height: 24),

          // Max Fare Slider
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Maximum Ticket Fare',
                style: TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
              ),
              Text(
                _currencyFormat.format(_maxFare),
                style: const TextStyle(
                  color: Color(0xFF22C55E),
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          Slider(
            value: _maxFare,
            min: 500.0,
            max: 5000.0,
            divisions: 45,
            activeColor: AppTheme.primaryColor,
            inactiveColor: const Color(0xFF334155),
            label: _currencyFormat.format(_maxFare),
            onChanged: (val) => setState(() => _maxFare = val),
          ),

          const Divider(color: Color(0xFF334155), height: 24),

          // Departure Time Window
          const Text(
            'Preferred Departure Window',
            style: TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 8,
            children: ['Any', 'Morning', 'Afternoon', 'Evening', 'Night'].map((win) {
              final isSelected = _departureWindow == win;
              return ChoiceChip(
                label: Text(win),
                selected: isSelected,
                selectedColor: AppTheme.primaryColor,
                backgroundColor: const Color(0xFF0F172A),
                labelStyle: TextStyle(
                  color: isSelected ? Colors.white : const Color(0xFF94A3B8),
                  fontSize: 12,
                  fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                ),
                onSelected: (selected) {
                  if (selected) {
                    setState(() => _departureWindow = win);
                  }
                },
              );
            }).toList(),
          ),

          const SizedBox(height: 24),

          // Apply Button
          WayPointButton(
            text: 'Apply Filter Preferences',
            onPressed: () {
              widget.onApply(
                JourneySearchPreferences(
                  directOnly: _directOnly,
                  requireAc: _requireAc,
                  maxFare: _maxFare,
                  departureWindow: _departureWindow,
                ),
              );
              Navigator.of(context).pop();
            },
          ),
        ],
      ),
    );
  }
}
