part of 'booking_history_bloc.dart';

abstract class BookingHistoryEvent extends Equatable {
  const BookingHistoryEvent();

  @override
  List<Object> get props => [];
}

class LoadBookingHistory extends BookingHistoryEvent {
  const LoadBookingHistory();
}

class CancelBooking extends BookingHistoryEvent {
  final Map<String, dynamic> booking;
  const CancelBooking(this.booking);

  @override
  List<Object> get props => [booking];
}
