import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/theme/theme_cubit.dart';
import '../../../core/storage/local_cache_service.dart';
import '../../../core/widgets/waypoint_card.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../../auth/bloc/auth_cubit.dart';

class PassengerSettingsScreen extends StatefulWidget {
  const PassengerSettingsScreen({super.key});

  @override
  State<PassengerSettingsScreen> createState() => _PassengerSettingsScreenState();
}

class _PassengerSettingsScreenState extends State<PassengerSettingsScreen> {
  final _cache = LocalCacheService();
  List<Map<String, dynamic>> _savedTravelers = [];
  bool _pushNotifications = true;
  bool _hapticFeedback = true;

  @override
  void initState() {
    super.initState();
    _loadTravelers();
  }

  Future<void> _loadTravelers() async {
    final travelers = await _cache.getSavedTravelers();
    if (travelers.isEmpty) {
      final samples = [
        {'name': 'Nimal Silva', 'nic': '198829104821', 'type': 'Primary Passenger'},
        {'name': 'Amara Silva', 'nic': '199074829102', 'type': 'Companion'},
      ];
      await _cache.saveTravelers(samples);
      if (mounted) setState(() => _savedTravelers = samples);
    } else {
      if (mounted) setState(() => _savedTravelers = travelers);
    }
  }

  void _showAddTravelerModal() {
    final nameCtrl = TextEditingController();
    final nicCtrl = TextEditingController();

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => Padding(
        padding: EdgeInsets.only(bottom: MediaQuery.of(ctx).viewInsets.bottom),
        child: Container(
          decoration: BoxDecoration(
            color: Theme.of(context).brightness == Brightness.dark
                ? const Color(0xFF1E293B)
                : Colors.white,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
          ),
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Add Saved Traveler', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              const SizedBox(height: 16),
              TextField(
                controller: nameCtrl,
                decoration: const InputDecoration(labelText: 'Full Name', border: OutlineInputBorder()),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: nicCtrl,
                decoration: const InputDecoration(labelText: 'NIC or Passport No', border: OutlineInputBorder()),
              ),
              const SizedBox(height: 20),
              WayPointButton(
                text: 'Save Traveler',
                onPressed: () async {
                  if (nameCtrl.text.isNotEmpty) {
                    final updated = List<Map<String, dynamic>>.from(_savedTravelers)
                      ..add({'name': nameCtrl.text.trim(), 'nic': nicCtrl.text.trim(), 'type': 'Companion'});
                    await _cache.saveTravelers(updated);
                    if (mounted) setState(() => _savedTravelers = updated);
                    if (ctx.mounted) Navigator.of(ctx).pop();
                  }
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final textMuted = isDark ? Colors.grey[400] : Colors.grey[600];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Settings & Preferences'),
        elevation: 0,
      ),
      body: Material(
        color: Colors.transparent,
        child: ListView(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
          children: [
            // User Profile
            BlocBuilder<AuthCubit, AuthState>(
              builder: (context, authState) {
                final email = authState.email ?? 'passenger@waypoint.lk';
                final name = authState.fullName ?? email.split('@').first.toUpperCase();
                return WayPointCard(
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 28,
                        backgroundColor: AppTheme.primaryColor.withOpacity(0.2),
                        child: const Icon(Icons.person, size: 32, color: AppTheme.primaryColor),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(name, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                            const SizedBox(height: 4),
                            Text(email, style: TextStyle(fontSize: 13, color: textMuted)),
                          ],
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
            const SizedBox(height: 24),

            // Appearance Section
            Text(
              'Appearance',
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w700,
                color: textMuted,
              ),
            ),
            const SizedBox(height: 8),
            WayPointCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Theme Mode', style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600)),
                  const SizedBox(height: 4),
                  Text('Choose your preferred visual theme across the app.', style: TextStyle(fontSize: 12, color: textMuted)),
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
                          HapticFeedback.selectionClick();
                          context.read<ThemeCubit>().setThemeMode(selected.first);
                        },
                      );
                    },
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // Saved Travelers Directory
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Saved Travelers',
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: textMuted,
                  ),
                ),
                TextButton.icon(
                  icon: const Icon(Icons.add, size: 16),
                  label: const Text('Add'),
                  onPressed: _showAddTravelerModal,
                ),
              ],
            ),
            const SizedBox(height: 4),
            WayPointCard(
              child: Column(
                children: _savedTravelers.map((t) {
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 6),
                    child: Row(
                      children: [
                        const Icon(Icons.badge_outlined, size: 20, color: AppTheme.primaryColor),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(t['name']?.toString() ?? '', style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14)),
                              Text('NIC: ${t['nic'] ?? 'N/A'} • ${t['type'] ?? 'Companion'}', style: TextStyle(fontSize: 12, color: textMuted)),
                            ],
                          ),
                        ),
                      ],
                    ),
                  );
                }).toList(),
              ),
            ),
            const SizedBox(height: 24),

            // Transit Helpline & Preferences
            Text(
              'Transit Preferences & Helpline',
              style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: textMuted),
            ),
            const SizedBox(height: 8),
            WayPointCard(
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Push Disruption Alerts', style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
                            Text('Live notifications for transit delays & route diversions', style: TextStyle(fontSize: 12, color: textMuted)),
                          ],
                        ),
                      ),
                      Switch.adaptive(
                        value: _pushNotifications,
                        activeColor: AppTheme.primaryColor,
                        onChanged: (val) => setState(() => _pushNotifications = val),
                      ),
                    ],
                  ),
                  const Divider(height: 20),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Haptic Confirmation', style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
                            Text('Tactile feedback on booking and payment', style: TextStyle(fontSize: 12, color: textMuted)),
                          ],
                        ),
                      ),
                      Switch.adaptive(
                        value: _hapticFeedback,
                        activeColor: AppTheme.primaryColor,
                        onChanged: (val) => setState(() => _hapticFeedback = val),
                      ),
                    ],
                  ),
                  const Divider(height: 20),
                  InkWell(
                    onTap: () {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Dialing 1955 National Transit Helpline...')),
                      );
                    },
                    child: Padding(
                      padding: const EdgeInsets.symmetric(vertical: 4),
                      child: Row(
                        children: [
                          const Icon(Icons.phone_in_talk, color: AppTheme.primaryColor),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text('National Transit Helpline: 1955', style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
                                Text('24/7 Government transit assistance for Sri Lanka', style: TextStyle(fontSize: 12, color: textMuted)),
                              ],
                            ),
                          ),
                          const Icon(Icons.call_made, size: 18),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 32),

            // Sign out
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
      ),
    );
  }
}
