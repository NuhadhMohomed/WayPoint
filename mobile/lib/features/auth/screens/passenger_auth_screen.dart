import 'package:flutter/material.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/waypoint_logo.dart';
import '../widgets/login_form.dart';
import '../widgets/register_form.dart';

class PassengerAuthScreen extends StatelessWidget {
  final VoidCallback? onAuthSuccess;

  const PassengerAuthScreen({super.key, this.onAuthSuccess});

  void _handleSuccess(BuildContext context) {
    if (onAuthSuccess != null) {
      onAuthSuccess!();
    } else {
      Navigator.of(context).pushReplacementNamed('/');
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return DefaultTabController(
      length: 2,
      child: Scaffold(
        backgroundColor: isDark ? AppTheme.darkBackground : AppTheme.lightBackground,
        body: SafeArea(
          child: Column(
            children: [
              const SizedBox(height: 24),
              // Brand Logo & Subtitle
              Center(
                child: WayPointLogo(
                  size: 48,
                  showText: true,
                  isDark: isDark,
                ),
              ),
              const SizedBox(height: 24),

              // Segmented Tabs
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24.0),
                child: Container(
                  height: 48,
                  decoration: BoxDecoration(
                    color: isDark ? AppTheme.darkCardBackground : AppTheme.lightSurfaceSubdued,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(
                      color: isDark ? AppTheme.darkBorderColor : AppTheme.lightBorderColor,
                    ),
                  ),
                  child: TabBar(
                    indicator: BoxDecoration(
                      color: AppTheme.primaryColor,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    indicatorSize: TabBarIndicatorSize.tab,
                    labelColor: AppTheme.onPrimaryColor,
                    unselectedLabelColor: isDark ? AppTheme.textMutedDark : AppTheme.textMutedLight,
                    labelStyle: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                    dividerColor: Colors.transparent,
                    tabs: const [
                      Tab(text: 'Sign In'),
                      Tab(text: 'Create Account'),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Tab Views
              Expanded(
                child: TabBarView(
                  children: [
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 24.0),
                      child: SingleChildScrollView(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const SizedBox(height: 16),
                            LoginForm(
                              onSuccess: () => _handleSuccess(context),
                            ),
                          ],
                        ),
                      ),
                    ),
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 24.0),
                      child: SingleChildScrollView(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const SizedBox(height: 16),
                            RegisterForm(
                              onSuccess: () => _handleSuccess(context),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
