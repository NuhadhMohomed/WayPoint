part of 'seat_picker_bloc.dart';

/// Status progression for the Seat Picker flow.
enum SeatPickerStatus { initial, loading, loaded, seatsHolding, seatsHeld, holdExpired, error }

/// Immutable state for MOB-05 Seat Picker.
class SeatPickerState extends Equatable {
  final SeatPickerStatus status;
  final Map<String, dynamic>? seatMatrix;
  final List<String> selectedSeats;
  final int remainingSeconds;
  final String? errorMessage;

  const SeatPickerState({
    this.status = SeatPickerStatus.initial,
    this.seatMatrix,
    this.selectedSeats = const [],
    this.remainingSeconds = 0,
    this.errorMessage,
  });

  SeatPickerState copyWith({
    SeatPickerStatus? status,
    Map<String, dynamic>? seatMatrix,
    List<String>? selectedSeats,
    int? remainingSeconds,
    String? errorMessage,
  }) {
    return SeatPickerState(
      status: status ?? this.status,
      seatMatrix: seatMatrix ?? this.seatMatrix,
      selectedSeats: selectedSeats ?? this.selectedSeats,
      remainingSeconds: remainingSeconds ?? this.remainingSeconds,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }

  @override
  List<Object?> get props => [status, seatMatrix, selectedSeats, remainingSeconds, errorMessage];
}
