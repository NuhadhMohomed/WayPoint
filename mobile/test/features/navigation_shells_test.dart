import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:waypoint_mobile/core/theme/theme_cubit.dart';
import 'package:waypoint_mobile/core/network/api_client.dart';
import 'package:waypoint_mobile/core/storage/secure_storage_service.dart';
import 'package:waypoint_mobile/features/auth/bloc/auth_cubit.dart';
import 'package:waypoint_mobile/features/auth/models/auth_models.dart';
import 'package:waypoint_mobile/features/navigation/screens/passenger_navigation_shell.dart';
import 'package:waypoint_mobile/features/navigation/screens/conductor_navigation_shell.dart';

void main() {
  const testPassenger = UserModel(
    id: 'usr-p1',
    fullName: 'Kasun Passenger',
    email: 'kasun@waypoint.lk',
    role: 'Passenger',
  );

  const testConductor = UserModel(
    id: 'usr-c1',
    fullName: 'Sunil Conductor',
    email: 'conductor@waypoint.lk',
    role: 'Conductor',
  );

  testWidgets('PassengerNavigationShell renders Velora tabs: Explore, Seats, Tickets, Profile', (tester) async {
    await tester.pumpWidget(
      MultiBlocProvider(
        providers: [
          BlocProvider(create: (_) => ThemeCubit()),
          BlocProvider(create: (_) => AuthCubit(apiClient: ApiClient(), storageService: SecureStorageService())),
        ],
        child: const MaterialApp(
          home: PassengerNavigationShell(user: testPassenger),
        ),
      ),
    );

    expect(find.text('Explore'), findsOneWidget);
    expect(find.text('Seats'), findsOneWidget);
    expect(find.text('Tickets'), findsOneWidget);
    expect(find.text('Profile'), findsOneWidget);
  });

  testWidgets('ConductorNavigationShell renders Conductor tabs: Scan QR, Manifest, Staff Ops', (tester) async {
    await tester.pumpWidget(
      MultiBlocProvider(
        providers: [
          BlocProvider(create: (_) => ThemeCubit()),
          BlocProvider(create: (_) => AuthCubit(apiClient: ApiClient(), storageService: SecureStorageService())),
        ],
        child: const MaterialApp(
          home: ConductorNavigationShell(user: testConductor),
        ),
      ),
    );

    expect(find.text('Scan QR'), findsOneWidget);
    expect(find.text('Manifest'), findsOneWidget);
    expect(find.text('Staff Ops'), findsOneWidget);
    expect(find.text('Conductor Terminal'), findsOneWidget);
  });
}
