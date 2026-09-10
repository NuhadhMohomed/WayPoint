part of 'payment_bloc.dart';

/// Events for the Payment Sandbox Checkout (MOB-06).
abstract class PaymentEvent extends Equatable {
  const PaymentEvent();

  @override
  List<Object> get props => [];
}

/// Initialize payment form with booking summary data.
class InitiatePayment extends PaymentEvent {
  final String serviceCode;
  final List<String> seatNumbers;
  final double totalAmount;

  const InitiatePayment({
    required this.serviceCode,
    required this.seatNumbers,
    required this.totalAmount,
  });

  @override
  List<Object> get props => [serviceCode, seatNumbers, totalAmount];
}

/// User submits sandbox card details.
class ConfirmPayment extends PaymentEvent {
  final String cardNumber;
  final String expiry;
  final String cvv;

  const ConfirmPayment({required this.cardNumber, required this.expiry, required this.cvv});

  @override
  List<Object> get props => [cardNumber, expiry, cvv];
}

/// Seat hold expired during checkout — abort payment.
class PaymentTimeout extends PaymentEvent {
  const PaymentTimeout();
}
