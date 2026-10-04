import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../journey/screens/journey_search_screen.dart';
import '../../booking/screens/booking_history_screen.dart';
import '../../disruption/screens/disruption_alert_screen.dart';
import '../../disruption/models/disruption_models.dart';
import '../../settings/screens/passenger_settings_screen.dart';

class PassengerNavigationShell extends StatefulWidget {
  const PassengerNavigationShell({super.key});

  @override
  State<PassengerNavigationShell> createState() => _PassengerNavigationShellState();
}

class _PassengerNavigationShellState extends State<PassengerNavigationShell> {
  int _currentIndex = 0;

  Widget _buildScreen(int index) {
    switch (index) {
      case 0:
        return const JourneySearchScreen();
      case 1:
        return const BookingHistoryScreen();
      case 2:
        return DisruptionAlertScreen(disruption: DisruptionAlertModel.sampleColomboToElla());
      case 3:
        return const PassengerSettingsScreen();
      default:
        return const JourneySearchScreen();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      body: _buildScreen(_currentIndex),
      bottomNavigationBar: Container(
        decoration: BoxDecoration(
          border: Border(
            top: BorderSide(
              color: isDark ? const Color(0xFF1E293B) : const Color(0xFFE2E8F0),
              width: 1,
            ),
          ),
        ),
        child: NavigationBar(
          selectedIndex: _currentIndex,
          onDestinationSelected: (index) {
            setState(() {
              _currentIndex = index;
            });
          },
          indicatorColor: AppTheme.primaryColor.withOpacity(0.18),
          backgroundColor: isDark ? const Color(0xFF0F172A) : Colors.white,
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.explore_outlined),
              selectedIcon: Icon(Icons.explore, color: AppTheme.primaryColor),
              label: 'Explore',
            ),
            NavigationDestination(
              icon: Icon(Icons.confirmation_number_outlined),
              selectedIcon: Icon(Icons.confirmation_number, color: AppTheme.primaryColor),
              label: 'My Trips',
            ),
            NavigationDestination(
              icon: Icon(Icons.notifications_active_outlined),
              selectedIcon: Icon(Icons.notifications_active, color: AppTheme.primaryColor),
              label: 'Alerts',
            ),
            NavigationDestination(
              icon: Icon(Icons.settings_outlined),
              selectedIcon: Icon(Icons.settings, color: AppTheme.primaryColor),
              label: 'Settings',
            ),
          ],
        ),
      ),
    );
  }
}
