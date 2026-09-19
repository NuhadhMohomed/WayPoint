import 'package:equatable/equatable.dart';

abstract class ReviewEvent extends Equatable {
  const ReviewEvent();

  @override
  List<Object?> get props => [];
}

class SubmitBusReview extends ReviewEvent {
  final String busId;
  final String bookingId;
  final int rating;
  final String? comment;
  final bool isAnonymous;

  const SubmitBusReview({
    required this.busId,
    required this.bookingId,
    required this.rating,
    this.comment,
    this.isAnonymous = false,
  });

  @override
  List<Object?> get props => [busId, bookingId, rating, comment, isAnonymous];
}

class SubmitDriverReview extends ReviewEvent {
  final String driverId;
  final String bookingId;
  final int rating;
  final String? comment;
  final bool isAnonymous;

  const SubmitDriverReview({
    required this.driverId,
    required this.bookingId,
    required this.rating,
    this.comment,
    this.isAnonymous = false,
  });

  @override
  List<Object?> get props => [driverId, bookingId, rating, comment, isAnonymous];
}
