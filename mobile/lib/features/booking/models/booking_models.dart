import 'package:equatable/equatable.dart';

/// Enum representing the simulated outcome of a sandbox payment card.
enum SandboxOutcome {
  success,
  declined,
  timeout,
}

/// Strongly-typed model representing an active temporary 10-minute seat hold.
class SeatHoldInfo extends Equatable {
  final String holdId;
  final String serviceId;
  final String serviceCode;
  final String routeTitle;
  final String originCity;
  final String destinationCity;
  final DateTime departureTime;
  final DateTime arrivalTime;
  final List<String> seatNumbers;
  final double farePerSeat;
  final double serviceFee;
  final DateTime heldAt;
  final DateTime heldUntil;

  const SeatHoldInfo({
    required this.holdId,
    required this.serviceId,
    required this.serviceCode,
    required this.routeTitle,
    required this.originCity,
    required this.destinationCity,
    required this.departureTime,
    required this.arrivalTime,
    required this.seatNumbers,
    required this.farePerSeat,
    this.serviceFee = 150.0,
    required this.heldAt,
    required this.heldUntil,
  });

  int get seatCount => seatNumbers.length;
  double get subtotal => farePerSeat * seatCount;
  double get totalAmount => subtotal + serviceFee;

  /// Total duration in seconds of the original hold window (typically 600s = 10 mins).
  int get totalHoldSeconds {
    final diff = heldUntil.difference(heldAt).inSeconds;
    return diff > 0 ? diff : 600;
  }

  /// Calculates remaining seconds from the current clock.
  int get remainingSeconds {
    final now = DateTime.now();
    if (now.isAfter(heldUntil)) return 0;
    return heldUntil.difference(now).inSeconds;
  }

  /// True if the temporary hold has expired.
  bool get isExpired => DateTime.now().isAfter(heldUntil);

  /// Factory helper providing realistic seed data for Colombo - Ella Scenic Express.
  factory SeatHoldInfo.sampleColomboToElla() {
    final now = DateTime.now();
    return SeatHoldInfo(
      holdId: 'HLD-882190-LK',
      serviceId: 'srv-col-ella-0800-guid',
      serviceCode: 'SRV-COL-ELLA-0800',
      routeTitle: 'Colombo - Ella Highland Scenic Corridor',
      originCity: 'Makumbura MMC (Colombo)',
      destinationCity: 'Ella City Station',
      departureTime: now.add(const Duration(days: 1, hours: 8)),
      arrivalTime: now.add(const Duration(days: 1, hours: 13)),
      seatNumbers: const ['4A', '4B'],
      farePerSeat: 2850.0,
      serviceFee: 150.0,
      heldAt: now,
      heldUntil: now.add(const Duration(minutes: 10)),
    );
  }

  @override
  List<Object?> get props => [
        holdId,
        serviceId,
        serviceCode,
        routeTitle,
        departureTime,
        seatNumbers,
        farePerSeat,
        heldUntil,
      ];
}

/// Model representing preset sandbox test payment cards.
class PaymentSandboxCard extends Equatable {
  final String name;
  final String cardNumber;
  final String expiryDate;
  final String cvv;
  final String cardholderName;
  final SandboxOutcome expectedOutcome;
  final String description;

  const PaymentSandboxCard({
    required this.name,
    required this.cardNumber,
    required this.expiryDate,
    required this.cvv,
    required this.cardholderName,
    required this.expectedOutcome,
    required this.description,
  });

  /// Preset card 1: Instant success authorization
  static const successCard = PaymentSandboxCard(
    name: 'Instant Success',
    cardNumber: '4000 0000 0000 0001',
    expiryDate: '12/28',
    cvv: '123',
    cardholderName: 'NIMAL SILVA',
    expectedOutcome: SandboxOutcome.success,
    description: 'Simulates 200 OK payment confirmation & issues QR e-ticket.',
  );

  /// Preset card 2: Simulated insufficient funds / card decline
  static const declineCard = PaymentSandboxCard(
    name: 'Card Declined',
    cardNumber: '4000 0000 0000 0002',
    expiryDate: '06/27',
    cvv: '456',
    cardholderName: 'NIMAL SILVA',
    expectedOutcome: SandboxOutcome.declined,
    description: 'Simulates 402 Payment Required (Insufficient Funds).',
  );

  /// Preset card 3: Simulated gateway timeout
  static const timeoutCard = PaymentSandboxCard(
    name: 'Gateway Timeout',
    cardNumber: '4000 0000 0000 0003',
    expiryDate: '09/29',
    cvv: '789',
    cardholderName: 'NIMAL SILVA',
    expectedOutcome: SandboxOutcome.timeout,
    description: 'Simulates 504 Gateway Timeout for resilience testing.',
  );

  static const List<PaymentSandboxCard> allPresets = [
    successCard,
    declineCard,
    timeoutCard,
  ];

  @override
  List<Object?> get props => [name, cardNumber, expectedOutcome];
}

/// Transactional booking confirmation result returned upon checkout completion.
class BookingConfirmationResult extends Equatable {
  final bool isSuccess;
  final String bookingReference;
  final String serviceCode;
  final List<String> seatNumbers;
  final double totalAmount;
  final String ticketQrPayload;
  final DateTime confirmedAt;
  final String? errorMessage;

  const BookingConfirmationResult({
    required this.isSuccess,
    this.bookingReference = '',
    this.serviceCode = '',
    this.seatNumbers = const [],
    this.totalAmount = 0.0,
    this.ticketQrPayload = '',
    required this.confirmedAt,
    this.errorMessage,
  });

  factory BookingConfirmationResult.success({
    required String reference,
    required String serviceCode,
    required List<String> seats,
    required double amount,
    required String qrPayload,
  }) {
    return BookingConfirmationResult(
      isSuccess: true,
      bookingReference: reference,
      serviceCode: serviceCode,
      seatNumbers: seats,
      totalAmount: amount,
      ticketQrPayload: qrPayload,
      confirmedAt: DateTime.now(),
    );
  }

  factory BookingConfirmationResult.failure(String error) {
    return BookingConfirmationResult(
      isSuccess: false,
      confirmedAt: DateTime.now(),
      errorMessage: error,
    );
  }

  @override
  List<Object?> get props => [isSuccess, bookingReference, errorMessage];
}
