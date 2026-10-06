import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/theme/app_theme.dart';
import '../../auth/bloc/auth_cubit.dart';
import '../../auth/models/auth_models.dart';
import '../../auth/screens/passenger_auth_screen.dart';

import '../../journey/screens/journey_search_screen.dart';
import '../../booking/screens/seat_picker_screen.dart';
import '../../booking/screens/ticket_wallet_screen.dart';
import '../../settings/screens/passenger_settings_screen.dart';
import '../../fleet/data/fleet_api_service.dart';

class PassengerNavigationShell extends StatefulWidget {
  final UserModel? user;

  const PassengerNavigationShell({super.key, this.user});

  @override
  State<PassengerNavigationShell> createState() => _PassengerNavigationShellState();
}

class _PassengerNavigationShellState extends State<PassengerNavigationShell> {
  int _currentIndex = 0;

  @override
  Widget build(BuildContext context) {
    final List<Widget> pages = [
      // 0: Journey Search (Component 1 - Sethum)
      const JourneySearchScreen(),
      // 1: Seat Matrix (Component 2 - Nuhadh)
      SeatPickerScreen(
        serviceId: 'srv-colombo-galle-01',
        apiService: FleetApiService(),
      ),
      // 2: Ticket Wallet (Component 3 - Mithila)
      const TicketWalletScreen(),
      // 3: Profile & Settings
      _buildProfileTab(context),
    ];

    return Scaffold(
      body: SafeArea(child: pages[_currentIndex]),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (idx) => setState(() => _currentIndex = idx),
        indicatorColor: AppTheme.secondaryColor.withValues(alpha: 0.2),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.explore_outlined),
            selectedIcon: Icon(Icons.explore_rounded, color: AppTheme.primaryColor),
            label: 'Explore',
          ),
          NavigationDestination(
            icon: Icon(Icons.event_seat_outlined),
            selectedIcon: Icon(Icons.event_seat_rounded, color: AppTheme.primaryColor),
            label: 'Seats',
          ),
          NavigationDestination(
            icon: Icon(Icons.confirmation_num_outlined),
            selectedIcon: Icon(Icons.confirmation_num_rounded, color: AppTheme.primaryColor),
            label: 'Tickets',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person_rounded, color: AppTheme.primaryColor),
            label: 'Profile',
          ),
        ],
      ),
    );
  }


  Widget _buildProfileTab(BuildContext context) {
    final user = widget.user;

    return Padding(
      padding: const EdgeInsets.all(24.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 30,
                backgroundColor: AppTheme.primaryColor,
                child: Text(
                  user != null && user.fullName.isNotEmpty
                      ? user.fullName[0].toUpperCase()
                      : 'G',
                  style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.white),
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      user != null ? user.fullName : 'Guest Explorer',
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                    ),
                    Text(
                      user != null ? user.email : 'Explore routes and schedules freely',
                      style: TextStyle(color: Colors.grey[500], fontSize: 13),
                    ),
                    const SizedBox(height: 4),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: AppTheme.primaryColor.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Text(
                        (user != null ? user.role : 'GUEST').toUpperCase(),
                        style: const TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                          color: AppTheme.primaryColor,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 32),
          const Text(
            'App Settings',
            style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: Colors.grey),
          ),
          const SizedBox(height: 8),
          const ListTile(
            leading: Icon(Icons.wb_sunny_outlined, color: AppTheme.primaryColor),
            title: Text('Visual Theme'),
            subtitle: Text('Sovereign Transit Light Theme', style: TextStyle(fontSize: 12, color: Colors.grey)),
          ),
          ListTile(
            leading: const Icon(Icons.tune, color: AppTheme.primaryColor),
            title: const Text('Preferences & Travelers'),
            subtitle: const Text('Transit alerts, saved travelers & helpline', style: TextStyle(fontSize: 12, color: Colors.grey)),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const PassengerSettingsScreen()),
              );
            },
          ),
          const Divider(),
          const Spacer(),
          SizedBox(
            width: double.infinity,
            child: user != null
                ? OutlinedButton.icon(
                    onPressed: () => context.read<AuthCubit>().logout(),
                    icon: const Icon(Icons.logout, color: AppTheme.errorColor),
                    label: const Text('Sign Out', style: TextStyle(color: AppTheme.errorColor)),
                    style: OutlinedButton.styleFrom(
                      side: const BorderSide(color: AppTheme.errorColor),
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                  )
                : ElevatedButton.icon(
                    onPressed: () {
                      Navigator.of(context).push(
                        MaterialPageRoute(builder: (_) => const PassengerAuthScreen()),
                      );
                    },
                    icon: const Icon(Icons.login),
                    label: const Text('Sign In / Register'),
                  ),
          ),
        ],
      ),
    );
  }
}
