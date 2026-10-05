import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:dio/dio.dart';
import '../../../core/network/api_client.dart';
import '../../../core/storage/secure_storage_service.dart';
import '../models/auth_models.dart';
import 'auth_state.dart';

class AuthCubit extends Cubit<AuthState> {
  final ApiClient apiClient;
  final SecureStorageService storageService;

  AuthCubit({
    required this.apiClient,
    required this.storageService,
  }) : super(AuthInitial());

  Future<void> checkAuthStatus() async {
    try {
      final token = await storageService.getToken();
      final userJson = await storageService.getUser();

      if (token != null && token.isNotEmpty && userJson != null) {
        final user = UserModel.fromJson(userJson);
        emit(Authenticated(user: user, token: token));
      } else {
        emit(Unauthenticated());
      }
    } catch (_) {
      emit(Unauthenticated());
    }
  }

  Future<void> login(String email, String password) async {
    emit(AuthLoading());
    try {
      final response = await apiClient.post(
        '/auth/login',
        data: {
          'email': email.trim(),
          'password': password,
        },
      );

      final data = response.data;
      if (data == null) {
        emit(const AuthError('Empty response received from authentication server.'));
        return;
      }

      final authResponse = AuthResponse.fromJson(data as Map<String, dynamic>);
      await storageService.saveToken(authResponse.token);
      if (authResponse.refreshToken != null) {
        await storageService.saveRefreshToken(authResponse.refreshToken!);
      }
      await storageService.saveUser(authResponse.user.toJson());

      emit(Authenticated(user: authResponse.user, token: authResponse.token));
    } on DioException catch (e) {
      final message = e.response?.data is Map && e.response?.data['message'] != null
          ? e.response?.data['message']
          : (e.message ?? 'Authentication failed. Please verify credentials.');
      emit(AuthError(message.toString()));
    } catch (e) {
      emit(AuthError('An unexpected error occurred: ${e.toString()}'));
    }
  }

  Future<void> register({
    required String email,
    required String password,
    required String fullName,
    String? phoneNumber,
    String role = 'Passenger',
  }) async {
    emit(AuthLoading());
    try {
      final response = await apiClient.post(
        '/auth/register',
        data: {
          'email': email.trim(),
          'password': password,
          'fullName': fullName.trim(),
          'phoneNumber': phoneNumber?.trim(),
          'role': role,
        },
      );

      final data = response.data;
      if (data == null) {
        emit(const AuthError('Empty response received from registration server.'));
        return;
      }

      final authResponse = AuthResponse.fromJson(data as Map<String, dynamic>);
      await storageService.saveToken(authResponse.token);
      if (authResponse.refreshToken != null) {
        await storageService.saveRefreshToken(authResponse.refreshToken!);
      }
      await storageService.saveUser(authResponse.user.toJson());

      emit(Authenticated(user: authResponse.user, token: authResponse.token));
    } on DioException catch (e) {
      final message = e.response?.data is Map && e.response?.data['message'] != null
          ? e.response?.data['message']
          : (e.message ?? 'Registration failed. Please verify your details.');
      emit(AuthError(message.toString()));
    } catch (e) {
      emit(AuthError('An unexpected error occurred: ${e.toString()}'));
    }
  }

  Future<void> logout() async {
    emit(AuthLoading());
    try {
      await storageService.clearAll();
    } catch (_) {}
    emit(Unauthenticated());
  }
}
