import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'core/theme/app_theme.dart';
import 'core/theme/theme_cubit.dart';
import 'core/network/api_client.dart';
import 'core/storage/secure_storage_service.dart';
import 'features/auth/bloc/auth_cubit.dart';
import 'features/navigation/screens/auth_gate.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  final storageService = SecureStorageService();
  final apiClient = ApiClient(storage: storageService);

  runApp(WayPointApp(
    storageService: storageService,
    apiClient: apiClient,
  ));
}

class WayPointApp extends StatelessWidget {
  final SecureStorageService storageService;
  final ApiClient apiClient;

  const WayPointApp({
    super.key,
    required this.storageService,
    required this.apiClient,
  });

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider<ThemeCubit>(
          create: (_) => ThemeCubit(),
        ),
        BlocProvider<AuthCubit>(
          create: (_) => AuthCubit(
            apiClient: apiClient,
            storageService: storageService,
          ),
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
          );
        },
      ),
    );
  }
}
