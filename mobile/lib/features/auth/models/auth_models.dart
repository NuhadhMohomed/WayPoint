class LoginRequest {
  final String email;
  final String password;

  LoginRequest({required this.email, required this.password});

  Map<String, dynamic> toJson() => {
    'email': email,
    'password': password,
  };
}

class RegisterRequest {
  final String email;
  final String password;
  final String fullName;
  final String nicOrPassport;
  final String? phoneNumber;

  RegisterRequest({
    required this.email,
    required this.password,
    required this.fullName,
    required this.nicOrPassport,
    this.phoneNumber,
  });

  Map<String, dynamic> toJson() => {
    'email': email,
    'password': password,
    'fullName': fullName,
    'nicOrPassport': nicOrPassport,
    'phoneNumber': phoneNumber,
    'role': 'Passenger',
  };
}

class AuthResponse {
  final String token;
  final String refreshToken;
  final String userId;

  AuthResponse({
    required this.token,
    required this.refreshToken,
    required this.userId,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) {
    final user = json['user'] ?? json['User'];
    final userId = user is Map ? (user['id'] ?? user['Id'] ?? '') : (json['userId'] ?? '');
    return AuthResponse(
      token: (json['token'] ?? json['Token'] ?? '').toString(),
      refreshToken: (json['refreshToken'] ?? json['RefreshToken'] ?? '').toString(),
      userId: userId.toString(),
    );
  }
}
