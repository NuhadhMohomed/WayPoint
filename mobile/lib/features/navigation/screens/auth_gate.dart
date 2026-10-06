import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/storage/secure_storage_service.dart';
import '../../auth/bloc/auth_cubit.dart';
import '../../auth/bloc/auth_state.dart';
import '../../auth/screens/passenger_auth_screen.dart';
import '../../onboarding/screens/onboarding_screen.dart';
import 'branded_splash_screen.dart';
import 'passenger_navigation_shell.dart';
import 'conductor_navigation_shell.dart';

class AuthGate extends StatefulWidget {
  final SecureStorageService? storageService;

  const AuthGate({super.key, this.storageService});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  late final SecureStorageService _storage;
  bool _isCheckingOnboarding = true;
  bool _hasCompletedOnboarding = false;
  bool _isGuestMode = false;

  @override
  void initState() {
    super.initState();
    _storage = widget.storageService ?? SecureStorageService();
    _checkInitialState();
  }

  Future<void> _checkInitialState() async {
    final completed = await _storage.isOnboardingCompleted();
    if (mounted) {
      setState(() {
        _hasCompletedOnboarding = completed;
        _isCheckingOnboarding = false;
      });
      // Trigger cold-start auth check
      context.read<AuthCubit>().checkAuthStatus();
    }
  }

  Future<void> _onFinishOnboarding() async {
    await _storage.setOnboardingCompleted(completed: true);
    if (mounted) {
      setState(() {
        _hasCompletedOnboarding = true;
        _isGuestMode = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isCheckingOnboarding) {
      return const BrandedSplashScreen();
    }

    if (!_hasCompletedOnboarding) {
      return OnboardingScreen(onFinish: _onFinishOnboarding);
    }

    return BlocBuilder<AuthCubit, AuthState>(
      builder: (context, state) {
        if (state is AuthInitial || state is AuthLoading) {
          return const BrandedSplashScreen();
        }

        if (state is Authenticated) {
          final role = state.user.role.toLowerCase();
          if (role == 'conductor' || role == 'transitmanager' || role == 'driver') {
            return ConductorNavigationShell(user: state.user);
          }
          return PassengerNavigationShell(user: state.user);
        }

        // If user explicitly chose to explore as guest
        if (_isGuestMode) {
          return const PassengerNavigationShell(user: null);
        }

        // Prompt unauthenticated user to Sign In or Register right after onboarding
        return PassengerAuthScreen(
          onContinueAsGuest: () {
            setState(() {
              _isGuestMode = true;
            });
          },
        );
      },
    );
  }
}
