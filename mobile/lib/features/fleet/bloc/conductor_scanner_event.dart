part of 'conductor_scanner_bloc.dart';

abstract class ConductorScannerEvent extends Equatable {
  const ConductorScannerEvent();

  @override
  List<Object> get props => [];
}

/// QR code detected by camera — send payload for verification.
class ScanQrCode extends ConductorScannerEvent {
  final String payload;
  const ScanQrCode(this.payload);

  @override
  List<Object> get props => [payload];
}

/// Reset scanner to ready state for next passenger.
class ResetScanner extends ConductorScannerEvent {
  const ResetScanner();
}
