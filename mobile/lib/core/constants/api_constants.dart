class ApiConstants {
  // Use 10.0.2.2 for Android emulator pointing to host localhost, or localhost for iOS/desktop
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5010/api/v1',
  );

  // Authentication endpoints
  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String currentUser = '/auth/me';

  // Feature endpoints
  static const String searchJourneys = '/journeys/search';
  static const String serviceSeats = '/services';
  static const String holdSeat = '/bookings/hold';
  static const String confirmPayment = '/payments/sandbox-charge';
  static const String bookings = '/bookings';
  static const String disruptionAlerts = '/alerts';
}
