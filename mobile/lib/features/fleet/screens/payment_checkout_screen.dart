import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/payment_bloc.dart';

/// MOB-06: Payment Sandbox Checkout with Hold Countdown Bar.
///
/// Shows booking summary, hold timer, and sandbox card form.
/// Test card: 4242 4242 4242 4242 (always succeeds).
class PaymentCheckoutScreen extends StatelessWidget {
  final int initialRemainingSeconds;
  final List<String> selectedSeats;
  final String serviceCode;
  final double amount;

  const PaymentCheckoutScreen({
    super.key,
    required this.initialRemainingSeconds,
    required this.selectedSeats,
    required this.serviceCode,
    required this.amount,
  });

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => PaymentBloc()
        ..add(InitiatePayment(serviceCode: serviceCode, seatNumbers: selectedSeats, totalAmount: amount)),
      child: _CheckoutBody(initialRemainingSeconds: initialRemainingSeconds),
    );
  }
}

class _CheckoutBody extends StatefulWidget {
  final int initialRemainingSeconds;
  const _CheckoutBody({required this.initialRemainingSeconds});

  @override
  State<_CheckoutBody> createState() => _CheckoutBodyState();
}

class _CheckoutBodyState extends State<_CheckoutBody> {
  late int _remaining;
  Timer? _timer;

  final _cardCtrl = TextEditingController(text: '4242 4242 4242 4242');
  final _expiryCtrl = TextEditingController(text: '12/28');
  final _cvvCtrl = TextEditingController(text: '123');

  @override
  void initState() {
    super.initState();
    _remaining = widget.initialRemainingSeconds;
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted) return;
      setState(() => _remaining--);
      if (_remaining <= 0) {
        _timer?.cancel();
        context.read<PaymentBloc>().add(const PaymentTimeout());
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    _cardCtrl.dispose();
    _expiryCtrl.dispose();
    _cvvCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Checkout'),
        backgroundColor: const Color(0xFF0056D2),
        foregroundColor: Colors.white,
      ),
      body: BlocConsumer<PaymentBloc, PaymentState>(
        listener: (context, state) {
          if (state.status == PaymentStatus.success) {
            showDialog(
              context: context,
              barrierDismissible: false,
              builder: (_) => AlertDialog(
                icon: const Icon(Icons.check_circle, color: Colors.green, size: 48),
                title: const Text('Payment Successful!'),
                content: Text('Booking Reference: ${state.bookingReference}\n\nYour ticket has been added to your wallet.'),
                actions: [
                  TextButton(
                    onPressed: () {
                      Navigator.of(context).pop(); // dialog
                      Navigator.of(context).pop(); // back
                    },
                    child: const Text('View Ticket'),
                  ),
                ],
              ),
            );
          } else if (state.status == PaymentStatus.failed) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.errorMessage ?? 'Payment failed'), backgroundColor: Colors.red),
            );
          }
        },
        builder: (context, state) {
          return Column(
            children: [
              // Hold countdown bar
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(vertical: 10),
                color: _remaining > 60 ? Colors.amber : Colors.red,
                child: Text(
                  'Complete payment in ${_remaining ~/ 60}:${(_remaining % 60).toString().padLeft(2, '0')}',
                  textAlign: TextAlign.center,
                  style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.white, fontSize: 14),
                ),
              ),
              Expanded(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Booking Summary Card
                      Card(
                        elevation: 0,
                        shape: RoundedRectangleBorder(
                          side: BorderSide(color: Colors.grey.shade300),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Booking Summary', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                              const SizedBox(height: 12),
                              _SummaryRow(label: 'Service', value: state.serviceCode),
                              _SummaryRow(label: 'Seats', value: state.seatNumbers.join(', ')),
                              const Divider(height: 24),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  const Text('Total Fare', style: TextStyle(fontWeight: FontWeight.bold)),
                                  Text(
                                    'Rs. ${state.totalAmount.toStringAsFixed(2)}',
                                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 20, color: Color(0xFF0056D2)),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(height: 24),
                      const Text('Payment Details (Sandbox)', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                      const SizedBox(height: 4),
                      Text('Use test card 4242 4242 4242 4242', style: TextStyle(fontSize: 12, color: Colors.grey.shade600)),
                      const SizedBox(height: 16),
                      TextField(
                        controller: _cardCtrl,
                        decoration: const InputDecoration(labelText: 'Card Number', border: OutlineInputBorder()),
                        keyboardType: TextInputType.number,
                      ),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Expanded(child: TextField(controller: _expiryCtrl, decoration: const InputDecoration(labelText: 'Expiry', border: OutlineInputBorder()))),
                          const SizedBox(width: 16),
                          Expanded(child: TextField(controller: _cvvCtrl, decoration: const InputDecoration(labelText: 'CVV', border: OutlineInputBorder()), obscureText: true)),
                        ],
                      ),
                      const SizedBox(height: 32),
                      SizedBox(
                        width: double.infinity,
                        height: 52,
                        child: ElevatedButton(
                          onPressed: state.status == PaymentStatus.processing || _remaining <= 0
                              ? null
                              : () {
                                  context.read<PaymentBloc>().add(ConfirmPayment(
                                    cardNumber: _cardCtrl.text.replaceAll(' ', ''),
                                    expiry: _expiryCtrl.text,
                                    cvv: _cvvCtrl.text,
                                  ));
                                },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: const Color(0xFF0056D2),
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                          ),
                          child: state.status == PaymentStatus.processing
                              ? const CircularProgressIndicator(color: Colors.white, strokeWidth: 2)
                              : Text(
                                  'Pay Rs. ${state.totalAmount.toStringAsFixed(2)}',
                                  style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                                ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}

class _SummaryRow extends StatelessWidget {
  final String label;
  final String value;
  const _SummaryRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Colors.grey.shade600)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}
