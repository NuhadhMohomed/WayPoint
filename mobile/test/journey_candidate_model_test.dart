import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/journey/models/journey_models.dart';

void main() {
  test('fromJson parses totalDurationMinutes and availableSeatsCount from backend response', () {
    final json = {
      'serviceId': 'srv-001',
      'serviceCode': 'SRV-001',
      'routeNumber': 'EX-08',
      'originCity': 'Colombo',
      'destinationCity': 'Ella',
      'departureTime': '2026-10-10T06:30:00Z',
      'arrivalTime': '2026-10-10T12:30:00Z',
      'totalFare': 2400.0,
      'totalDurationMinutes': 360,
      'isConnecting': false,
      'matchScore': 0.96,
      'availableSeatsCount': 14,
      'busClass': 'Super Line Luxury AC',
    };

    final model = JourneyCandidateModel.fromJson(json);

    expect(model.durationMinutes, 360);
    expect(model.availableSeats, 14);
  });

  test('fromJson still parses durationMinutes from Python AI response', () {
    final json = {
      'serviceId': 'srv-002',
      'durationMinutes': 240,
      'availableSeats': 20,
      'origin': 'Colombo',
      'destination': 'Kandy',
      'departureTime': '2026-10-10T07:00:00Z',
      'arrivalTime': '2026-10-10T11:00:00Z',
      'totalFare': 1800.0,
    };

    final model = JourneyCandidateModel.fromJson(json);

    expect(model.durationMinutes, 240);
    expect(model.availableSeats, 20);
  });
}
