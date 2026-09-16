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

/// Strongly-typed digital boarding pass model representing issued e-tickets in MOB-07.
class DigitalTicketPass extends Equatable {
  final String ticketId;
  final String bookingReference;
  final String serviceCode;
  final String routeTitle;
  final String originCity;
  final String destinationCity;
  final String boardingPointName;
  final DateTime departureTime;
  final DateTime arrivalTime;
  final String busRegistration;
  final String busClass;
  final List<String> seatNumbers;
  final String passengerName;
  final double totalFare;
  final bool isBoarded;
  final DateTime? boardedAt;
  final String qrCodePayload;
  final DateTime issuedAt;

  const DigitalTicketPass({
    required this.ticketId,
    required this.bookingReference,
    required this.serviceCode,
    required this.routeTitle,
    required this.originCity,
    required this.destinationCity,
    required this.boardingPointName,
    required this.departureTime,
    required this.arrivalTime,
    required this.busRegistration,
    required this.busClass,
    required this.seatNumbers,
    required this.passengerName,
    required this.totalFare,
    this.isBoarded = false,
    this.boardedAt,
    required this.qrCodePayload,
    required this.issuedAt,
  });

  bool get isUpcoming => departureTime.isAfter(DateTime.now()) && !isBoarded;

  /// Sample upcoming ticket for Colombo - Ella Scenic Corridor (Tomorrow)
  factory DigitalTicketPass.sampleColomboToElla() {
    final tomorrow = DateTime.now().add(const Duration(days: 1));
    final dep = DateTime(tomorrow.year, tomorrow.month, tomorrow.day, 8, 0);
    final arr = DateTime(tomorrow.year, tomorrow.month, tomorrow.day, 13, 0);

    return DigitalTicketPass(
      ticketId: 'TCK-99014-LK',
      bookingReference: 'WP-7B92K1',
      serviceCode: 'SRV-COL-ELLA-0800',
      routeTitle: 'Colombo - Ella Highland Scenic Corridor',
      originCity: 'Makumbura MMC (Colombo)',
      destinationCity: 'Ella City Station',
      boardingPointName: 'Platform 3, Makumbura Highway Terminal',
      departureTime: dep,
      arrivalTime: arr,
      busRegistration: 'NC-8890',
      busClass: 'SuperLuxury Express',
      seatNumbers: const ['4A', '4B'],
      passengerName: 'Nimal Silva',
      totalFare: 5700.0,
      isBoarded: false,
      qrCodePayload: 'WP|REF:WP-7B92K1|SRV:SRV-COL-ELLA-0800|SEATS:4A,4B|PASS:Nimal Silva|HMAC:a8f93c2e71d4b6',
      issuedAt: DateTime.now().subtract(const Duration(minutes: 5)),
    );
  }

  /// Sample completed ticket for Colombo - Kandy Intercity Express
  factory DigitalTicketPass.sampleColomboToKandy() {
    final past = DateTime.now().subtract(const Duration(days: 3));
    final dep = DateTime(past.year, past.month, past.day, 7, 0);
    final arr = DateTime(past.year, past.month, past.day, 10, 0);

    return DigitalTicketPass(
      ticketId: 'TCK-77182-LK',
      bookingReference: 'WP-3X88M9',
      serviceCode: 'SRV-COL-KDY-0700',
      routeTitle: 'Colombo - Kandy Intercity Express',
      originCity: 'Colombo Fort Central Terminal',
      destinationCity: 'Kandy Goodshed Terminal',
      boardingPointName: 'Bay 4, Fort Central Bus Stand',
      departureTime: dep,
      arrivalTime: arr,
      busRegistration: 'ND-5421',
      busClass: 'Luxury Air-Conditioned',
      seatNumbers: const ['2C'],
      passengerName: 'Nimal Silva',
      totalFare: 1450.0,
      isBoarded: true,
      boardedAt: dep.subtract(const Duration(minutes: 12)),
      qrCodePayload: 'WP|REF:WP-3X88M9|SRV:SRV-COL-KDY-0700|SEATS:2C|PASS:Nimal Silva|HMAC:f1c099e2a84b',
      issuedAt: past.subtract(const Duration(days: 1)),
    );
  }

  @override
  List<Object?> get props => [
        ticketId,
        bookingReference,
        serviceCode,
        seatNumbers,
        isBoarded,
        qrCodePayload,
      ];
}

