import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/waypoint_card.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../../../core/widgets/transit_badge.dart';
import '../../fleet/screens/conductor_scanner_screen.dart';
import '../../fleet/screens/conductor_manifest_screen.dart';
import '../../auth/bloc/auth_cubit.dart';

class ConductorNavigationShell extends StatefulWidget {
  const ConductorNavigationShell({super.key});

  @override
  State<ConductorNavigationShell> createState() => _ConductorNavigationShellState();
}

class _ConductorNavigationShellState extends State<ConductorNavigationShell> {
  int _currentIndex = 0;

  Widget _buildScreen(int index) {
    switch (index) {
      case 0:
        return const _ConductorServicesTab();
      case 1:
        return const ConductorScannerScreen();
      case 2:
        return const ConductorManifestScreen();
      case 3:
        return const _ConductorProfileTab();
      default:
        return const _ConductorServicesTab();
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
              icon: Icon(Icons.directions_bus_outlined),
              selectedIcon: Icon(Icons.directions_bus, color: AppTheme.primaryColor),
              label: 'Services',
            ),
            NavigationDestination(
              icon: Icon(Icons.qr_code_scanner),
              selectedIcon: Icon(Icons.qr_code_scanner, color: AppTheme.primaryColor),
              label: 'Scanner',
            ),
            NavigationDestination(
              icon: Icon(Icons.list_alt),
              selectedIcon: Icon(Icons.list_alt, color: AppTheme.primaryColor),
              label: 'Manifest',
            ),
            NavigationDestination(
              icon: Icon(Icons.person_outline),
              selectedIcon: Icon(Icons.person, color: AppTheme.primaryColor),
              label: 'Profile',
            ),
          ],
        ),
      ),
    );
  }
}

class _ConductorServicesTab extends StatelessWidget {
  const _ConductorServicesTab();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Assigned Transit Services'),
        elevation: 0,
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const Text(
            'TODAY\'S ASSIGNMENTS',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.2,
              color: Colors.grey,
            ),
          ),
          const SizedBox(height: 12),
          WayPointCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    TransitBadge(status: TransitStatus.available, customLabel: 'Active Service'),
                    Text(
                      'Bus ND-8821',
                      style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                const Text(
                  'Colombo Fort -> Ella Superline Express',
                  style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Departure: 07:30 AM * Platform Bay 04',
                  style: TextStyle(fontSize: 13, color: Colors.grey),
                ),
                const SizedBox(height: 14),
                const LinearProgressIndicator(
                  value: 0.82,
                  backgroundColor: Color(0xFF1E293B),
                  valueColor: AlwaysStoppedAnimation<Color>(AppTheme.primaryColor),
                ),
                const SizedBox(height: 8),
                const Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Boarded: 33 / 40 seats', style: TextStyle(fontSize: 12, color: Colors.grey)),
                    Text('82% Capacity', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ConductorProfileTab extends StatelessWidget {
  const _ConductorProfileTab();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Conductor Staff Profile'),
        elevation: 0,
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const WayPointCard(
            child: Row(
              children: [
                CircleAvatar(
                  radius: 28,
                  backgroundColor: AppTheme.primaryColor,
                  child: Icon(Icons.badge, color: AppTheme.onPrimaryColor, size: 30),
                ),
                SizedBox(width: 16),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'CONDUCTOR STAFF',
                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'Staff ID: COND-9912 * Colombo Depot',
                      style: TextStyle(fontSize: 13, color: Colors.grey),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          WayPointButton(
            text: 'Sign Out Staff Session',
            variant: WayPointButtonVariant.outline,
            icon: Icons.logout,
            onPressed: () {
              context.read<AuthCubit>().logout();
            },
          ),
        ],
      ),
    );
  }
}
