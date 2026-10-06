import 'package:equatable/equatable.dart';

class UserModel extends Equatable {
  final String id;
  final String fullName;
  final String email;
  final String role;
  final String? phoneNumber;

  const UserModel({
    required this.id,
    required this.fullName,
    required this.email,
    required this.role,
    this.phoneNumber,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id']?.toString() ?? '',
      fullName: json['fullName'] ?? json['name'] ?? '',
      email: json['email'] ?? '',
      role: json['role'] ?? 'Passenger',
      phoneNumber: json['phoneNumber'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'fullName': fullName,
      'email': email,
      'role': role,
      'phoneNumber': phoneNumber,
    };
  }

  @override
  List<Object?> get props => [id, fullName, email, role, phoneNumber];
}

class AuthResponse extends Equatable {
  final String token;
  final String? refreshToken;
  final UserModel user;

  const AuthResponse({
    required this.token,
    this.refreshToken,
    required this.user,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) {
    return AuthResponse(
      token: json['token'] ?? '',
      refreshToken: json['refreshToken'],
      user: UserModel.fromJson(json['user'] ?? json),
    );
  }

  @override
  List<Object?> get props => [token, refreshToken, user];
}
