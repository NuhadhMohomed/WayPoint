import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';

part 'booking_history_event.dart';
part 'booking_history_state.dart';

/// BLoC for MOB-08: Booking History & Tiered Refund Modal.
///
/// Tiered refund policy:
///   >24h before departure: 90% refund
///   12–24h: 50% refund
///   <12h: Non-refundable (0%)
class BookingHistoryBloc extends Bloc<BookingHistoryEvent, BookingHistoryState> {
  BookingHistoryBloc() : super(const BookingHistoryLoading()) {
    on<LoadBookingHistory>(_onLoad);
    on<CancelBooking>(_onCancel);
  }

  Future<void> _onLoad(LoadBookingHistory event, Emitter<BookingHistoryState> emit) async {
    emit(const BookingHistoryLoading());
    await Future.delayed(const Duration(milliseconds: 800));

    final bookings = [
      {
        'id': 'BK-554433',
        'serviceCode': 'CMB-KAN-001',
        'route': 'Colombo → Kandy',
        'departureTime': DateTime.now().add(const Duration(hours: 48)).toIso8601String(),
        'seats': ['1A', '1B'],
        'status': 'Confirmed',
        'amount': 2500.0,
      },
      {
        'id': 'BK-778899',
        'serviceCode': 'CMB-GAL-002',
        'route': 'Colombo → Galle',
        'departureTime': DateTime.now().add(const Duration(hours: 10)).toIso8601String(),
        'seats': ['7A'],
        'status': 'Confirmed',
        'amount': 1800.0,
      },
      {
        'id': 'BK-112233',
        'serviceCode': 'CMB-GAL-002',
        'route': 'Colombo → Galle',
        'departureTime': DateTime.now().subtract(const Duration(days: 5)).toIso8601String(),
        'seats': ['4C'],
        'status': 'Completed',
        'amount': 1200.0,
      },
      {
        'id': 'BK-998877',
        'serviceCode': 'KAN-CMB-003',
        'route': 'Kandy → Colombo',
        'departureTime': DateTime.now().subtract(const Duration(days: 1)).toIso8601String(),
        'seats': ['10D'],
        'status': 'Cancelled',
        'amount': 1500.0,
      },
    ];

    emit(BookingHistoryLoaded(bookings: bookings));
  }

  Future<void> _onCancel(CancelBooking event, Emitter<BookingHistoryState> emit) async {
    final currentBookings = (state is BookingHistoryLoaded)
        ? (state as BookingHistoryLoaded).bookings
        : <Map<String, dynamic>>[];

    emit(CancellationProcessing(bookings: currentBookings));
    await Future.delayed(const Duration(seconds: 2));

    // Tiered refund calculation
    final departure = DateTime.parse(event.booking['departureTime'] as String);
    final hoursUntil = departure.difference(DateTime.now()).inHours;
    final amount = (event.booking['amount'] as num).toDouble();

    double percentage;
    if (hoursUntil > 24) {
      percentage = 0.9; // 90%
    } else if (hoursUntil >= 12) {
      percentage = 0.5; // 50%
    } else {
      percentage = 0.0; // Non-refundable
    }

    emit(CancellationResult(
      bookings: currentBookings,
      refundAmount: amount * percentage,
      refundPercentage: percentage,
    ));

    // Reload list after showing result
    await Future.delayed(const Duration(seconds: 1));
    add(const LoadBookingHistory());
  }
}
