library;

/// Journey Planning Data Models
/// Component 1: Journey Planning & Route Catalogue (Sethum)

class RouteModel {
  final String id;
  final String routeNumber;
  final String originCity;
  final String destinationCity;
  final int estimatedDurationMinutes;
  final bool isActive;
  final List<RouteStopModel> stops;

  RouteModel({
    required this.id,
    required this.routeNumber,
    required this.originCity,
    required this.destinationCity,
    required this.estimatedDurationMinutes,
    this.isActive = true,
    this.stops = const [],
  });

  factory RouteModel.fromJson(Map<String, dynamic> json) {
    return RouteModel(
      id: json['id']?.toString() ?? '',
      routeNumber: json['routeNumber']?.toString() ?? '',
      originCity: json['originCity']?.toString() ?? '',
      destinationCity: json['destinationCity']?.toString() ?? '',
      estimatedDurationMinutes: json['estimatedDurationMinutes'] ?? 0,
      isActive: json['isActive'] ?? true,
      stops: (json['stops'] as List<dynamic>?)
              ?.map((s) => RouteStopModel.fromJson(s as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }
}

class RouteStopModel {
  final String id;
  final String stopName;
  final int sequenceOrder;
  final int arrivalOffsetMinutes;
  final double distanceFromOriginKm;

  RouteStopModel({
    required this.id,
    required this.stopName,
    required this.sequenceOrder,
    required this.arrivalOffsetMinutes,
    required this.distanceFromOriginKm,
  });

  factory RouteStopModel.fromJson(Map<String, dynamic> json) {
    return RouteStopModel(
      id: json['id']?.toString() ?? '',
      stopName: json['stopName']?.toString() ?? '',
      sequenceOrder: json['sequenceOrder'] ?? 0,
      arrivalOffsetMinutes: json['arrivalOffsetMinutes'] ?? 0,
      distanceFromOriginKm: (json['distanceFromOriginKm'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class TransferLegModel {
  final String serviceId;
  final String serviceCode;
  final String routeNumber;
  final String originCity;
  final String destinationCity;
  final DateTime departureTime;
  final DateTime arrivalTime;
  final double fare;
  final String busClass;

  TransferLegModel({
    required this.serviceId,
    required this.serviceCode,
    required this.routeNumber,
    required this.originCity,
    required this.destinationCity,
    required this.departureTime,
    required this.arrivalTime,
    required this.fare,
    required this.busClass,
  });

  factory TransferLegModel.fromJson(Map<String, dynamic> json) {
    return TransferLegModel(
      serviceId: json['serviceId']?.toString() ?? '',
      serviceCode: json['serviceCode']?.toString() ?? '',
      routeNumber: json['routeNumber']?.toString() ?? '',
      originCity: json['originCity']?.toString() ?? '',
      destinationCity: json['destinationCity']?.toString() ?? '',
      departureTime: DateTime.tryParse(json['departureTime']?.toString() ?? '') ?? DateTime.now(),
      arrivalTime: DateTime.tryParse(json['arrivalTime']?.toString() ?? '') ?? DateTime.now(),
      fare: (json['fare'] as num?)?.toDouble() ?? 0.0,
      busClass: json['busClass']?.toString() ?? 'Standard',
    );
  }
}

class JourneyCandidateModel {
  final String serviceId;
  final String serviceCode;
  final String routeNumber;
  final String origin;
  final String destination;
  final DateTime departureTime;
  final DateTime arrivalTime;
  final double totalFare;
  final int durationMinutes;
  final bool isConnecting;
  final double matchScore;
  final int availableSeats;
  final String busClass;
  final List<TransferLegModel> legs;
  final int? transferBufferMinutes;

  JourneyCandidateModel({
    required this.serviceId,
    required this.serviceCode,
    required this.routeNumber,
    required this.origin,
    required this.destination,
    required this.departureTime,
    required this.arrivalTime,
    required this.totalFare,
    required this.durationMinutes,
    required this.isConnecting,
    required this.matchScore,
    required this.availableSeats,
    required this.busClass,
    this.legs = const [],
    this.transferBufferMinutes,
  });

  factory JourneyCandidateModel.fromJson(Map<String, dynamic> json) {
    return JourneyCandidateModel(
      serviceId: json['serviceId']?.toString() ?? json['id']?.toString() ?? '',
      serviceCode: json['serviceCode']?.toString() ?? 'SRV-000',
      routeNumber: json['routeNumber']?.toString() ?? 'EX-01',
      origin: json['origin']?.toString() ?? json['originCity']?.toString() ?? '',
      destination: json['destination']?.toString() ?? json['destinationCity']?.toString() ?? '',
      departureTime: DateTime.tryParse(json['departureTime']?.toString() ?? '') ?? DateTime.now(),
      arrivalTime: DateTime.tryParse(json['arrivalTime']?.toString() ?? '') ?? DateTime.now(),
      totalFare: (json['totalFare'] as num?)?.toDouble() ?? (json['baseFare'] as num?)?.toDouble() ?? 0.0,
      durationMinutes: json['durationMinutes'] ?? json['estimatedDurationMinutes'] ?? 0,
      isConnecting: json['isConnecting'] ?? false,
      matchScore: (json['matchScore'] as num?)?.toDouble() ?? 0.90,
      availableSeats: json['availableSeats'] ?? 20,
      busClass: json['busClass']?.toString() ?? 'Luxury Super Line',
      legs: (json['legs'] as List<dynamic>?)
              ?.map((l) => TransferLegModel.fromJson(l as Map<String, dynamic>))
              .toList() ??
          [],
      transferBufferMinutes: json['transferBufferMinutes'],
    );
  }

  /// Sample Direct Colombo → Ella candidate (MOB-04)
  factory JourneyCandidateModel.sampleColomboToEllaDirect() {
    final now = DateTime.now();
    final dep = DateTime(now.year, now.month, now.day + 1, 6, 30);
    final arr = dep.add(const Duration(hours: 6));

    return JourneyCandidateModel(
      serviceId: 'srv-col-ella-0630',
      serviceCode: 'SRV-COL-ELLA-0630',
      routeNumber: 'EX-08',
      origin: 'Colombo (Bastian Hill)',
      destination: 'Ella Town Terminal',
      departureTime: dep,
      arrivalTime: arr,
      totalFare: 2400.0,
      durationMinutes: 360,
      isConnecting: false,
      matchScore: 0.96,
      availableSeats: 14,
      busClass: 'Super Line Luxury AC',
    );
  }

  /// Sample Connecting Colombo → Kandy → Ella candidate (BR-TRANSFER-001 >= 20 min)
  factory JourneyCandidateModel.sampleConnectingViaKandy() {
    final now = DateTime.now();
    final leg1Dep = DateTime(now.year, now.month, now.day + 1, 7, 0);
    final leg1Arr = leg1Dep.add(const Duration(hours: 3, minutes: 15));
    // 35-minute transfer window (compliant with >= 20 min BR-TRANSFER-001)
    final leg2Dep = leg1Arr.add(const Duration(minutes: 35));
    final leg2Arr = leg2Dep.add(const Duration(hours: 3, minutes: 30));

    return JourneyCandidateModel(
      serviceId: 'srv-conn-col-kdy-ella',
      serviceCode: 'SRV-CONN-0700',
      routeNumber: 'RT-01 + EX-08',
      origin: 'Colombo (Central Super)',
      destination: 'Ella Town via Kandy',
      departureTime: leg1Dep,
      arrivalTime: leg2Arr,
      totalFare: 2150.0,
      durationMinutes: 440,
      isConnecting: true,
      matchScore: 0.88,
      availableSeats: 8,
      busClass: 'Connecting Express',
      transferBufferMinutes: 35,
      legs: [
        TransferLegModel(
          serviceId: 'srv-col-kdy-0700',
          serviceCode: 'SRV-COL-KDY-0700',
          routeNumber: 'RT-01',
          originCity: 'Colombo',
          destinationCity: 'Kandy Goods Shed',
          departureTime: leg1Dep,
          arrivalTime: leg1Arr,
          fare: 950.0,
          busClass: 'Semi-Luxury',
        ),
        TransferLegModel(
          serviceId: 'srv-kdy-ella-1050',
          serviceCode: 'SRV-KDY-ELLA-1050',
          routeNumber: 'EX-08',
          originCity: 'Kandy Goods Shed',
          destinationCity: 'Ella Town Terminal',
          departureTime: leg2Dep,
          arrivalTime: leg2Arr,
          fare: 1200.0,
          busClass: 'Scenic Mountain Coach',
        ),
      ],
    );
  }

  /// Sample Direct Colombo → Kandy
  factory JourneyCandidateModel.sampleColomboToKandyDirect() {
    final now = DateTime.now();
    final dep = DateTime(now.year, now.month, now.day + 1, 8, 0);
    final arr = dep.add(const Duration(hours: 3, minutes: 15));

    return JourneyCandidateModel(
      serviceId: 'srv-col-kdy-0800',
      serviceCode: 'SRV-COL-KDY-0800',
      routeNumber: 'RT-01',
      origin: 'Colombo (Central Super)',
      destination: 'Kandy Goods Shed',
      departureTime: dep,
      arrivalTime: arr,
      totalFare: 1100.0,
      durationMinutes: 195,
      isConnecting: false,
      matchScore: 0.94,
      availableSeats: 22,
      busClass: 'Intercity Highway Express',
    );
  }
}

class JourneySearchPreferences {
  bool directOnly;
  bool requireAc;
  double maxFare;
  String departureWindow; // Any, Morning, Afternoon, Evening, Night

  JourneySearchPreferences({
    this.directOnly = false,
    this.requireAc = true,
    this.maxFare = 4000.0,
    this.departureWindow = 'Any',
  });
}
