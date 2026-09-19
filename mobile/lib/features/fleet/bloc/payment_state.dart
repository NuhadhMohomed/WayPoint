part of 'payment_bloc.dart';

enum PaymentStatus { initial, processing, success, failed }

/// Immutable state for MOB-06 Payment Checkout.
class PaymentState extends Equatable {
  final PaymentStatus status;
  final String serviceCode;
  final List<String> seatNumbers;
  final double totalAmount;
  final String? bookingReference;
  final String? errorMessage;

  const PaymentState({
    this.status = PaymentStatus.initial,
    this.serviceCode = '',
    this.seatNumbers = const [],
    this.totalAmount = 0,
    this.bookingReference,
    this.errorMessage,
  });

  PaymentState copyWith({
    PaymentStatus? status,
    String? serviceCode,
    List<String>? seatNumbers,
    double? totalAmount,
    String? bookingReference,
    String? errorMessage,
  }) {
    return PaymentState(
      status: status ?? this.status,
      serviceCode: serviceCode ?? this.serviceCode,
      seatNumbers: seatNumbers ?? this.seatNumbers,
      totalAmount: totalAmount ?? this.totalAmount,
      bookingReference: bookingReference ?? this.bookingReference,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }

  @override
  List<Object?> get props => [status, serviceCode, seatNumbers, totalAmount, bookingReference, errorMessage];
}
