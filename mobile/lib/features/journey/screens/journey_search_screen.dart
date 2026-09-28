import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/transit_badge.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../../../core/widgets/waypoint_card.dart';
import '../models/journey_models.dart';
import '../services/journey_api_service.dart';
import '../widgets/preference_filter_sheet.dart';
import 'journey_comparison_screen.dart';

/// MOB-02: Journey Search, Corridors & Dates
/// Stitch Screen ID: 4baf1853d7a14d7abd597916567b5370
/// Component 1: Journey Planning & Route Catalogue (Sethum)
class JourneySearchScreen extends StatefulWidget {
  const JourneySearchScreen({super.key});

  @override
  State<JourneySearchScreen> createState() => _JourneySearchScreenState();
}

class _JourneySearchScreenState extends State<JourneySearchScreen> {
  final _apiService = JourneyApiService();

  String _originCity = 'Colombo';
  String _destinationCity = 'Ella';
  DateTime _travelDate = DateTime.now().add(const Duration(days: 1));
  int _passengerCount = 1;
  JourneySearchPreferences _preferences = JourneySearchPreferences();

  bool _isSearching = false;

  final List<String> _majorCities = [
    'Colombo',
    'Kandy',
    'Galle',
    'Ella',
    'Badulla',
    'Jaffna',
    'Matara',
    'Anuradhapura',
    'Ratnapura',
  ];

  final List<Map<String, String>> _popularCorridors = [
    {'origin': 'Colombo', 'destination': 'Ella', 'tag': 'Hill Country'},
    {'origin': 'Colombo', 'destination': 'Kandy', 'tag': 'Sacred Capital'},
    {'origin': 'Colombo', 'destination': 'Galle', 'tag': 'Southern Expressway'},
    {'origin': 'Colombo', 'destination': 'Jaffna', 'tag': 'Northern Link'},
  ];

