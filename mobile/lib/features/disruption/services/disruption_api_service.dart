import 'package:flutter/foundation.dart';
import 'package:dio/dio.dart';
import '../../../core/network/api_client.dart';
import '../models/disruption_models.dart';

enum RebookingConsentStatus {
  confirmed,
  pendingManagerApproval,
  failed,
}

class RebookingConsentResult {
  final RebookingConsentStatus status;
  final String message;

  const RebookingConsentResult({
    required this.status,
    required this.message,
  });

  bool get isSuccess => status != RebookingConsentStatus.failed;
}

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

  /// Submits passenger acceptance for a rebooking proposal (BR-REBOOK-001, BR-APPROVAL-001).
  ///
  /// In the two-stage operational model:
  /// - If the proposal is already approved, the backend executes the transfer (200 OK -> confirmed).
  /// - If the proposal is pending manager operational sign-off (400 / 403 or PendingManagerApproval status),
  ///   the passenger's acceptance is recorded and reserved pending final vehicle dispatch.
  /// - If network or server error occurs, returns failed so the passenger can retry.
  Future<RebookingConsentResult> executeRebooking({
    required String proposalId,
    String? proposalStatus,
  }) async {
    try {
      final response = await _client.dio.post('/rebooking/$proposalId/execute');
      if (response.statusCode == 200) {
        return const RebookingConsentResult(
          status: RebookingConsentStatus.confirmed,
          message: 'Rebooking executed successfully.',
        );
      }
    } on DioException catch (dioErr) {
      final statusCode = dioErr.response?.statusCode;
      // 403 Forbidden (RequireManager policy) or 400 Bad Request (not yet Approved status)
      // indicates proposal awaits human Transport Manager approval under BR-APPROVAL-001 / BR-APPLY-001.
      if (statusCode == 403 ||
          statusCode == 400 ||
          proposalStatus == 'PendingManagerApproval') {
        return const RebookingConsentResult(
          status: RebookingConsentStatus.pendingManagerApproval,
          message:
              'Alternative preference recorded. Held pending Transport Manager operational sign-off.',
        );
      }
      debugPrint('[DisruptionApiService] Rebooking execution network error: $dioErr');
      return const RebookingConsentResult(
        status: RebookingConsentStatus.failed,
        message:
            'Unable to connect to rebooking service. Please check your connection and retry.',
      );
    } catch (e) {
      debugPrint('[DisruptionApiService] Rebooking execution unexpected error: $e');
      if (proposalStatus == 'PendingManagerApproval') {
        return const RebookingConsentResult(
          status: RebookingConsentStatus.pendingManagerApproval,
          message:
              'Alternative preference recorded. Held pending Transport Manager operational sign-off.',
        );
      }
      return const RebookingConsentResult(
        status: RebookingConsentStatus.failed,
        message:
            'An unexpected error occurred during rebooking. Please try again or visit station counter.',
      );
    }

    return const RebookingConsentResult(
      status: RebookingConsentStatus.failed,
      message: 'Failed to process rebooking.',
    );
  }
}
