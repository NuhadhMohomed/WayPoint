import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/waypoint_button.dart';

class PreferenceFilterSheet extends StatefulWidget {
  final Function(Map<String, dynamic>) onApplyFilters;

  const PreferenceFilterSheet({super.key, required this.onApplyFilters});

  @override
  PreferenceFilterSheetState createState() => PreferenceFilterSheetState();
}

class PreferenceFilterSheetState extends State<PreferenceFilterSheet> {
  String _selectedBusClass = 'Any';
  int _maxTransfers = 2;
  RangeValues _priceRange = const RangeValues(0, 5000);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(24.0),
      decoration: const BoxDecoration(
        color: Colors.white, // In real app, use Theme.of(context).colorScheme.surface
        borderRadius: BorderRadius.only(
          topLeft: Radius.circular(24),
          topRight: Radius.circular(24),
        ),
      ),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                margin: const EdgeInsets.only(bottom: 24),
                decoration: BoxDecoration(
                  color: Colors.grey[300],
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            const Text(
              'Filter Journeys',
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 24),

            // Bus Class
            const Text('Bus Class', style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            DropdownButtonFormField<String>(
              initialValue: _selectedBusClass,
              decoration: const InputDecoration(
                border: OutlineInputBorder(),
                contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              ),
              items: ['Any', 'Standard', 'Semi-Luxury', 'Luxury', 'Super Luxury']
                  .map((c) => DropdownMenuItem(value: c, child: Text(c)))
                  .toList(),
              onChanged: (val) {
                if (val != null) setState(() => _selectedBusClass = val);
              },
            ),
            const SizedBox(height: 24),

            // Maximum Transfers
            const Text('Maximum Transfers', style: TextStyle(fontWeight: FontWeight.w600)),
            Slider(
              value: _maxTransfers.toDouble(),
              min: 0,
              max: 3,
              divisions: 3,
              label: _maxTransfers.toString(),
              activeColor: AppTheme.primaryColor,
              onChanged: (val) {
                setState(() => _maxTransfers = val.round());
              },
            ),
            const SizedBox(height: 24),

            // Price Range
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Price Range (LKR)', style: TextStyle(fontWeight: FontWeight.w600)),
                Text('${_priceRange.start.round()} - ${_priceRange.end.round()}'),
              ],
            ),
            RangeSlider(
              values: _priceRange,
              min: 0,
              max: 10000,
              divisions: 20,
              labels: RangeLabels(
                _priceRange.start.round().toString(),
                _priceRange.end.round().toString(),
              ),
              activeColor: AppTheme.primaryColor,
              onChanged: (val) {
                setState(() => _priceRange = val);
              },
            ),
            const SizedBox(height: 32),

            WayPointButton(
              text: 'Apply Filters',
              onPressed: () {
                widget.onApplyFilters({
                  'busClass': _selectedBusClass,
                  'maxTransfers': _maxTransfers,
                  'minPrice': _priceRange.start,
                  'maxPrice': _priceRange.end,
                });
                Navigator.pop(context);
              },
            ),
          ],
        ),
      ),
    );
  }
}
