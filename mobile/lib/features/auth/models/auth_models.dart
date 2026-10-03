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

  RegisterRequest({
    required this.email,
    required this.password,
    required this.fullName,
    required this.nicOrPassport,
  });

  Map<String, dynamic> toJson() => {
    'email': email,
    'password': password,
    'fullName': fullName,
    'nicOrPassport': nicOrPassport,
    'roleId': 'passenger', // Example role assignment
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
    return AuthResponse(
      token: json['token'],
      refreshToken: json['refreshToken'],
      userId: json['userId'] ?? '',
    );
  }
}
