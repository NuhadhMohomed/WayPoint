import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/storage/local_cache_service.dart';

class AuthState {
  final bool isAuthenticated;
  final String? token;
  final String? email;
  final String? fullName;
  final String userRole; // 'Passenger' or 'Conductor' or 'Operator'

  const AuthState({
    this.isAuthenticated = false,
    this.token,
    this.email,
    this.fullName,
    this.userRole = 'Passenger',
  });

  bool get isConductor => userRole.toLowerCase() == 'conductor' || userRole.toLowerCase() == 'operator';

  AuthState copyWith({
    bool? isAuthenticated,
    String? token,
    String? email,
    String? fullName,
    String? userRole,
  }) {
    return AuthState(
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
      token: token ?? this.token,
      email: email ?? this.email,
      fullName: fullName ?? this.fullName,
      userRole: userRole ?? this.userRole,
    );
  }
}

class AuthCubit extends Cubit<AuthState> {
  final LocalCacheService _cache;

  AuthCubit({LocalCacheService? cache})
      : _cache = cache ?? LocalCacheService(),
        super(const AuthState()) {
    checkAuthStatus();
  }

  Future<void> checkAuthStatus() async {
    final token = await _cache.getAuthToken();
    final profile = await _cache.getUserData();
    if (token != null && token.isNotEmpty) {
      emit(AuthState(
        isAuthenticated: true,
        token: token,
        email: profile?['email']?.toString(),
        fullName: profile?['fullName']?.toString() ?? profile?['name']?.toString(),
        userRole: profile?['role']?.toString() ?? 'Passenger',
      ));
    } else {
      emit(const AuthState(isAuthenticated: false));
    }
  }

  void emitAuthenticated({
    required String token,
    required String email,
    required String role,
    String? fullName,
  }) {
    _cache.saveAuthToken(token);
    _cache.saveUserData({
      'email': email,
      'role': role,
      'fullName': fullName ?? 'Travel Passenger',
    });
    emit(AuthState(
      isAuthenticated: true,
      token: token,
      email: email,
      fullName: fullName ?? 'Travel Passenger',
      userRole: role,
    ));
  }

  Future<void> logout() async {
    await _cache.clearAuth();
    emit(const AuthState(isAuthenticated: false));
  }
}