  Future<void> _selectDate(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _travelDate,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 90)),
      builder: (context, child) {
        return Theme(
          data: ThemeData.dark().copyWith(
            colorScheme: const ColorScheme.dark(
              primary: AppTheme.primaryColor,
              surface: Color(0xFF1E293B),
            ),
          ),
          child: child!,
        );
      },
    );
    if (picked != null && picked != _travelDate) {
      setState(() => _travelDate = picked);
    }
  }

  void _openPreferenceFilters() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => PreferenceFilterSheet(
        initialPreferences: _preferences,
        onApply: (updated) {
          setState(() => _preferences = updated);
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Search preferences updated.'),
              duration: Duration(seconds: 2),
            ),
          );
        },
      ),
    );
  }

  Future<void> _executeSearch() async {
    if (_originCity == _destinationCity) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Origin and destination cities must be different.'),
          backgroundColor: AppTheme.errorColor,
        ),
      );
      return;
    }

    setState(() => _isSearching = true);

    final candidates = await _apiService.searchJourneys(
      originCity: _originCity,
      destinationCity: _destinationCity,
      travelDate: _travelDate,
      passengerCount: _passengerCount,
      preferences: _preferences,
    );

    if (!mounted) return;
    setState(() => _isSearching = false);

    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => JourneyComparisonScreen(
          originCity: _originCity,
          destinationCity: _destinationCity,
          travelDate: _travelDate,
          candidates: candidates,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('EEE, dd MMM yyyy');

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header Badge & Title
          const Row(
            children: [
              TransitBadge(status: TransitStatus.express, customLabel: 'Component 1'),
              SizedBox(width: 8),
              Text(
                'Sethum',
                style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13, fontWeight: FontWeight.bold),
              ),
            ],
          ),
          const SizedBox(height: 10),
          const Text(
            'Intercity Journey Planner',
            style: TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 4),
          const Text(
            'Search scheduled direct and connecting bus corridors across Sri Lanka.',
            style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
          ),
          const SizedBox(height: 20),

          // Main Search Card
          WayPointCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Origin Selector
                const Text(
                  'FROM (ORIGIN)',
                  style: TextStyle(color: Color(0xFF64748B), fontSize: 11, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 6),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 14),
                  decoration: BoxDecoration(
                    color: const Color(0xFF0F172A),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: const Color(0xFF334155)),
                  ),
                  child: DropdownButtonHideUnderline(
                    child: DropdownButton<String>(
                      value: _originCity,
                      isExpanded: true,
                      dropdownColor: const Color(0xFF1E293B),
                      icon: const Icon(Icons.keyboard_arrow_down, color: Color(0xFF94A3B8)),
                      style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
                      items: _majorCities.map((city) {
                        return DropdownMenuItem(value: city, child: Text(city));
                      }).toList(),
                      onChanged: (val) {
                        if (val != null) setState(() => _originCity = val);
                      },
                    ),
                  ),
                ),
                const SizedBox(height: 14),

                // Swap Origin & Destination Button
                Center(
                  child: IconButton(
                    icon: const Icon(Icons.swap_vert_circle, color: AppTheme.primaryColor, size: 28),
                    onPressed: () {
                      setState(() {
                        final temp = _originCity;
                        _originCity = _destinationCity;
                        _destinationCity = temp;
                      });
                    },
                  ),
                ),

                // Destination Selector
                const Text(
                  'TO (DESTINATION)',
                  style: TextStyle(color: Color(0xFF64748B), fontSize: 11, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 6),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 14),
                  decoration: BoxDecoration(
                    color: const Color(0xFF0F172A),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: const Color(0xFF334155)),
                  ),
                  child: DropdownButtonHideUnderline(
                    child: DropdownButton<String>(
                      value: _destinationCity,
                      isExpanded: true,
                      dropdownColor: const Color(0xFF1E293B),
                      icon: const Icon(Icons.keyboard_arrow_down, color: Color(0xFF94A3B8)),
                      style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
                      items: _majorCities.map((city) {
                        return DropdownMenuItem(value: city, child: Text(city));
                      }).toList(),
                      onChanged: (val) {
                        if (val != null) setState(() => _destinationCity = val);
                      },
                    ),
                  ),
                ),
                const SizedBox(height: 18),

                // Date Picker & Passenger Counter
                Row(
                  children: [
                    // Date
                    Expanded(
                      flex: 3,
                      child: InkWell(
                        onTap: () => _selectDate(context),
                        borderRadius: BorderRadius.circular(10),
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                          decoration: BoxDecoration(
                            color: const Color(0xFF0F172A),
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(color: const Color(0xFF334155)),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'TRAVEL DATE',
                                style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold),
                              ),
                              const SizedBox(height: 4),
                              Row(
                                children: [
                                  const Icon(Icons.calendar_month, size: 16, color: Color(0xFF818CF8)),
                                  const SizedBox(width: 6),
                                  Text(
                                    dateFormat.format(_travelDate),
                                    style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w600),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),

                    // Passengers Stepper
                    Expanded(
                      flex: 2,
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                        decoration: BoxDecoration(
                          color: const Color(0xFF0F172A),
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(color: const Color(0xFF334155)),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'PASSENGERS',
                              style: TextStyle(color: Color(0xFF64748B), fontSize: 10, fontWeight: FontWeight.bold),
                            ),
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                IconButton(
                                  padding: EdgeInsets.zero,
                                  constraints: const BoxConstraints(),
                                  icon: const Icon(Icons.remove, size: 16, color: Colors.white70),
                                  onPressed: _passengerCount > 1
                                      ? () => setState(() => _passengerCount--)
                                      : null,
                                ),
                                Text(
                                  '$_passengerCount',
                                  style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.bold),
                                ),
                                IconButton(
                                  padding: EdgeInsets.zero,
                                  constraints: const BoxConstraints(),
                                  icon: const Icon(Icons.add, size: 16, color: Colors.white70),
                                  onPressed: _passengerCount < 6
                                      ? () => setState(() => _passengerCount++)
                                      : null,
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 18),

                // Preference Filter Sheet Launcher
                InkWell(
                  onTap: _openPreferenceFilters,
                  borderRadius: BorderRadius.circular(8),
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                    decoration: BoxDecoration(
                      color: const Color(0xFF0F172A).withAlpha(128),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: const Color(0xFF334155)),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            const Icon(Icons.tune, size: 16, color: Color(0xFF818CF8)),
                            const SizedBox(width: 8),
                            Text(
                              _preferences.directOnly
                                  ? 'Direct Only • Max Rs. ${_preferences.maxFare.toInt()}'
                                  : 'Connecting Feasible • ${_preferences.departureWindow}',
                              style: const TextStyle(color: Color(0xFFCBD5E1), fontSize: 12),
                            ),
                          ],
                        ),
                        const Text(
                          'Edit',
                          style: TextStyle(color: Color(0xFF818CF8), fontSize: 12, fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 20),

                // Search Action Button
                WayPointButton(
                  text: _isSearching ? 'Analyzing Corridors...' : 'Search Journeys (MOB-02)',
                  icon: Icons.search,
                  isLoading: _isSearching,
                  onPressed: _isSearching ? null : _executeSearch,
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),

          // Popular Sri Lankan Corridors Quick Chips
          const Text(
            'POPULAR SCENIC CORRIDORS',
            style: TextStyle(color: Color(0xFF64748B), fontSize: 11, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: _popularCorridors.map((c) {
              return ActionChip(
                backgroundColor: const Color(0xFF1E293B),
                side: const BorderSide(color: Color(0xFF334155)),
                label: Text(
                  '${c['origin']} → ${c['destination']}',
                  style: const TextStyle(color: Colors.white, fontSize: 12),
                ),
                avatar: const Icon(Icons.navigation, size: 14, color: AppTheme.primaryColor),
                onPressed: () {
                  setState(() {
                    _originCity = c['origin']!;
                    _destinationCity = c['destination']!;
                  });
                },
              );
            }).toList(),
          ),
        ],
      ),
    );
  }
}
