import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'core/theme/app_theme.dart';
import 'core/theme/theme_cubit.dart';
import 'features/auth/bloc/auth_cubit.dart';
import 'features/navigation/screens/auth_gate.dart';
import 'features/navigation/screens/passenger_navigation_shell.dart';
import 'features/navigation/screens/conductor_navigation_shell.dart';
import 'features/auth/screens/passenger_auth_screen.dart';
import 'features/journey/screens/journey_search_screen.dart';
import 'features/journey/screens/journey_comparison_screen.dart';
import 'features/journey/models/journey_models.dart';
import 'features/booking/screens/seat_picker_screen.dart';
import 'features/booking/screens/payment_checkout_screen.dart';
import 'features/booking/screens/ticket_wallet_screen.dart';
import 'features/booking/screens/booking_history_screen.dart';
import 'features/booking/models/booking_models.dart';
import 'features/disruption/screens/disruption_alert_screen.dart';
import 'features/disruption/models/disruption_models.dart';
import 'features/fleet/data/fleet_api_service.dart';
import 'features/fleet/screens/conductor_scanner_screen.dart';
import 'features/fleet/screens/conductor_manifest_screen.dart';
import 'features/fleet/presentation/screens/review_submission_screen.dart';
import 'features/settings/screens/passenger_settings_screen.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const WayPointApp());
}

class WayPointApp extends StatelessWidget {
  const WayPointApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider<ThemeCubit>(
          create: (_) => ThemeCubit(),
        ),
        BlocProvider<AuthCubit>(
          create: (_) => AuthCubit(),
        ),
      ],
      child: BlocBuilder<ThemeCubit, ThemeMode>(
        builder: (context, themeMode) {
          return MaterialApp(
            title: 'WayPoint Transit',
            debugShowCheckedModeBanner: false,
            theme: AppTheme.lightTheme,
            darkTheme: AppTheme.darkTheme,
            themeMode: themeMode,
            home: const AuthGate(),
            routes: {
              '/passenger': (_) => const PassengerNavigationShell(),
              '/conductor': (_) => const ConductorNavigationShell(),
              '/auth': (_) => const PassengerAuthScreen(),
              '/journey-search': (_) => const JourneySearchScreen(),
              '/journey-compare': (_) => JourneyComparisonScreen(
                    originCity: 'Colombo',
                    destinationCity: 'Ella',
                    travelDate: DateTime.now().add(const Duration(days: 1)),
                    candidates: [
                      JourneyCandidateModel.sampleColomboToEllaDirect(),
                      JourneyCandidateModel.sampleConnectingViaKandy(),
                    ],
                  ),
              '/seat-picker': (_) => SeatPickerScreen(
                    serviceId: '0cff3495-ae17-4dda-9f5f-2d030cd016a3',
                    apiService: FleetApiService(),
                  ),
              '/checkout': (_) => PaymentCheckoutScreen(
                    holdInfo: SeatHoldInfo.sampleColomboToElla(),
                  ),
              '/wallet': (_) => const TicketWalletScreen(),
              '/booking-history': (_) => const BookingHistoryScreen(),
              '/disruption-alert': (_) => DisruptionAlertScreen(
                    disruption: DisruptionAlertModel.sampleColomboToElla(),
                  ),
              '/conductor-scanner': (_) => const ConductorScannerScreen(),
              '/conductor-manifest': (_) => const ConductorManifestScreen(),
              '/review-bus': (_) => const ReviewSubmissionScreen(
                    entityId: 'BUS-101',
                    bookingId: 'WP-BK-COMPLETED-101',
                    entityType: 'bus',
                    entityName: 'Super Line Luxury ND-8821',
                  ),
              '/settings': (_) => const PassengerSettingsScreen(),
            },
          );
        },
      ),
    );
  }
}
