import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:dio/dio.dart';
import 'package:waypoint_mobile/core/network/api_client.dart';
import 'package:waypoint_mobile/features/journey/services/journey_api_service.dart';

class MockDio extends Mock implements Dio {}

void main() {
  late MockDio mockDio;
  late ApiClient apiClient;
  late JourneyApiService service;

  setUp(() {
    mockDio = MockDio();
    when(() => mockDio.interceptors).thenReturn(Interceptors());
    apiClient = ApiClient(dioClient: mockDio);
    service = JourneyApiService(client: apiClient);
  });

  test('getAiRecommendations parses response and handles fallback flag', () async {
    when(() => mockDio.post(
          '/journeys/ai-recommendation',
          data: any(named: 'data'),
        )).thenAnswer((_) async => Response(
          requestOptions: RequestOptions(path: '/journeys/ai-recommendation'),
          statusCode: 200,
          data: {
            'workflowId': 'e0f7f3a2-71c1-4b1f-9b2f-2d7c588e1a12',
            'status': 'Completed',
            'agentReasoning': 'Evaluated 3 routes. Selected EX-08 for shortest duration.',
            'isAiFallback': false,
            'candidates': [
              {
                'serviceId': 'srv-1',
                'serviceCode': 'SRV-COL-ELLA-0800',
                'routeNumber': 'EX-08',
                'origin': 'Colombo',
                'destination': 'Ella',
                'departureTime': '2026-10-01T08:00:00Z',
                'arrivalTime': '2026-10-01T14:30:00Z',
                'totalFare': 2500.0,
                'durationMinutes': 390,
                'isConnecting': false,
                'matchScore': 0.95,
                'availableSeats': 12,
                'busClass': 'SemiLuxury',
              }
            ],
          },
        ));

    final result = await service.getAiRecommendations(
      objective: 'Fastest bus to Ella with AC',
      passengerCount: 2,
    );

    expect(result.isAiFallback, isFalse);
    expect(result.status, equals('Completed'));
    expect(result.agentReasoning, contains('Selected EX-08'));
    expect(result.candidates.length, equals(1));
    expect(result.candidates.first.matchScore, equals(0.95));
    expect(result.candidates.first.routeNumber, equals('EX-08'));
  });

  test('getAiRecommendations falls back gracefully on DioException', () async {
    when(() => mockDio.post(
          '/journeys/ai-recommendation',
          data: any(named: 'data'),
        )).thenThrow(DioException(
      requestOptions: RequestOptions(path: '/journeys/ai-recommendation'),
      type: DioExceptionType.connectionError,
    ));

    final result = await service.getAiRecommendations(
      objective: 'Bus to Ella',
    );

    expect(result.isAiFallback, isTrue);
    expect(result.status, equals('SafeFailure'));
    expect(result.candidates, isNotEmpty);
  });
}
