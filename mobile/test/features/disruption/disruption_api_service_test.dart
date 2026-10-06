import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:dio/dio.dart';
import 'package:waypoint_mobile/core/network/api_client.dart';
import 'package:waypoint_mobile/features/disruption/services/disruption_api_service.dart';

class MockDio extends Mock implements Dio {}

void main() {
  late MockDio mockDio;
  late ApiClient apiClient;
  late DisruptionApiService service;

  setUp(() {
    mockDio = MockDio();
    when(() => mockDio.interceptors).thenReturn(Interceptors());
    apiClient = ApiClient(dioClient: mockDio);
    service = DisruptionApiService(client: apiClient);
  });

  test('getAiRebookingProposal parses response and constructs DisruptionAlertModel', () async {
    when(() => mockDio.post(
          '/rebooking/generate-ai-proposal',
          data: any(named: 'data'),
        )).thenAnswer((_) async => Response(
          requestOptions: RequestOptions(path: '/rebooking/generate-ai-proposal'),
          statusCode: 200,
          data: {
            'proposalId': 'prop-123',
            'workflowId': 'wf-456789',
            'status': 'PendingManagerApproval',
            'agentReasoning': 'Found optimal replacement AC coach via Kandy bypass.',
            'isAiFallback': false,
            'candidates': [
              {
                'serviceId': 'srv-rep-1',
                'serviceCode': 'WP-CAD-4120',
                'routeNumber': 'EX-08',
                'originCity': 'Bastion Hill',
                'destinationCity': 'Ella Town',
                'departureTime': '2026-10-10T07:15:00Z',
                'arrivalTime': '2026-10-10T12:20:00Z',
                'totalFare': 4950.0,
                'busClass': 'Super Line Luxury Coach',
                'availableSeatsCount': 18,
              }
            ],
          },
        ));

    final result = await service.getAiRebookingProposal(
      disruptionCaseId: 'dc-001',
    );

    expect(result.proposalId, equals('prop-123'));
    expect(result.disruptionCaseId, equals('dc-001'));
    expect(result.replacementBusPlate, equals('WP-CAD-4120'));
    expect(result.replacementBusClass, equals('Super Line Luxury Coach'));
    expect(result.disruptionReason, contains('Kandy bypass'));
  });

  test('getAiRebookingProposal falls back gracefully to sample on DioException', () async {
    when(() => mockDio.post(
          '/rebooking/generate-ai-proposal',
          data: any(named: 'data'),
        )).thenThrow(DioException(
      requestOptions: RequestOptions(path: '/rebooking/generate-ai-proposal'),
      type: DioExceptionType.connectionError,
    ));

    final result = await service.getAiRebookingProposal(
      disruptionCaseId: 'dc-offline',
    );

    expect(result.bookingReference, equals('WP-8F29K1'));
    expect(result.replacementBusPlate, contains('WP-CAD-4120'));
  });

  test('executeRebooking sends post and returns true on success', () async {
    when(() => mockDio.post('/rebooking/prop-123/execute')).thenAnswer(
      (_) async => Response(
        requestOptions: RequestOptions(path: '/rebooking/prop-123/execute'),
        statusCode: 200,
      ),
    );

    final success = await service.executeRebooking(proposalId: 'prop-123');
    expect(success, isTrue);
  });

  test('executeRebooking returns false on error', () async {
    when(() => mockDio.post('/rebooking/prop-err/execute')).thenThrow(
      DioException(
        requestOptions: RequestOptions(path: '/rebooking/prop-err/execute'),
        type: DioExceptionType.badResponse,
      ),
    );

    final success = await service.executeRebooking(proposalId: 'prop-err');
    expect(success, isFalse);
  });
}
