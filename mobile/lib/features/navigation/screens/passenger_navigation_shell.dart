import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/theme/theme_cubit.dart';
import '../../auth/bloc/auth_cubit.dart';
import '../../auth/models/auth_models.dart';

class PassengerNavigationShell extends StatefulWidget {
  final UserModel user;

  const PassengerNavigationShell({super.key, required this.user});

  @override
  State<PassengerNavigationShell> createState() => _PassengerNavigationShellState();
}

class _PassengerNavigationShellState extends State<PassengerNavigationShell> {
  int _currentIndex = 0;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final List<Widget> pages = [
      // 0: Journey Search Placeholder (Replaced in Task 3)
      _buildPlaceholderTab(
        title: 'Journey Search & Catalog',
        subtitle: 'Find intercity coaches across Sri Lanka',
        icon: Icons.search_rounded,
        actionText: 'Explore Routes',
      ),
      // 1: Seat Matrix (Replaced in Task 4)
      _buildPlaceholderTab(
        title: 'Seat Layout & Coach Feasibility',
        subtitle: 'Interactive seat selection matrix',
        icon: Icons.event_seat_rounded,
        actionText: 'Select Coach',
      ),
      // 2: Ticket Wallet (Replaced in Task 5)
      _buildPlaceholderTab(
        title: 'My Tickets Wallet',
        subtitle: 'Active QR boarding passes & history',
        icon: Icons.confirmation_number_rounded,
        actionText: 'View E-Tickets',
      ),
      // 3: Profile & Disruption Alerts
      _buildProfileTab(context, isDark),
    ];

    return Scaffold(
      body: SafeArea(child: pages[_currentIndex]),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (idx) => setState(() => _currentIndex = idx),
        indicatorColor: AppTheme.secondaryColor.withOpacity(0.2),
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

  Widget _buildPlaceholderTab({
    required String title,
    required String subtitle,
    required IconData icon,
    required String actionText,
  }) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: AppTheme.primaryColor.withOpacity(0.1),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, size: 48, color: AppTheme.primaryColor),
            ),
            const SizedBox(height: 16),
            Text(
              title,
              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              subtitle,
              style: const TextStyle(color: Colors.grey, fontSize: 13),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildProfileTab(BuildContext context, bool isDark) {
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
                  widget.user.fullName.isNotEmpty ? widget.user.fullName[0].toUpperCase() : 'U',
                  style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.white),
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      widget.user.fullName,
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                    ),
                    Text(
                      widget.user.email,
                      style: TextStyle(color: Colors.grey[500], fontSize: 13),
                    ),
                    const SizedBox(height: 4),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: AppTheme.primaryColor.withOpacity(0.15),
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Text(
                        widget.user.role.toUpperCase(),
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
          ListTile(
            leading: const Icon(Icons.dark_mode_outlined),
            title: const Text('Dark Mode'),
            trailing: Switch(
              value: isDark,
              onChanged: (_) => context.read<ThemeCubit>().toggleTheme(),
            ),
          ),
          const Divider(),
          const Spacer(),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed: () => context.read<AuthCubit>().logout(),
              icon: const Icon(Icons.logout, color: AppTheme.errorColor),
              label: const Text('Sign Out', style: TextStyle(color: AppTheme.errorColor)),
              style: OutlinedButton.styleFrom(
                side: const BorderSide(color: AppTheme.errorColor),
                padding: const EdgeInsets.symmetric(vertical: 14),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
