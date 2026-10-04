import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/theme/theme_cubit.dart';
import '../../../core/widgets/waypoint_card.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../../auth/bloc/auth_cubit.dart';

class PassengerSettingsScreen extends StatelessWidget {
  const PassengerSettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Settings & Preferences'),
        elevation: 0,
      ),
      body: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
        children: [
          // User summary
          BlocBuilder<AuthCubit, AuthState>(
            builder: (context, authState) {
              final email = authState.email ?? 'passenger@waypoint.lk';
              return WayPointCard(
                child: Row(
                  children: [
                    CircleAvatar(
                      radius: 28,
                      backgroundColor: AppTheme.primaryColor.withOpacity(0.2),
                      child: const Icon(
                        Icons.person,
                        size: 32,
                        color: AppTheme.primaryColor,
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            email.split('@').first.toUpperCase(),
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            email,
                            style: TextStyle(
                              fontSize: 13,
                              color: isDark ? Colors.grey[400] : Colors.grey[600],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
          const SizedBox(height: 24),

          // Appearance section
          Text(
            'APPEARANCE',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.2,
              color: isDark ? Colors.grey[400] : Colors.grey[600],
            ),
          ),
          const SizedBox(height: 8),
          WayPointCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Theme Mode',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Choose between dark, light, or system automated themes.',
                  style: TextStyle(
                    fontSize: 12,
                    color: isDark ? Colors.grey[400] : Colors.grey[600],
                  ),
                ),
                const SizedBox(height: 14),
                BlocBuilder<ThemeCubit, ThemeMode>(
                  builder: (context, currentMode) {
                    return SegmentedButton<ThemeMode>(
                      segments: const [
                        ButtonSegment(
                          value: ThemeMode.system,
                          icon: Icon(Icons.brightness_auto, size: 18),
                          label: Text('System'),
                        ),
                        ButtonSegment(
                          value: ThemeMode.light,
                          icon: Icon(Icons.light_mode, size: 18),
                          label: Text('Light'),
                        ),
                        ButtonSegment(
                          value: ThemeMode.dark,
                          icon: Icon(Icons.dark_mode, size: 18),
                          label: Text('Dark'),
                        ),
                      ],
                      selected: {currentMode},
                      onSelectionChanged: (selected) {
                        context.read<ThemeCubit>().setThemeMode(selected.first);
                      },
                      style: ButtonStyle(
                        visualDensity: VisualDensity.compact,
                        backgroundColor: MaterialStateProperty.resolveWith((states) {
                          if (states.contains(MaterialState.selected)) {
                            return AppTheme.primaryColor.withOpacity(0.2);
                          }
                          return Colors.transparent;
                        }),
                        foregroundColor: MaterialStateProperty.resolveWith((states) {
                          if (states.contains(MaterialState.selected)) {
                            return AppTheme.primaryColor;
                          }
                          return isDark ? Colors.white70 : Colors.black87;
                        }),
                      ),
                    );
                  },
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),

          // Preferences section
          Text(
            'TRANSIT PREFERENCES',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.2,
              color: isDark ? Colors.grey[400] : Colors.grey[600],
            ),
          ),
          const SizedBox(height: 8),
          WayPointCard(
            child: Column(
              children: [
                SwitchListTile.adaptive(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Push Disruption Alerts'),
                  subtitle: const Text('Receive instant notifications for delays & route blocks'),
                  value: true,
                  activeColor: AppTheme.primaryColor,
                  onChanged: (val) {},
                ),
                const Divider(),
                SwitchListTile.adaptive(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Haptic Confirmation'),
                  subtitle: const Text('Tactile feedback when holding seats and paying'),
                  value: true,
                  activeColor: AppTheme.primaryColor,
                  onChanged: (val) {},
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),

          // About & Support
          Text(
            'SYSTEM & SUPPORT',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.2,
              color: isDark ? Colors.grey[400] : Colors.grey[600],
            ),
          ),
          const SizedBox(height: 8),
          WayPointCard(
            child: Column(
              children: [
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(Icons.help_outline),
                  title: const Text('Help & Transit Support'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () {},
                ),
                const Divider(),
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(Icons.shield_outlined),
                  title: const Text('Privacy & Passenger Rights'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () {},
                ),
                const Divider(),
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(Icons.info_outline),
                  title: const Text('App Version'),
                  trailing: Text(
                    'v2.4.0 (Build 412)',
                    style: TextStyle(
                      fontSize: 12,
                      color: isDark ? Colors.grey[400] : Colors.grey[600],
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 32),

          // Sign out button
          WayPointButton(
            text: 'Sign Out',
            variant: WayPointButtonVariant.outline,
            icon: Icons.logout,
            onPressed: () {
              context.read<AuthCubit>().logout();
            },
          ),
          const SizedBox(height: 20),
        ],
      ),
    );
  }
}
