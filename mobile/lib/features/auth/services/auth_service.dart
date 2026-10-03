import 'dart:convert';
import 'package:http/http.dart' as http;
import '../../../core/constants/api_constants.dart';
import '../models/auth_models.dart';

class AuthService {
  final http.Client _client;

  AuthService({http.Client? client}) : _client = client ?? http.Client();

  String get baseUrl => ApiConstants.baseUrl;

  Future<AuthResponse> login(LoginRequest request) async {
    final response = await _client.post(
      Uri.parse('$baseUrl/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(request.toJson()),
    );

    if (response.statusCode == 200) {
      return AuthResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception(_extractErrorMessage(response.body, 'Failed to login'));
    }
  }

  Future<AuthResponse> register(RegisterRequest request) async {
    final response = await _client.post(
      Uri.parse('$baseUrl/auth/register'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(request.toJson()),
    );

    if (response.statusCode == 200 || response.statusCode == 201) {
      return AuthResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception(_extractErrorMessage(response.body, 'Failed to register'));
    }
  }

  String _extractErrorMessage(String responseBody, String defaultMsg) {
    try {
      final decoded = jsonDecode(responseBody);
      if (decoded is Map && decoded.containsKey('detail') && decoded['detail'] != null) {
        return decoded['detail'].toString();
      }
      if (decoded is Map && decoded.containsKey('message') && decoded['message'] != null) {
        return decoded['message'].toString();
      }
    } catch (_) {}
    return defaultMsg;
  }
}
