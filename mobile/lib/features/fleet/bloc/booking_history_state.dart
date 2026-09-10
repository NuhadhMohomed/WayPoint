part of 'booking_history_bloc.dart';

abstract class BookingHistoryState extends Equatable {
  const BookingHistoryState();

  @override
  List<Object> get props => [];
}

class BookingHistoryLoading extends BookingHistoryState {
  const BookingHistoryLoading();
}

class BookingHistoryLoaded extends BookingHistoryState {
  final List<Map<String, dynamic>> bookings;
  const BookingHistoryLoaded({required this.bookings});

  @override
  List<Object> get props => [bookings];
}

class CancellationProcessing extends BookingHistoryState {
  final List<Map<String, dynamic>> bookings;
  const CancellationProcessing({required this.bookings});

  @override
  List<Object> get props => [bookings];
}

class CancellationResult extends BookingHistoryState {
  final List<Map<String, dynamic>> bookings;
  final double refundAmount;
  final double refundPercentage;

  const CancellationResult({
    required this.bookings,
    required this.refundAmount,
    required this.refundPercentage,
  });

  @override
  List<Object> get props => [bookings, refundAmount, refundPercentage];
}

class BookingHistoryError extends BookingHistoryState {
  final String message;
  const BookingHistoryError(this.message);

  @override
  List<Object> get props => [message];
}
