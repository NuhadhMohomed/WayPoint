import 'package:equatable/equatable.dart';

abstract class ReviewState extends Equatable {
  const ReviewState();

  @override
  List<Object?> get props => [];
}

class ReviewInitial extends ReviewState {}

class ReviewSubmitting extends ReviewState {}

class ReviewSubmittedSuccess extends ReviewState {
  final String message;

  const ReviewSubmittedSuccess({required this.message});

  @override
  List<Object?> get props => [message];
}

class ReviewSubmissionError extends ReviewState {
  final String errorMessage;

  const ReviewSubmissionError({required this.errorMessage});

  @override
  List<Object?> get props => [errorMessage];
}
