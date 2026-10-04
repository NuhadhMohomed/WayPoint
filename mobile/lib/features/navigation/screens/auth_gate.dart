import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/storage/local_cache_service.dart';
import '../../auth/bloc/auth_cubit.dart';
import '../../auth/screens/passenger_auth_screen.dart';
import '../../onboarding/screens/onboarding_screen.dart';

class AuthGate extends StatefulWidget {
  final Widget? passengerHome;
  final Widget? conductorHome;

  const AuthGate({super.key, this.passengerHome, this.conductorHome});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  final LocalCacheService _cache = LocalCacheService();
  bool _isLoading = true;
  bool _firstRunCompleted = false;

  @override
  void initState() {
    super.initState();
    _checkFirstRun();
  }

  Future<void> _checkFirstRun() async {
    final completed = await _cache.isFirstRunCompleted();
    if (mounted) {
      setState(() {
        _firstRunCompleted = completed;
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(
        body: Center(
          child: CircularProgressIndicator(),
        ),
      );
    }

    if (!_firstRunCompleted) {
      return OnboardingScreen(
        onFinish: () {
          setState(() {
            _firstRunCompleted = true;
          });
        },
      );
    }

    return BlocBuilder<AuthCubit, AuthState>(
      builder: (context, state) {
        if (!state.isAuthenticated) {
          return PassengerAuthScreen(
            onAuthSuccess: () {
              context.read<AuthCubit>().checkAuthStatus();
            },
          );
        }

        if (state.isConductor) {
          return widget.conductorHome ?? const Scaffold(body: Center(child: Text('Conductor Portal')));
        }

        return widget.passengerHome ?? const Scaffold(body: Center(child: Text('Passenger Portal')));
      },
    );
  }
}
