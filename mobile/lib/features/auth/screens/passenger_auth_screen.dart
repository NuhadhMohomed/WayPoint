import 'package:flutter/material.dart';
import '../widgets/login_form.dart';
import '../widgets/register_form.dart';
import '../../../core/theme/app_theme.dart';

class PassengerAuthScreen extends StatelessWidget {
  const PassengerAuthScreen({super.key});

  void _handleAuthSuccess(BuildContext context) {
    // Navigate to the main application area
    Navigator.of(context).pushReplacementNamed('/home');
  }

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Welcome to WayPoint'),
          bottom: const TabBar(
            tabs: [
              Tab(text: 'Login'),
              Tab(text: 'Register'),
            ],
            labelColor: AppTheme.primaryColor,
            unselectedLabelColor: Colors.grey,
            indicatorColor: AppTheme.primaryColor,
          ),
        ),
        body: SafeArea(
          child: TabBarView(
            children: [
              Padding(
                padding: const EdgeInsets.all(24.0),
                child: SingleChildScrollView(
                  child: Column(
                    children: [
                      const SizedBox(height: 32),
                      const Text(
                        'Sign In',
                        style: TextStyle(
                          fontSize: 24,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 32),
                      LoginForm(
                        onSuccess: () => _handleAuthSuccess(context),
                      ),
                    ],
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(24.0),
                child: SingleChildScrollView(
                  child: Column(
                    children: [
                      const SizedBox(height: 32),
                      const Text(
                        'Create Account',
                        style: TextStyle(
                          fontSize: 24,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 32),
                      RegisterForm(
                        onSuccess: () => _handleAuthSuccess(context),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
