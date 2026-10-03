import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/auth_models.dart';

class AuthService {
  final String baseUrl = 'http://10.0.2.2:5000/api/v1'; // Assuming Android emulator localhost

  Future<AuthResponse> login(LoginRequest request) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(request.toJson()),
    );

    if (response.statusCode == 200) {
      return AuthResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to login: ${response.body}');
    }
  }

  Future<AuthResponse> register(RegisterRequest request) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/register'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(request.toJson()),
    );

    if (response.statusCode == 200) {
      return AuthResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to register: ${response.body}');
    }
  }
}
