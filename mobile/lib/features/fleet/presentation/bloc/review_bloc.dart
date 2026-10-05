import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:dio/dio.dart';
import '../../data/fleet_api_service.dart';
import 'review_event.dart';
import 'review_state.dart';

class ReviewBloc extends Bloc<ReviewEvent, ReviewState> {
  final FleetApiService apiService;

  ReviewBloc({required this.apiService}) : super(ReviewInitial()) {
    on<SubmitBusReview>(_onSubmitBusReview);
    on<SubmitDriverReview>(_onSubmitDriverReview);
  }

  Future<void> _onSubmitBusReview(SubmitBusReview event, Emitter<ReviewState> emit) async {
    emit(ReviewSubmitting());
    try {
      await apiService.submitBusReview(
        busId: event.busId,
        bookingId: event.bookingId,
        rating: event.rating,
        comment: event.comment,
        isAnonymous: event.isAnonymous,
      );
      emit(const ReviewSubmittedSuccess(message: "Bus review submitted successfully!"));
    } on DioException catch (e) {
      final errorMessage = e.response?.data['detail'] ?? "Failed to submit bus review.";
      emit(ReviewSubmissionError(errorMessage: errorMessage));
    } catch (e) {
      emit(ReviewSubmissionError(errorMessage: e.toString()));
    }
  }

  Future<void> _onSubmitDriverReview(SubmitDriverReview event, Emitter<ReviewState> emit) async {
    emit(ReviewSubmitting());
    try {
      await apiService.submitDriverReview(
        driverId: event.driverId,
        bookingId: event.bookingId,
        rating: event.rating,
        comment: event.comment,
        isAnonymous: event.isAnonymous,
      );
      emit(const ReviewSubmittedSuccess(message: "Driver review submitted successfully!"));
    } on DioException catch (e) {
      final errorMessage = e.response?.data['detail'] ?? "Failed to submit driver review.";
      emit(ReviewSubmissionError(errorMessage: errorMessage));
    } catch (e) {
      emit(ReviewSubmissionError(errorMessage: e.toString()));
    }
  }
}
