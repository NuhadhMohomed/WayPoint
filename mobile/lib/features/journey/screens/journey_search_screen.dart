import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../../../core/widgets/waypoint_card.dart';
import '../models/journey_models.dart';
import '../services/journey_api_service.dart';
import '../widgets/preference_filter_sheet.dart';
import '../widgets/ai_journey_prompt_card.dart';
import '../widgets/first_run_welcome_card.dart';
import 'journey_comparison_screen.dart';

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
  bool _isAiSearching = false;
  bool _showFirstRunWelcome = true;

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
    'Trincomalee',
    'Negombo',
    'Nuwara Eliya',
  ];

  final List<Map<String, String>> _popularCorridors = [
    {
      'origin': 'Colombo',
      'destination': 'Ella',
      'tag': 'Hill Country Scenic',
      'time': '5h 30m',
      'fare': 'Rs. 2,400',
    },
    {
      'origin': 'Colombo',
      'destination': 'Kandy',
      'tag': 'Central Expressway',
      'time': '3h 15m',
      'fare': 'Rs. 1,600',
    },
    {
      'origin': 'Colombo',
      'destination': 'Galle',
      'tag': 'Southern Coastal',
      'time': '2h 10m',
      'fare': 'Rs. 1,200',
    },
    {
      'origin': 'Colombo',
      'destination': 'Jaffna',
      'tag': 'Northern Link',
      'time': '8h 00m',
      'fare': 'Rs. 3,800',
    },
  ];

  Future<void> _selectDate(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _travelDate,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 90)),
      builder: (context, child) {
        final isDark = Theme.of(context).brightness == Brightness.dark;
        return Theme(
          data: isDark
              ? ThemeData.dark().copyWith(
                  colorScheme: const ColorScheme.dark(
                    primary: AppTheme.primaryColor,
                    surface: Color(0xFF1E293B),
                  ),
                )
              : ThemeData.light().copyWith(
                  colorScheme: const ColorScheme.light(
                    primary: AppTheme.primaryColor,
                    surface: Colors.white,
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

    setState(() {
      _isSearching = true;
      _showFirstRunWelcome = false;
    });

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

  Future<void> _executeAiSearch(String objective) async {
    setState(() => _isAiSearching = true);

    try {
      final result = await _apiService.getAiRecommendations(
        objective: objective,
        passengerCount: _passengerCount,
        travelDate: _travelDate,
      );

      if (!mounted) return;
      setState(() => _isAiSearching = false);

      Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => JourneyComparisonScreen(
            originCity: _originCity,
            destinationCity: _destinationCity,
            travelDate: _travelDate,
            candidates: result.candidates,
            agentReasoning: result.agentReasoning,
            isAiFallback: result.isAiFallback,
          ),
        ),
      );
    } catch (_) {
      if (!mounted) return;
      setState(() => _isAiSearching = false);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Failed to get AI recommendations. Please try standard search.'),
          backgroundColor: AppTheme.errorColor,
        ),
      );
    }
  }

  void _setDatePreset(int daysFromNow) {
    HapticFeedback.selectionClick();
    setState(() {
      _travelDate = DateTime.now().add(Duration(days: daysFromNow));
    });
  }

  void _setWeekendPreset() {
    HapticFeedback.selectionClick();
    final now = DateTime.now();
    final daysUntilSaturday = (DateTime.saturday - now.weekday + 7) % 7;
    setState(() {
      _travelDate = now.add(Duration(days: daysUntilSaturday == 0 ? 7 : daysUntilSaturday));
    });
  }

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('EEE, dd MMM yyyy');
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final surfaceColor = isDark ? const Color(0xFF0F172A) : const Color(0xFFF8FAFC);
    final borderColor = isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0);
    final textMuted = isDark ? const Color(0xFF94A3B8) : const Color(0xFF64748B);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Book Intercity Travel'),
        elevation: 0,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (_showFirstRunWelcome)
              FirstRunWelcomeCard(
                onSelectCorridor: (orig, dest) {
                  HapticFeedback.lightImpact();
                  setState(() {
                    _originCity = orig;
                    _destinationCity = dest;
                  });
                },
                onDismiss: () => setState(() => _showFirstRunWelcome = false),
              ),

            // AI Journey Prompt Card
            AiJourneyPromptCard(
              isSearching: _isAiSearching,
              onSubmit: _executeAiSearch,
            ),

            // Explore Corridors Header & Carousel
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Explore Corridors',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    letterSpacing: -0.2,
                  ),
                ),
                Text(
                  'Popular Routes',
                  style: TextStyle(
                    fontSize: 12,
                    color: textMuted,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            SizedBox(
              height: 120,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                itemCount: _popularCorridors.length,
                separatorBuilder: (_, __) => const SizedBox(width: 12),
                itemBuilder: (context, index) {
                  final corridor = _popularCorridors[index];
                  final isSelected = _originCity == corridor['origin'] && _destinationCity == corridor['destination'];
                  return GestureDetector(
                    onTap: () {
                      HapticFeedback.lightImpact();
                      setState(() {
                        _originCity = corridor['origin']!;
                        _destinationCity = corridor['destination']!;
                      });
                    },
                    child: Container(
                      width: 210,
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: isSelected
                            ? AppTheme.primaryColor.withValues(alpha: isDark ? 0.18 : 0.1)
                            : (isDark ? const Color(0xFF1E293B) : Colors.white),
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(
                          color: isSelected ? AppTheme.primaryColor : borderColor,
                          width: isSelected ? 1.8 : 1.0,
                        ),
                        boxShadow: isDark
                            ? null
                            : [
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: 0.04),
                                  blurRadius: 8,
                                  offset: const Offset(0, 2),
                                ),
                              ],
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Flexible(
                                child: Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                  decoration: BoxDecoration(
                                    color: AppTheme.primaryColor.withValues(alpha: 0.2),
                                    borderRadius: BorderRadius.circular(6),
                                  ),
                                  child: Text(
                                    corridor['tag']!,
                                    style: const TextStyle(
                                      fontSize: 10,
                                      fontWeight: FontWeight.w600,
                                      color: AppTheme.primaryColor,
                                    ),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                              const SizedBox(width: 6),
                              Text(
                                corridor['fare']!,
                                style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                          Row(
                            children: [
                              Flexible(
                                child: Text(
                                  corridor['origin']!,
                                  style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                              const Padding(
                                padding: EdgeInsets.symmetric(horizontal: 4),
                                child: Icon(Icons.arrow_forward, size: 14, color: AppTheme.primaryColor),
                              ),
                              Flexible(
                                child: Text(
                                  corridor['destination']!,
                                  style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            ],
                          ),
                          Text(
                            'Duration: ${corridor['time']}',
                            style: TextStyle(fontSize: 11, color: textMuted),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ),
            const SizedBox(height: 20),

            // Main Search Card
            WayPointCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Origin Selector
                  Text(
                    'FROM (ORIGIN)',
                    style: TextStyle(color: textMuted, fontSize: 11, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 6),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14),
                    decoration: BoxDecoration(
                      color: surfaceColor,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: borderColor),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<String>(
                        value: _originCity,
                        isExpanded: true,
                        dropdownColor: isDark ? const Color(0xFF1E293B) : Colors.white,
                        icon: Icon(Icons.keyboard_arrow_down, color: textMuted),
                        style: TextStyle(
                          color: isDark ? Colors.white : Colors.black87,
                          fontSize: 15,
                          fontWeight: FontWeight.w600,
                        ),
                        items: _majorCities.map((city) {
                          return DropdownMenuItem(value: city, child: Text(city));
                        }).toList(),
                        onChanged: (val) {
                          if (val != null) setState(() => _originCity = val);
                        },
                      ),
                    ),
                  ),
                  const SizedBox(height: 10),

                  // Quick Swap Button
                  Center(
                    child: IconButton(
                      icon: const Icon(Icons.swap_vert, color: AppTheme.primaryColor, size: 28),
                      tooltip: 'Swap origin and destination',
                      onPressed: () {
                        HapticFeedback.lightImpact();
                        setState(() {
                          final temp = _originCity;
                          _originCity = _destinationCity;
                          _destinationCity = temp;
                        });
                      },
                    ),
                  ),
                  const SizedBox(height: 2),

                  // Destination Selector
                  Text(
                    'TO (DESTINATION)',
                    style: TextStyle(color: textMuted, fontSize: 11, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 6),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14),
                    decoration: BoxDecoration(
                      color: surfaceColor,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: borderColor),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<String>(
                        value: _destinationCity,
                        isExpanded: true,
                        dropdownColor: isDark ? const Color(0xFF1E293B) : Colors.white,
                        icon: Icon(Icons.keyboard_arrow_down, color: textMuted),
                        style: TextStyle(
                          color: isDark ? Colors.white : Colors.black87,
                          fontSize: 15,
                          fontWeight: FontWeight.w600,
                        ),
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

                  // Travel Date & Passengers Row
                  Row(
                    children: [
                      // Date selector
                      Expanded(
                        flex: 3,
                        child: InkWell(
                          onTap: () => _selectDate(context),
                          borderRadius: BorderRadius.circular(10),
                          child: Container(
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                            decoration: BoxDecoration(
                              color: surfaceColor,
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(color: borderColor),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'TRAVEL DATE',
                                  style: TextStyle(color: textMuted, fontSize: 10, fontWeight: FontWeight.bold),
                                ),
                                const SizedBox(height: 4),
                                Row(
                                  children: [
                                    const Icon(Icons.calendar_month, size: 16, color: AppTheme.primaryColor),
                                    const SizedBox(width: 6),
                                    Expanded(
                                      child: Text(
                                        dateFormat.format(_travelDate),
                                        style: TextStyle(
                                          color: isDark ? Colors.white : Colors.black87,
                                          fontSize: 13,
                                          fontWeight: FontWeight.w600,
                                        ),
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),

                      // Passenger Stepper
                      Expanded(
                        flex: 2,
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                          decoration: BoxDecoration(
                            color: surfaceColor,
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(color: borderColor),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'PASSENGERS',
                                style: TextStyle(color: textMuted, fontSize: 10, fontWeight: FontWeight.bold),
                              ),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  IconButton(
                                    padding: EdgeInsets.zero,
                                    constraints: const BoxConstraints(),
                                    icon: Icon(
                                      Icons.remove,
                                      size: 16,
                                      color: _passengerCount > 1
                                          ? (isDark ? Colors.white : Colors.black87)
                                          : textMuted,
                                    ),
                                    onPressed: _passengerCount > 1
                                        ? () {
                                            HapticFeedback.selectionClick();
                                            setState(() => _passengerCount--);
                                          }
                                        : null,
                                  ),
                                  Text(
                                    '$_passengerCount',
                                    style: TextStyle(
                                      color: isDark ? Colors.white : Colors.black87,
                                      fontSize: 15,
                                      fontWeight: FontWeight.bold,
                                    ),
                                  ),
                                  IconButton(
                                    padding: EdgeInsets.zero,
                                    constraints: const BoxConstraints(),
                                    icon: Icon(
                                      Icons.add,
                                      size: 16,
                                      color: _passengerCount < 6
                                          ? (isDark ? Colors.white : Colors.black87)
                                          : textMuted,
                                    ),
                                    onPressed: _passengerCount < 6
                                        ? () {
                                            HapticFeedback.selectionClick();
                                            setState(() => _passengerCount++);
                                          }
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
                  const SizedBox(height: 12),

                  // Quick Date Chips
                  Wrap(
                    spacing: 8,
                    children: [
                      ActionChip(
                        label: const Text('Today', style: TextStyle(fontSize: 12)),
                        onPressed: () => _setDatePreset(0),
                      ),
                      ActionChip(
                        label: const Text('Tomorrow', style: TextStyle(fontSize: 12)),
                        onPressed: () => _setDatePreset(1),
                      ),
                      ActionChip(
                        label: const Text('Weekend', style: TextStyle(fontSize: 12)),
                        onPressed: _setWeekendPreset,
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),

                  // Preference Filter Trigger
                  InkWell(
                    onTap: _openPreferenceFilters,
                    borderRadius: BorderRadius.circular(8),
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                      decoration: BoxDecoration(
                        color: surfaceColor,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: borderColor),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Row(
                            children: [
                              const Icon(Icons.tune, size: 16, color: AppTheme.primaryColor),
                              const SizedBox(width: 8),
                              Text(
                                _preferences.directOnly
                                    ? 'Direct Only • Max Rs. ${_preferences.maxFare.toInt()}'
                                    : 'Connecting Feasible • ${_preferences.departureWindow}',
                                style: TextStyle(
                                  color: isDark ? const Color(0xFFCBD5E1) : const Color(0xFF475569),
                                  fontSize: 12,
                                ),
                              ),
                            ],
                          ),
                          const Text(
                            'Edit',
                            style: TextStyle(
                              color: AppTheme.primaryColor,
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Search Action Button
                  WayPointButton(
                    text: _isSearching ? 'Analyzing Corridors...' : 'Search Buses',
                    icon: Icons.search,
                    isLoading: _isSearching,
                    onPressed: _isSearching ? null : _executeSearch,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
