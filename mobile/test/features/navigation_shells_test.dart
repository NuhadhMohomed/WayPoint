import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:waypoint_mobile/core/theme/theme_cubit.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_cubit.dart';
import 'package:waypoint_mobile/features/navigation/screens/passenger_navigation_shell.dart';
import 'package:waypoint_mobile/features/navigation/screens/conductor_navigation_shell.dart';

void main() {
  testWidgets('PassengerNavigationShell renders 4 tabs: Explore, My Trips, Alerts, Settings', (tester) async {
    await tester.pumpWidget(
      MultiBlocProvider(
        providers: [
          BlocProvider(create: (_) => ThemeCubit()),
          BlocProvider(create: (_) => AuthCubit()),
        ],
        child: const MaterialApp(
          home: PassengerNavigationShell(),
        ),
      ),
    );
    expect(find.text('Explore'), findsOneWidget);
    expect(find.text('My Trips'), findsOneWidget);
    expect(find.text('Alerts'), findsOneWidget);
    expect(find.text('Settings'), findsOneWidget);
  });

  testWidgets('ConductorNavigationShell renders 4 tabs: Services, Scanner, Manifest, Profile', (tester) async {
    await tester.pumpWidget(
      MultiBlocProvider(
        providers: [
          BlocProvider(create: (_) => ThemeCubit()),
          BlocProvider(create: (_) => AuthCubit()),
        ],
        child: const MaterialApp(
          home: ConductorNavigationShell(),
        ),
      ),
    );
    expect(find.text('Services'), findsOneWidget);
    expect(find.text('Scanner'), findsOneWidget);
    expect(find.text('Manifest'), findsOneWidget);
    expect(find.text('Profile'), findsOneWidget);
  });
}
