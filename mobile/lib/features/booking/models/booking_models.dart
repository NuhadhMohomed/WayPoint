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

/// Model representing a historical booking item in MOB-08.
class HistoricalBookingItem extends Equatable {
  final String bookingId;
  final String bookingReference;
  final String serviceCode;
  final String routeTitle;
  final String originCity;
  final String destinationCity;
  final DateTime departureTime;
  final DateTime arrivalTime;
  final List<String> seatNumbers;
  final double totalPaid;
  final String status; // 'Confirmed', 'Completed', 'Cancelled'
  final DateTime bookedAt;
  final double? refundAmount;
  final double? refundPercentage;
  final String? cancellationReason;

  const HistoricalBookingItem({
    required this.bookingId,
    required this.bookingReference,
    required this.serviceCode,
    required this.routeTitle,
    required this.originCity,
    required this.destinationCity,
    required this.departureTime,
    required this.arrivalTime,
    required this.seatNumbers,
    required this.totalPaid,
    required this.status,
    required this.bookedAt,
    this.refundAmount,
    this.refundPercentage,
    this.cancellationReason,
  });

  bool get isUpcoming => departureTime.isAfter(DateTime.now()) && status == 'Confirmed';
  bool get isCancelled => status == 'Cancelled';
  bool get isCompleted => status == 'Completed' || (departureTime.isBefore(DateTime.now()) && status != 'Cancelled');

  /// Hours until departure from current clock.
  int get hoursUntilDeparture => departureTime.difference(DateTime.now()).inHours;

  /// Deterministic BR-REFUND-001 Tiered Policy
  double get refundTierPercentage {
    if (hoursUntilDeparture > 24) return 0.90; // 90%
    if (hoursUntilDeparture >= 12) return 0.50; // 50%
    return 0.0; // 0% non-refundable
  }

  double get estimatedRefundAmount => totalPaid * refundTierPercentage;

  HistoricalBookingItem copyWith({
    String? status,
    double? refundAmount,
    double? refundPercentage,
    String? cancellationReason,
  }) {
    return HistoricalBookingItem(
      bookingId: bookingId,
      bookingReference: bookingReference,
      serviceCode: serviceCode,
      routeTitle: routeTitle,
      originCity: originCity,
      destinationCity: destinationCity,
      departureTime: departureTime,
      arrivalTime: arrivalTime,
      seatNumbers: seatNumbers,
      totalPaid: totalPaid,
      status: status ?? this.status,
      bookedAt: bookedAt,
      refundAmount: refundAmount ?? this.refundAmount,
      refundPercentage: refundPercentage ?? this.refundPercentage,
      cancellationReason: cancellationReason ?? this.cancellationReason,
    );
  }

  static List<HistoricalBookingItem> sampleBookings() {
    final now = DateTime.now();

    // 1. Upcoming booking (> 24h away - 90% refund eligible)
    final tomorrow = now.add(const Duration(days: 1, hours: 4));
    final b1 = HistoricalBookingItem(
      bookingId: 'BK-10029-LK',
      bookingReference: 'WP-7B92K1',
      serviceCode: 'SRV-COL-ELLA-0800',
      routeTitle: 'Colombo - Ella Highland Scenic Corridor',
      originCity: 'Makumbura MMC (Colombo)',
      destinationCity: 'Ella City Station',
      departureTime: tomorrow,
      arrivalTime: tomorrow.add(const Duration(hours: 5)),
      seatNumbers: const ['4A', '4B'],
      totalPaid: 5700.0,
      status: 'Confirmed',
      bookedAt: now.subtract(const Duration(hours: 2)),
    );

    // 2. Upcoming booking (14h away - 50% refund eligible)
    final soon = now.add(const Duration(hours: 14));
    final b2 = HistoricalBookingItem(
      bookingId: 'BK-10044-LK',
      bookingReference: 'WP-5D11K8',
      serviceCode: 'SRV-COL-GAL-0930',
      routeTitle: 'Colombo - Galle Southern Expressway Direct',
      originCity: 'Makumbura MMC (Colombo)',
      destinationCity: 'Pinnaduwa (Galle)',
      departureTime: soon,
      arrivalTime: soon.add(const Duration(hours: 1, minutes: 15)),
      seatNumbers: const ['12C'],
      totalPaid: 1150.0,
      status: 'Confirmed',
      bookedAt: now.subtract(const Duration(hours: 10)),
    );

    // 3. Completed Past booking
    final past = now.subtract(const Duration(days: 3));
    final b3 = HistoricalBookingItem(
      bookingId: 'BK-09822-LK',
      bookingReference: 'WP-3X88M9',
      serviceCode: 'SRV-COL-KDY-0700',
      routeTitle: 'Colombo - Kandy Intercity Express',
      originCity: 'Colombo Fort Central Terminal',
      destinationCity: 'Kandy Goodshed Terminal',
      departureTime: past,
      arrivalTime: past.add(const Duration(hours: 3)),
      seatNumbers: const ['2C'],
      totalPaid: 1450.0,
      status: 'Completed',
      bookedAt: past.subtract(const Duration(days: 2)),
    );

    // 4. Cancelled / Refunded booking
    final cancelledPast = now.subtract(const Duration(days: 7));
    final b4 = HistoricalBookingItem(
      bookingId: 'BK-09110-LK',
      bookingReference: 'WP-1A99Z3',
      serviceCode: 'SRV-COL-SIG-0630',
      routeTitle: 'Colombo - Sigiriya Heritage Express',
      originCity: 'Colombo Fort Central Terminal',
      destinationCity: 'Sigiriya Cultural Junction',
      departureTime: cancelledPast,
      arrivalTime: cancelledPast.add(const Duration(hours: 4)),
      seatNumbers: const ['3B'],
      totalPaid: 2400.0,
      status: 'Cancelled',
      bookedAt: cancelledPast.subtract(const Duration(days: 3)),
      refundAmount: 2160.0,
      refundPercentage: 0.90,
      cancellationReason: 'Change of travel plans',
    );

    return [b1, b2, b3, b4];
  }

  @override
  List<Object?> get props => [
        bookingId,
        bookingReference,
        status,
        totalPaid,
        refundAmount,
      ];
}


