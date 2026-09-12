import 'package:dio/dio.dart';

/// Shared HTTP client for all fleet-related API calls.
/// Connects to the WayPoint ASP.NET Core backend.
class FleetApiService {
  final Dio _dio;

  FleetApiService({Dio? dio})
      : _dio = dio ??
            Dio(BaseOptions(
              baseUrl: 'http://10.0.2.2:5010', // Android emulator → host
              connectTimeout: const Duration(seconds: 10),
              receiveTimeout: const Duration(seconds: 10),
            ));

  // ─── Seat Availability (Component 2 core) ───

  Future<Map<String, dynamic>> getServiceSeats(String serviceId) async {
    final response = await _dio.get('/api/v1/services/$serviceId/seats');
    return response.data as Map<String, dynamic>;
  }

  // ─── Seat Hold ───

  Future<Map<String, dynamic>> holdSeats({
    required String serviceId,
    required List<String> seatNumbers,
    required String passengerId,
  }) async {
    final response = await _dio.post('/api/v1/services/$serviceId/seats/hold', data: {
      'seatNumbers': seatNumbers,
      'passengerId': passengerId,
    });
    return response.data as Map<String, dynamic>;
  }

  // ─── Payment (Sandbox) ───

  Future<Map<String, dynamic>> confirmSandboxCharge({
    required String bookingId,
    required String cardNumber,
    required String expiry,
    required String cvv,
  }) async {
    final response = await _dio.post(
      '/api/v1/payments/confirm-sandbox-charge/$bookingId',
      data: {'cardNumber': cardNumber, 'expiry': expiry, 'cvv': cvv},
    );
    return response.data as Map<String, dynamic>;
  }

  // ─── Tickets ───

  Future<List<Map<String, dynamic>>> getTickets(String passengerId) async {
    final response = await _dio.get('/api/v1/tickets', queryParameters: {'passengerId': passengerId});
    return (response.data as List).cast<Map<String, dynamic>>();
  }

  Future<Map<String, dynamic>> verifyTicket(String qrPayload) async {
    final response = await _dio.post('/api/v1/tickets/verify', data: {'payload': qrPayload});
    return response.data as Map<String, dynamic>;
  }

  // ─── Bookings ───

  Future<List<Map<String, dynamic>>> getBookingHistory(String passengerId) async {
    final response =
        await _dio.get('/api/v1/bookings', queryParameters: {'passengerId': passengerId});
    return (response.data as List).cast<Map<String, dynamic>>();
  }

  Future<Map<String, dynamic>> cancelBooking(String bookingId) async {
    final response = await _dio.post('/api/v1/bookings/$bookingId/cancel');
    return response.data as Map<String, dynamic>;
  }

  // ─── Conductor Manifest ───

  Future<Map<String, dynamic>> getManifest(String serviceId) async {
    final response = await _dio.get('/api/v1/services/$serviceId/manifest');
    return response.data as Map<String, dynamic>;
  }

  // ─── Reviews ───

  Future<Map<String, dynamic>> submitBusReview({
    required String busId,
    required String bookingId,
    required int rating,
    String? comment,
    bool isAnonymous = false,
  }) async {
    final response = await _dio.post('/api/v1/reviews/buses', data: {
      'busId': busId,
      'bookingId': bookingId,
      'rating': rating,
      'comment': comment,
      'isAnonymous': isAnonymous,
    });
    return response.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> submitDriverReview({
    required String driverId,
    required String bookingId,
    required int rating,
    String? comment,
    bool isAnonymous = false,
  }) async {
    final response = await _dio.post('/api/v1/reviews/drivers', data: {
      'driverId': driverId,
      'bookingId': bookingId,
      'rating': rating,
      'comment': comment,
      'isAnonymous': isAnonymous,
    });
    return response.data as Map<String, dynamic>;
  }
}
