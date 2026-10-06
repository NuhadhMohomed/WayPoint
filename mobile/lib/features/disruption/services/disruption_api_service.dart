import 'package:flutter/foundation.dart';
import '../../../core/network/api_client.dart';
import '../models/disruption_models.dart';

/// API Service for disruption recovery and AI rebooking (FR-AI-001, UC-03, BR-REBOOK-001).
class DisruptionApiService {
  final MobileApiClient _client;

  DisruptionApiService({MobileApiClient? client})
      : _client = client ?? MobileApiClient();

  /// Requests an AI-generated rebooking proposal from the backend.
  /// Calls POST /api/v1/rebooking/generate-ai-proposal (FR-AI-001).
  /// Falls back to sample data gracefully on error (BR-AIVAL-002 Safe Failure).
  Future<DisruptionAlertModel> getAiRebookingProposal({
    required String disruptionCaseId,
    String? objective,
  }) async {
    try {
      final response = await _client.dio.post(
        '/rebooking/generate-ai-proposal',
        data: {
          'disruptionCaseId': disruptionCaseId,
          if (objective != null) 'objective': objective,
        },
      );

      if (response.statusCode == 200 && response.data != null) {
        final data = response.data as Map<String, dynamic>;
        final candidates = (data['candidates'] as List<dynamic>?) ?? [];
        if (candidates.isNotEmpty) {
          return DisruptionAlertModel.fromAiProposal(
            json: data,
            disruptionCaseId: disruptionCaseId,
          );
        }
      }
    } catch (e) {
      debugPrint('[DisruptionApiService] AI proposal request failed: $e');
    }

    // Safe Failure fallback (BR-AIVAL-002)
    return DisruptionAlertModel.sampleColomboToElla();
  }

  /// Transactionally executes an approved rebooking proposal (BR-REBOOK-001).
  Future<bool> executeRebooking({required String proposalId}) async {
    try {
      final response = await _client.dio.post('/rebooking/$proposalId/execute');
      return response.statusCode == 200;
    } catch (e) {
      debugPrint('[DisruptionApiService] Rebooking execution failed: $e');
      return false;
    }
  }
}
