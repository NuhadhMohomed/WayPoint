import 'package:equatable/equatable.dart';

/// Represents a disrupted transit leg and its AI-generated replacement.
class DisruptionAlertModel extends Equatable {
  final String bookingReference;
  final String passengerName;
  final String disruptionTitle;
  final String disruptionReason;
  final String compensationNotice;
  
  // Original Service Details
  final String originalBusPlate;
  final String originalBusClass;
  final String originalDepartureTime;
  final String originalOriginStop;
  final String originalArrivalTime;
  final String originalDestinationStop;
  final double originalFarePaid;

  // AI Recommended Replacement Details
  final String replacementBusPlate;
  final String replacementBusClass;
  final String replacementDepartureTime;
  final String replacementOriginStop;
  final String replacementArrivalTime;
  final String replacementDestinationStop;
  final List<String> assignedSeats;
  final double fareDifference;
  final String fareDifferenceLabel;

  // Hold expiry (15-minute rebooking seat lock)
  final DateTime holdExpiresAt;

  const DisruptionAlertModel({
    required this.bookingReference,
    required this.passengerName,
    required this.disruptionTitle,
    required this.disruptionReason,
    required this.compensationNotice,
    required this.originalBusPlate,
    required this.originalBusClass,
    required this.originalDepartureTime,
    required this.originalOriginStop,
    required this.originalArrivalTime,
    required this.originalDestinationStop,
    required this.originalFarePaid,
    required this.replacementBusPlate,
    required this.replacementBusClass,
    required this.replacementDepartureTime,
    required this.replacementOriginStop,
    required this.replacementArrivalTime,
    required this.replacementDestinationStop,
    required this.assignedSeats,
    required this.fareDifference,
    required this.fareDifferenceLabel,
    required this.holdExpiresAt,
  });

  /// Remaining seconds on the 15-minute temporary seat lock.
  int get remainingSeconds {
    final now = DateTime.now();
    if (now.isAfter(holdExpiresAt)) return 0;
    return holdExpiresAt.difference(now).inSeconds;
  }

  bool get isExpired => DateTime.now().isAfter(holdExpiresAt);

  /// Sample scenario matching Google Stitch Screen MOB-09 (Screen ID: 2bfd1cb2561245a99f17148299e4099d).
  factory DisruptionAlertModel.sampleColomboToElla() {
    return DisruptionAlertModel(
      bookingReference: 'WP-8F29K1',
      passengerName: 'Nimal Silva',
      disruptionTitle: 'Service Disrupted: Bus ND-8821 Mechanical Failure',
      disruptionReason:
          'Your scheduled 06:30 AM Colombo to Ella service has experienced a mechanical breakdown near Kadawatha. Our AI operations engine has generated an optimized replacement itinerary.',
      compensationNotice: 'Complimentary tea/coffee voucher at Kandy Rest Stop included',
      originalBusPlate: 'Bus ND-8821',
      originalBusClass: 'Standard AC',
      originalDepartureTime: '06:30 AM',
      originalOriginStop: 'Bastion Hill',
      originalArrivalTime: '11:45 AM',
      originalDestinationStop: 'Ella Town',
      originalFarePaid: 4950.0,
      replacementBusPlate: 'Bus WP-CAD-4120',
      replacementBusClass: 'Super Line Luxury Coach',
      replacementDepartureTime: '07:15 AM',
      replacementOriginStop: 'Bastion Hill',
      replacementArrivalTime: '12:20 PM',
      replacementDestinationStop: 'Ella Town',
      assignedSeats: const ['1A', '2A'],
      fareDifference: 0.0,
      fareDifferenceLabel: 'Rs. 0.00 — Fully Absorbed',
      holdExpiresAt: DateTime.now().add(const Duration(minutes: 15)),
    );
  }

  @override
  List<Object?> get props => [
        bookingReference,
        passengerName,
        disruptionTitle,
        disruptionReason,
        originalBusPlate,
        replacementBusPlate,
        holdExpiresAt,
      ];
}
