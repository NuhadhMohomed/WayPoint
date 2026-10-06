import 'dart:convert';
import 'package:http/http.dart' as http;
import '../../../core/constants/api_constants.dart';
import '../../../core/storage/secure_storage_service.dart';

/// Service responsible for fetching disruption cases and service alerts.
class DisruptionService {
  final http.Client _client;
  final SecureStorageService _storageService;

  DisruptionService({
    http.Client? client,
    SecureStorageService? storageService,
  })  : _client = client ?? http.Client(),
        _storageService = storageService ?? SecureStorageService();

  /// Fetches all active disruption cases from the backend.
  Future<List<Map<String, dynamic>>> getDisruptions() async {
    final token = await _storageService.getToken();
    final url = Uri.parse('${ApiConstants.baseUrl}/disruptions');

    final response = await _client.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = jsonDecode(response.body);
      return data.cast<Map<String, dynamic>>();
    } else {
      throw Exception('Failed to load disruptions: ${response.statusCode}');
    }
  }

  /// Fetches a specific disruption case by ID.
  Future<Map<String, dynamic>> getDisruptionById(String id) async {
    final token = await _storageService.getToken();
    final url = Uri.parse('${ApiConstants.baseUrl}/disruptions/$id');

    final response = await _client.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else {
      throw Exception('Failed to load disruption details: ${response.statusCode}');
    }
  }
}
