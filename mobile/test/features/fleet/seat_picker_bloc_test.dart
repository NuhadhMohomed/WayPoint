import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:waypoint_mobile/features/fleet/bloc/seat_picker_bloc.dart';
import 'package:waypoint_mobile/features/fleet/data/fleet_api_service.dart';

class MockFleetApiService extends Mock implements FleetApiService {}

void main() {
  group('SeatPickerBloc', () {
    late MockFleetApiService mockApi;
    late SeatPickerBloc bloc;

    setUp(() {
      mockApi = MockFleetApiService();
      bloc = SeatPickerBloc(api: mockApi);
    });

    tearDown(() {
      bloc.close();
    });

    test('initial state is correct', () {
      expect(bloc.state.status, equals(SeatPickerStatus.initial));
      expect(bloc.state.selectedSeats, isEmpty);
    });

    blocTest<SeatPickerBloc, SeatPickerState>(
      'LoadSeatMap emits loading then loaded with empty selection',
      build: () {
        when(() => mockApi.getServiceSeats(any())).thenAnswer((_) async => {});
        return bloc;
      },
      act: (bloc) => bloc.add(const LoadSeatMap('svc-123')),
      expect: () => [
        const SeatPickerState(status: SeatPickerStatus.loading),
        const SeatPickerState(status: SeatPickerStatus.loaded, seatMatrix: {}, selectedSeats: []),
      ],
      verify: (_) {
        verify(() => mockApi.getServiceSeats('svc-123')).called(1);
      },
    );

    blocTest<SeatPickerBloc, SeatPickerState>(
      'SelectSeat adds seat to selection if not held',
      build: () => bloc,
      seed: () => const SeatPickerState(status: SeatPickerStatus.loaded, selectedSeats: []),
      act: (bloc) {
        bloc.add(const SelectSeat('1A'));
        bloc.add(const SelectSeat('1B'));
      },
      expect: () => [
        const SeatPickerState(status: SeatPickerStatus.loaded, selectedSeats: ['1A']),
        const SeatPickerState(status: SeatPickerStatus.loaded, selectedSeats: ['1A', '1B']),
      ],
    );

    blocTest<SeatPickerBloc, SeatPickerState>(
      'SelectSeat ignores duplicate seats',
      build: () => bloc,
      seed: () => const SeatPickerState(status: SeatPickerStatus.loaded, selectedSeats: ['1A']),
      act: (bloc) => bloc.add(const SelectSeat('1A')),
      expect: () => [], // No new state emitted
    );

    blocTest<SeatPickerBloc, SeatPickerState>(
      'SelectSeat is ignored when seats are held',
      build: () => bloc,
      seed: () => const SeatPickerState(status: SeatPickerStatus.seatsHeld, selectedSeats: ['1A']),
      act: (bloc) => bloc.add(const SelectSeat('1B')),
      expect: () => [], // Ignored
    );

    blocTest<SeatPickerBloc, SeatPickerState>(
      'DeselectSeat removes seat from selection',
      build: () => bloc,
      seed: () => const SeatPickerState(status: SeatPickerStatus.loaded, selectedSeats: ['1A', '1B']),
      act: (bloc) => bloc.add(const DeselectSeat('1A')),
      expect: () => [
        const SeatPickerState(status: SeatPickerStatus.loaded, selectedSeats: ['1B']),
      ],
    );

    blocTest<SeatPickerBloc, SeatPickerState>(
      'HoldSeats transitions to seatsHolding then seatsHeld',
      build: () => bloc,
      seed: () => const SeatPickerState(status: SeatPickerStatus.loaded, selectedSeats: ['1A']),
      act: (bloc) => bloc.add(const HoldSeats()),
      expect: () => [
        const SeatPickerState(status: SeatPickerStatus.seatsHolding, selectedSeats: ['1A']),
        const SeatPickerState(status: SeatPickerStatus.seatsHeld, selectedSeats: ['1A'], remainingSeconds: 600),
      ],
    );

    blocTest<SeatPickerBloc, SeatPickerState>(
      'HoldTimerTick updates remainingSeconds',
      build: () => bloc,
      seed: () => const SeatPickerState(status: SeatPickerStatus.seatsHeld, selectedSeats: ['1A'], remainingSeconds: 600),
      act: (bloc) => bloc.add(const HoldTimerTick(remainingSeconds: 599)),
      expect: () => [
        const SeatPickerState(status: SeatPickerStatus.seatsHeld, selectedSeats: ['1A'], remainingSeconds: 599),
      ],
    );

    blocTest<SeatPickerBloc, SeatPickerState>(
      'HoldExpired transitions to holdExpired and clears selection',
      build: () => bloc,
      seed: () => const SeatPickerState(status: SeatPickerStatus.seatsHeld, selectedSeats: ['1A'], remainingSeconds: 1),
      act: (bloc) => bloc.add(const HoldExpired()),
      expect: () => [
        const SeatPickerState(status: SeatPickerStatus.holdExpired, selectedSeats: [], remainingSeconds: 0),
      ],
    );
  });
}
