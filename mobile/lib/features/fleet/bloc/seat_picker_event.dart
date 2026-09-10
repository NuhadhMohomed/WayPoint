part of 'seat_picker_bloc.dart';

/// Events for the Seat Picker feature (MOB-05).
abstract class SeatPickerEvent extends Equatable {
  const SeatPickerEvent();

  @override
  List<Object> get props => [];
}

/// Load the real-time seat map from the backend.
class LoadSeatMap extends SeatPickerEvent {
  final String serviceId;
  const LoadSeatMap(this.serviceId);

  @override
  List<Object> get props => [serviceId];
}

/// Passenger taps an available seat to select it.
class SelectSeat extends SeatPickerEvent {
  final String seatNumber;
  const SelectSeat(this.seatNumber);

  @override
  List<Object> get props => [seatNumber];
}

/// Passenger taps a selected seat to deselect it.
class DeselectSeat extends SeatPickerEvent {
  final String seatNumber;
  const DeselectSeat(this.seatNumber);

  @override
  List<Object> get props => [seatNumber];
}

/// Passenger confirms selection — triggers 10-minute hold via API.
class HoldSeats extends SeatPickerEvent {
  const HoldSeats();
}

/// Internal tick from the 10-minute countdown stream.
class HoldTimerTick extends SeatPickerEvent {
  final int remainingSeconds;
  const HoldTimerTick({required this.remainingSeconds});

  @override
  List<Object> get props => [remainingSeconds];
}

/// Timer reached zero — hold expired.
class HoldExpired extends SeatPickerEvent {
  const HoldExpired();
}
