import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';

part 'conductor_scanner_event.dart';
part 'conductor_scanner_state.dart';

/// BLoC for MOB-10: Conductor QR Boarding Scanner.
///
/// Receives scanned QR payloads, verifies against backend,
/// and emits TicketValid (green overlay) or TicketInvalid (red overlay).
class ConductorScannerBloc extends Bloc<ConductorScannerEvent, ConductorScannerState> {
  ConductorScannerBloc() : super(const ScannerReady()) {
    on<ScanQrCode>(_onScan);
    on<ResetScanner>(_onReset);
  }

  Future<void> _onScan(ScanQrCode event, Emitter<ConductorScannerState> emit) async {
    if (state is VerifyingTicket) return; // Prevent duplicate scans

    emit(const VerifyingTicket());
    await Future.delayed(const Duration(seconds: 1)); // Simulate API verification

    // Mock: tickets containing 'TK-12345' are valid
    if (event.payload.contains('TK-12345')) {
      emit(const TicketValid(
        passengerName: 'Nuhadh Mohomed',
        seatNumbers: ['12A', '12B'],
      ));
    } else if (event.payload.contains('TK-00987')) {
      emit(const TicketValid(
        passengerName: 'Kamal Perera',
        seatNumbers: ['4C'],
      ));
    } else {
      emit(const TicketInvalid(reason: 'Ticket not found or already scanned.'));
    }
  }

  void _onReset(ResetScanner event, Emitter<ConductorScannerState> emit) {
    emit(const ScannerReady());
  }
}
