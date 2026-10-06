import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';

part 'payment_event.dart';
part 'payment_state.dart';

/// BLoC for MOB-06: Payment Sandbox Checkout.
///
/// Handles sandbox card payment flow. In production this would integrate
/// with an actual payment gateway; the sandbox version simulates a charge.
class PaymentBloc extends Bloc<PaymentEvent, PaymentState> {
  PaymentBloc() : super(const PaymentState()) {
    on<InitiatePayment>(_onInitiatePayment);
    on<ConfirmPayment>(_onConfirmPayment);
    on<PaymentTimeout>(_onPaymentTimeout);
  }

  void _onInitiatePayment(InitiatePayment event, Emitter<PaymentState> emit) {
    emit(state.copyWith(
      status: PaymentStatus.initial,
      serviceCode: event.serviceCode,
      seatNumbers: event.seatNumbers,
      totalAmount: event.totalAmount,
    ));
  }

  Future<void> _onConfirmPayment(ConfirmPayment event, Emitter<PaymentState> emit) async {
    emit(state.copyWith(status: PaymentStatus.processing));

    // Simulate sandbox API call (2s delay)
    await Future.delayed(const Duration(seconds: 2));

    // Mock: cards starting with '4242' always succeed
    if (event.cardNumber.startsWith('4242')) {
      emit(state.copyWith(
        status: PaymentStatus.success,
        bookingReference: 'WP-${DateTime.now().millisecondsSinceEpoch.toString().substring(5)}',
      ));
    } else {
      emit(state.copyWith(
        status: PaymentStatus.failed,
        errorMessage: 'Card declined. Use test card 4242 4242 4242 4242.',
      ));
    }
  }

  void _onPaymentTimeout(PaymentTimeout event, Emitter<PaymentState> emit) {
    emit(state.copyWith(
      status: PaymentStatus.failed,
      errorMessage: 'Seat hold expired. Payment session timed out.',
    ));
  }
}
