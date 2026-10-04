import 'dart:io';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import '../storage/local_cache_service.dart';

class ApiClient {
  late final Dio dio;
  final LocalCacheService _cacheService;

  static String get defaultBaseUrl {
    if (kIsWeb) return 'http://localhost:5000/api/v1';
    try {
      if (Platform.isAndroid) return 'http://10.0.2.2:5000/api/v1';
    } catch (_) {}
    return 'http://localhost:5000/api/v1';
  }

  ApiClient({String? baseUrl, LocalCacheService? cacheService})
      : _cacheService = cacheService ?? LocalCacheService() {
    dio = Dio(
      BaseOptions(
        baseUrl: baseUrl ?? defaultBaseUrl,
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 10),
        headers: {'Content-Type': 'application/json'},
      ),
    );

    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await _cacheService.getAuthToken();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          return handler.next(options);
        },
        onError: (DioException error, handler) async {
          if (error.response?.statusCode == 401) {
            await _cacheService.clearAuth();
          }
          return handler.next(error);
        },
      ),
    );
  }

  static String humanizeError(dynamic error) {
    if (error is DioException) {
      if (error.type == DioExceptionType.connectionTimeout ||
          error.type == DioExceptionType.receiveTimeout ||
          error.type == DioExceptionType.sendTimeout) {
        return 'Connection timed out. Please check your internet connection and try again.';
      }
      if (error.type == DioExceptionType.connectionError) {
        return 'Unable to connect to the transit server. Please check your network connection.';
      }
      if (error.response?.statusCode == 401) {
        return 'Your session has expired. Please sign in again to continue.';
      }
      if (error.response?.statusCode == 403) {
        return 'Access denied. You do not have permission to perform this action.';
      }
      if (error.response?.statusCode == 404) {
        return 'The requested transit service or booking was not found.';
      }
      if (error.response?.statusCode == 409) {
        return 'These seats were just reserved by another passenger. Please select alternative seats.';
      }
      if (error.response != null && error.response?.data is Map) {
        final data = error.response!.data as Map;
        if (data.containsKey('detail')) return data['detail'].toString();
        if (data.containsKey('title')) return data['title'].toString();
      }
      return 'Transit service unavailable (${error.response?.statusCode ?? 'offline'}). Please try again shortly.';
    }

    final msg = error.toString().toLowerCase();
    if (msg.contains('socket') || msg.contains('connection refused') || msg.contains('network')) {
      return 'Unable to connect to transit server. Running in offline/demo mode.';
    }
    return 'Something unexpected happened. Please try again.';
  }
}

// Backward-compatible alias
class MobileApiClient extends ApiClient {
  MobileApiClient() : super();
}
