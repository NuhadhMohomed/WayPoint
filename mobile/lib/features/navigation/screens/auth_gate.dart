import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/theme/app_theme.dart';
import '../../auth/bloc/auth_cubit.dart';
import '../../auth/bloc/auth_state.dart';
import '../../auth/screens/passenger_auth_screen.dart';
import 'passenger_navigation_shell.dart';
import 'conductor_navigation_shell.dart';

class AuthGate extends StatelessWidget {
  const AuthGate({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<AuthCubit, AuthState>(
      builder: (context, state) {
        if (state is AuthInitial) {
          // Trigger auth check on cold start
          context.read<AuthCubit>().checkAuthStatus();
          return const Scaffold(
            body: Center(
              child: CircularProgressIndicator(color: AppTheme.primaryColor),
            ),
          );
        }

        if (state is AuthLoading) {
          return const Scaffold(
            body: Center(
              child: CircularProgressIndicator(color: AppTheme.primaryColor),
            ),
          );
        }

        if (state is Authenticated) {
          final role = state.user.role.toLowerCase();
          if (role == 'conductor' || role == 'transitmanager' || role == 'driver') {
            return ConductorNavigationShell(user: state.user);
          }
          return PassengerNavigationShell(user: state.user);
        }

        return const PassengerAuthScreen();
      },
    );
  }
}
