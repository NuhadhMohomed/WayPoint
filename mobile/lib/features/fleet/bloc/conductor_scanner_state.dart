part of 'conductor_scanner_bloc.dart';

abstract class ConductorScannerState extends Equatable {
  const ConductorScannerState();

  @override
  List<Object> get props => [];
}

/// Camera is active and waiting for a QR scan.
class ScannerReady extends ConductorScannerState {
  const ScannerReady();
}

/// QR detected — verifying payload against backend.
class VerifyingTicket extends ConductorScannerState {
  const VerifyingTicket();
}

/// Ticket verified successfully — show green BOARDED overlay.
class TicketValid extends ConductorScannerState {
  final String passengerName;
  final List<String> seatNumbers;

  const TicketValid({required this.passengerName, required this.seatNumbers});

  @override
  List<Object> get props => [passengerName, seatNumbers];
}

/// Ticket invalid — show red overlay with reason.
class TicketInvalid extends ConductorScannerState {
  final String reason;
  const TicketInvalid({required this.reason});

  @override
  List<Object> get props => [reason];
}
