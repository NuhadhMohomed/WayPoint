import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import '../data/fleet_api_service.dart';
import '../../booking/models/booking_models.dart';
import '../../booking/services/booking_api_service.dart';

part 'seat_picker_event.dart';
part 'seat_picker_state.dart';

/// BLoC for MOB-05: Interactive Seat Picker with 10-minute hold.
///
/// Flow: LoadSeatMap → user taps seats → HoldSeats → 10-min countdown → checkout or HoldExpired.
class SeatPickerBloc extends Bloc<SeatPickerEvent, SeatPickerState> {
  final FleetApiService _api;
  final BookingApiService _bookingApi;
  String _serviceId = '';
  StreamSubscription<int>? _timerSubscription;

  SeatPickerBloc({FleetApiService? api, BookingApiService? bookingApi})
      : _api = api ?? FleetApiService(),
        _bookingApi = bookingApi ?? BookingApiService(),
        super(const SeatPickerState()) {
    on<LoadSeatMap>(_onLoadSeatMap);
    on<SelectSeat>(_onSelectSeat);
    on<DeselectSeat>(_onDeselectSeat);
    on<HoldSeats>(_onHoldSeats);
    on<HoldTimerTick>(_onHoldTimerTick);
    on<HoldExpired>(_onHoldExpired);
  }

  Future<void> _onLoadSeatMap(LoadSeatMap event, Emitter<SeatPickerState> emit) async {
    _serviceId = event.serviceId;
    emit(state.copyWith(status: SeatPickerStatus.loading));
    try {
      final matrix = await _api.getServiceSeats(event.serviceId);
      emit(state.copyWith(status: SeatPickerStatus.loaded, seatMatrix: matrix, selectedSeats: []));
    } catch (e) {
      emit(state.copyWith(status: SeatPickerStatus.error, errorMessage: e.toString()));
    }
  }

  void _onSelectSeat(SelectSeat event, Emitter<SeatPickerState> emit) {
    if (state.status == SeatPickerStatus.seatsHeld) return; // locked during hold
    final updated = List<String>.from(state.selectedSeats);
    if (!updated.contains(event.seatNumber)) {
      updated.add(event.seatNumber);
      emit(state.copyWith(selectedSeats: updated));
    }
  }

  void _onDeselectSeat(DeselectSeat event, Emitter<SeatPickerState> emit) {
    if (state.status == SeatPickerStatus.seatsHeld) return;
    final updated = List<String>.from(state.selectedSeats)..remove(event.seatNumber);
    emit(state.copyWith(selectedSeats: updated));
  }

  Future<void> _onHoldSeats(HoldSeats event, Emitter<SeatPickerState> emit) async {
    emit(state.copyWith(status: SeatPickerStatus.seatsHolding));
    try {
      final hold = await _bookingApi.createSeatHold(
        serviceId: _serviceId,
        seatNumbers: state.selectedSeats,
      );
      final remaining = hold.remainingSeconds > 0 ? hold.remainingSeconds : 600;
      emit(state.copyWith(
        status: SeatPickerStatus.seatsHeld,
        holdInfo: hold,
        remainingSeconds: remaining,
      ));
      _startCountdown(remaining);
    } catch (e) {
      emit(state.copyWith(status: SeatPickerStatus.seatsHeld, remainingSeconds: 600));
      _startCountdown(600);
    }
  }

  void _onHoldTimerTick(HoldTimerTick event, Emitter<SeatPickerState> emit) {
    if (event.remainingSeconds <= 0) {
      add(const HoldExpired());
    } else {
      emit(state.copyWith(remainingSeconds: event.remainingSeconds));
    }
  }

  void _onHoldExpired(HoldExpired event, Emitter<SeatPickerState> emit) {
    _timerSubscription?.cancel();
    emit(state.copyWith(status: SeatPickerStatus.holdExpired, selectedSeats: [], remainingSeconds: 0));
  }

  void _startCountdown([int initialRemaining = 600]) {
    _timerSubscription?.cancel();
    int remaining = initialRemaining;
    _timerSubscription =
        Stream.periodic(const Duration(seconds: 1), (x) => remaining - x - 1).take(remaining).listen(
      (timeLeft) => add(HoldTimerTick(remainingSeconds: timeLeft)),
    );
  }

  @override
  Future<void> close() {
    _timerSubscription?.cancel();
    return super.close();
  }
}
