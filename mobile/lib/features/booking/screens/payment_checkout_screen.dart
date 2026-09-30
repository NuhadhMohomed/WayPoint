import 'dart:async';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/transit_badge.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../../../core/widgets/waypoint_card.dart';
import '../models/booking_models.dart';
import '../services/booking_api_service.dart';
import '../widgets/hold_countdown_bar.dart';
import 'ticket_wallet_screen.dart';

/// MOB-06: Payment Sandbox Checkout & Hold Bar
/// Stitch Screen ID: 01076854fa0e41d299d8fc02ab224ad1
/// Component 3: Booking, Ticketing & Passenger Options (Mithila)
class PaymentCheckoutScreen extends StatefulWidget {
  final SeatHoldInfo holdInfo;
  final VoidCallback? onBookingSuccess;

  const PaymentCheckoutScreen({
    super.key,
    required this.holdInfo,
    this.onBookingSuccess,
  });

  @override
  State<PaymentCheckoutScreen> createState() => _PaymentCheckoutScreenState();
}

class _PaymentCheckoutScreenState extends State<PaymentCheckoutScreen> {
  // Timer & Expiration State
  late int _remainingSeconds;
  Timer? _countdownTimer;
  bool _isHoldExpired = false;

  // Form & Sandbox Payment State
  PaymentSandboxCard _selectedPreset = PaymentSandboxCard.successCard;
  final _cardNumberController = TextEditingController();
  final _expiryController = TextEditingController();
  final _cvvController = TextEditingController();
  final _nameController = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  // Execution State
  bool _isProcessing = false;
  final _currencyFormatter = NumberFormat.currency(
    locale: 'en_LK',
    symbol: 'Rs. ',
    decimalDigits: 2,
  );

  @override
  void initState() {
    super.initState();
    _remainingSeconds = widget.holdInfo.remainingSeconds;
    if (_remainingSeconds <= 0) {
      _isHoldExpired = true;
    } else {
      _startCountdownTimer();
    }
    _populateCardFields(_selectedPreset);
  }

  @override
  void dispose() {
    _countdownTimer?.cancel();
    _cardNumberController.dispose();
    _expiryController.dispose();
    _cvvController.dispose();
    _nameController.dispose();
    super.dispose();
  }

  /// Starts the second-by-second countdown timer for the 10-minute hold window.
  void _startCountdownTimer() {
    _countdownTimer?.cancel();
    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) return;
      if (_remainingSeconds > 0) {
        setState(() {
          _remainingSeconds--;
        });
      } else {
        timer.cancel();
        setState(() {
          _isHoldExpired = true;
        });
        _showHoldExpiredDialog();
      }
    });
  }

  /// Fills the checkout text fields with data from the chosen test card preset.
  void _populateCardFields(PaymentSandboxCard card) {
    _cardNumberController.text = card.cardNumber;
    _expiryController.text = card.expiryDate;
    _cvvController.text = card.cvv;
    _nameController.text = card.cardholderName;
  }

  /// Shows alert modal when the 10-minute server seat hold expires.
  void _showHoldExpiredDialog() {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF1E293B),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        icon: const Icon(
          Icons.alarm_off_rounded,
          color: AppTheme.errorColor,
          size: 48,
        ),
        title: const Text(
          'Seat Hold Expired',
          style: TextStyle(
            color: Colors.white,
            fontWeight: FontWeight.bold,
            fontSize: 20,
          ),
        ),
        content: const Text(
          'Your 10-minute temporary seat reservation has ended. The selected seats have been released back to public inventory to prevent lockouts.',
          style: TextStyle(color: Color(0xFF94A3B8), fontSize: 14),
          textAlign: TextAlign.center,
        ),
        actions: [
          WayPointButton(
            text: 'Return to Seat Picker',
            onPressed: () {
              Navigator.of(ctx).pop();
              Navigator.of(context).pop();
            },
          ),
        ],
      ),
    );
  }

  /// Simulates transactional payment gateway authorization and booking conversion.
  Future<void> _processPayment() async {
    if (_isHoldExpired) {
      _showHoldExpiredDialog();
      return;
    }

    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _isProcessing = true;
    });

    // 1. Process payment charge via live API / sandbox gateway
    final apiService = BookingApiService();
    final chargeResult = await apiService.processSandboxCharge(
      cardNumber: _cardNumberController.text,
      amount: widget.holdInfo.totalAmount,
      cardholderName: _nameController.text,
    );

    if (!mounted) return;

    setState(() {
      _isProcessing = false;
    });

    final isSuccess = chargeResult['isSuccess'] == true;
    final gatewayStatus = chargeResult['gatewayStatus']?.toString() ?? 'Error';

    if (isSuccess) {
      _countdownTimer?.cancel();
      final txnId = chargeResult['transactionId']?.toString() ?? 'TXN-CONFIRMED';
      
      // 2. Execute transactional atomic checkout on backend API
      final confirmation = await apiService.executeCheckout(
        holdId: widget.holdInfo.holdId,
        paymentTxnId: txnId,
        passengerName: _nameController.text.isNotEmpty ? _nameController.text : 'Nimal Silva',
      );

      if (!mounted) return;
      _showSuccessDialog(confirmation.bookingReference, confirmation.ticketQrPayload);
    } else if (gatewayStatus == 'Timeout') {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          backgroundColor: AppTheme.secondaryColor,
          behavior: SnackBarBehavior.floating,
          content: Row(
            children: [
              Icon(Icons.wifi_off_rounded, color: Color(0xFF191C1D)),
              SizedBox(width: 12),
              Expanded(
                child: Text(
                  'Gateway Timeout (HTTP 504): Simulated network delay. Please retry transaction.',
                  style: TextStyle(color: Color(0xFF191C1D), fontSize: 13, fontWeight: FontWeight.w600),
                ),
              ),
            ],
          ),
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: AppTheme.errorColor,
          behavior: SnackBarBehavior.floating,
          content: Row(
            children: [
              const Icon(Icons.error_outline, color: Colors.white),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  chargeResult['message']?.toString() ??
                      'Payment Declined: Simulated Insufficient Funds (HTTP 402). Your hold is still active, please try another card.',
                  style: const TextStyle(color: Colors.white, fontSize: 13),
                ),
              ),
            ],
          ),
        ),
      );
    }
  }

  /// Displays booking success confirmation bottom modal with ticket reference.
  void _showSuccessDialog(String reference, String qrPayload) {
    showModalBottomSheet(
      context: context,
      isDismissible: false,
      enableDrag: false,
      isScrollControlled: true,
      backgroundColor: const Color(0xFF0F172A),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 64,
              height: 64,
              decoration: const BoxDecoration(
                color: Color(0xFF05230F),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.check_circle_rounded,
                color: Color(0xFF22C55E),
                size: 40,
              ),
            ),
            const SizedBox(height: 16),
            const Text(
              'Payment Confirmed!',
              style: TextStyle(
                color: Colors.white,
                fontSize: 22,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            const Text(
              'Your seats have been booked and digital tickets issued.',
              style: TextStyle(color: Color(0xFF94A3B8), fontSize: 14),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 20),
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFF1E293B),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFF334155)),
              ),
              child: Column(
                children: [
                  _buildSummaryRow('Booking Reference', reference, isHighlight: true),
                  const Divider(color: Color(0xFF334155), height: 20),
                  _buildSummaryRow('Service', widget.holdInfo.serviceCode),
                  const SizedBox(height: 8),
                  _buildSummaryRow('Reserved Seats', widget.holdInfo.seatNumbers.join(', ')),
                  const SizedBox(height: 8),
                  _buildSummaryRow('Total Paid', _currencyFormatter.format(widget.holdInfo.totalAmount)),
                ],
              ),
            ),
            const SizedBox(height: 24),
            WayPointButton(
              text: 'Go to Ticket Wallet (MOB-07)',
              icon: Icons.confirmation_number_outlined,
              onPressed: () {
                Navigator.of(ctx).pop();
                Navigator.of(context).pushReplacement(
                  MaterialPageRoute(
                    builder: (_) => TicketWalletScreen(
                      initialTickets: [
                        DigitalTicketPass(
                          ticketId: 'TCK-${DateTime.now().millisecondsSinceEpoch.toString().substring(8)}',
                          bookingReference: reference,
                          serviceCode: widget.holdInfo.serviceCode,
                          routeTitle: widget.holdInfo.routeTitle,
                          originCity: widget.holdInfo.originCity,
                          destinationCity: widget.holdInfo.destinationCity,
                          boardingPointName: 'Platform 3, Makumbura Highway Terminal',
                          departureTime: widget.holdInfo.departureTime,
                          arrivalTime: widget.holdInfo.arrivalTime,
                          busRegistration: 'NC-8890',
                          busClass: 'SuperLuxury Express',
                          seatNumbers: widget.holdInfo.seatNumbers,
                          passengerName: 'Nimal Silva',
                          totalFare: widget.holdInfo.totalAmount,
                          isBoarded: false,
                          qrCodePayload: qrPayload,
                          issuedAt: DateTime.now(),
                        ),
                        DigitalTicketPass.sampleColomboToKandy(),
                      ],
                    ),
                  ),
                );
                if (widget.onBookingSuccess != null) {
                  widget.onBookingSuccess!();
                }
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A), // Slate 900
      appBar: AppBar(
        backgroundColor: const Color(0xFF0F172A),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new, color: Colors.white, size: 20),
          onPressed: () => Navigator.of(context).pop(),
        ),
        title: const Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.lock_outline_rounded, color: Color(0xFF22C55E), size: 18),
            SizedBox(width: 8),
            Text(
              'Secure Checkout',
              style: TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
        actions: const [
          Padding(
            padding: EdgeInsets.only(right: 16),
            child: Center(
              child: TransitBadge(
                status: TransitStatus.luxury,
                customLabel: 'MOB-06',
              ),
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          // Sticky Top 10-minute hold progress bar
          HoldCountdownBar(
            remainingSeconds: _remainingSeconds,
            totalSeconds: widget.holdInfo.totalHoldSeconds,
            isExpired: _isHoldExpired,
          ),

          // Scrollable Checkout Body
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // 1. Journey Summary Card
                    _buildJourneyCard(),
                    const SizedBox(height: 16),

                    // 2. Fare Breakdown Card
                    _buildFareCard(),
                    const SizedBox(height: 20),

                    // 3. Payment Sandbox Presets
                    _buildSandboxPresetSelector(),
                    const SizedBox(height: 16),

                    // 4. Card Details Input Form
                    _buildCardDetailsForm(),
                    const SizedBox(height: 20),

                    // 5. Security & Gateway Note
                    _buildSecurityNote(),
                    const SizedBox(height: 80), // Padding for sticky bottom button
                  ],
                ),
              ),
            ),
          ),
        ],
      ),

      // Sticky Bottom Checkout Button
      bottomSheet: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(
          color: Color(0xFF1E293B),
          border: Border(
            top: BorderSide(color: Color(0xFF334155), width: 1),
          ),
        ),
        child: SafeArea(
          child: Row(
            children: [
              Expanded(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'TOTAL PAYABLE',
                      style: TextStyle(
                        color: Color(0xFF94A3B8),
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 0.5,
                      ),
                    ),
                    Text(
                      _currencyFormatter.format(widget.holdInfo.totalAmount),
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 20,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                flex: 2,
                child: WayPointButton(
                  text: _isHoldExpired ? 'Hold Expired' : 'Pay & Confirm',
                  icon: _isHoldExpired ? Icons.lock_clock : Icons.credit_card,
                  isLoading: _isProcessing,
                  onPressed: (_isHoldExpired || _isProcessing) ? null : _processPayment,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  /// Builds the transit corridor trip card with departure, destination, and seat badges.
  Widget _buildJourneyCard() {
    final dateFormat = DateFormat('EEE, dd MMM yyyy • hh:mm a');

    return WayPointCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                widget.holdInfo.serviceCode,
                style: const TextStyle(
                  color: AppTheme.secondaryColor,
                  fontWeight: FontWeight.bold,
                  fontSize: 13,
                  fontFamily: 'monospace',
                ),
              ),
              const TransitBadge(
                status: TransitStatus.held,
                customLabel: 'Hold Active',
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            widget.holdInfo.routeTitle,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 16,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              const Icon(Icons.departure_board, color: Color(0xFF94A3B8), size: 16),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  '${widget.holdInfo.originCity}  ➔  ${widget.holdInfo.destinationCity}',
                  style: const TextStyle(color: Color(0xFFE2E8F0), fontSize: 13),
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              const Icon(Icons.schedule, color: Color(0xFF94A3B8), size: 16),
              const SizedBox(width: 8),
              Text(
                dateFormat.format(widget.holdInfo.departureTime),
                style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
              ),
            ],
          ),
          const Divider(color: Color(0xFF334155), height: 24),
          Row(
            children: [
              const Text(
                'Reserved Seats: ',
                style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
              ),
              const SizedBox(width: 6),
              Wrap(
                spacing: 6,
                children: widget.holdInfo.seatNumbers.map((seat) {
                  return Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: AppTheme.primaryColor.withOpacity(0.2),
                      borderRadius: BorderRadius.circular(6),
                      border: Border.all(color: AppTheme.primaryColor),
                    ),
                    child: Text(
                      'Seat $seat',
                      style: const TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                      ),
                    ),
                  );
                }).toList(),
              ),
            ],
          ),
        ],
      ),
    );
  }

  /// Builds the itemized fare breakdown table according to Sri Lankan Rupee pricing.
  Widget _buildFareCard() {
    return WayPointCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Fare Summary',
            style: TextStyle(
              color: Colors.white,
              fontSize: 15,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 12),
          _buildSummaryRow(
            'Seat Fare (${widget.holdInfo.seatCount} × ${_currencyFormatter.format(widget.holdInfo.farePerSeat)})',
            _currencyFormatter.format(widget.holdInfo.subtotal),
          ),
          const SizedBox(height: 8),
          _buildSummaryRow(
            'Service & Processing Fee',
            _currencyFormatter.format(widget.holdInfo.serviceFee),
          ),
          const Divider(color: Color(0xFF334155), height: 20),
          _buildSummaryRow(
            'Grand Total (LKR)',
            _currencyFormatter.format(widget.holdInfo.totalAmount),
            isHighlight: true,
          ),
        ],
      ),
    );
  }

  /// Builds the interactive preset selector for sandbox test scenarios.
  Widget _buildSandboxPresetSelector() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'Payment Sandbox Presets',
              style: TextStyle(
                color: Colors.white,
                fontSize: 15,
                fontWeight: FontWeight.bold,
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: BoxDecoration(
                color: const Color(0xFF334155),
                borderRadius: BorderRadius.circular(4),
              ),
              child: const Text(
                'TEST MODE',
                style: TextStyle(color: Color(0xFF94A3B8), fontSize: 10, fontWeight: FontWeight.bold),
              ),
            ),
          ],
        ),
        const SizedBox(height: 6),
        const Text(
          'Select a mock card scenario to test transactional responses:',
          style: TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
        ),
        const SizedBox(height: 12),
        Row(
          children: PaymentSandboxCard.allPresets.map((preset) {
            final isSelected = _selectedPreset == preset;
            Color chipColor;
            switch (preset.expectedOutcome) {
              case SandboxOutcome.success:
                chipColor = const Color(0xFF22C55E);
                break;
              case SandboxOutcome.declined:
                chipColor = AppTheme.errorColor;
                break;
              case SandboxOutcome.timeout:
                chipColor = AppTheme.secondaryColor;
                break;
            }

            return Expanded(
              child: GestureDetector(
                onTap: () {
                  setState(() {
                    _selectedPreset = preset;
                    _populateCardFields(preset);
                  });
                },
                child: Container(
                  margin: const EdgeInsets.symmetric(horizontal: 4),
                  padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 8),
                  decoration: BoxDecoration(
                    color: isSelected ? chipColor.withOpacity(0.18) : const Color(0xFF1E293B),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                      color: isSelected ? chipColor : const Color(0xFF334155),
                      width: isSelected ? 1.8 : 1.0,
                    ),
                  ),
                  child: Column(
                    children: [
                      Icon(
                        preset.expectedOutcome == SandboxOutcome.success
                            ? Icons.check_circle_outline
                            : preset.expectedOutcome == SandboxOutcome.declined
                                ? Icons.cancel_outlined
                                : Icons.timer_outlined,
                        color: chipColor,
                        size: 20,
                      ),
                      const SizedBox(height: 6),
                      Text(
                        preset.name,
                        style: TextStyle(
                          color: isSelected ? Colors.white : const Color(0xFF94A3B8),
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                        ),
                        textAlign: TextAlign.center,
                      ),
                    ],
                  ),
                ),
              ),
            );
          }).toList(),
        ),
        const SizedBox(height: 8),
        Container(
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: const Color(0xFF1E293B),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Row(
            children: [
              const Icon(Icons.info_outline, color: Color(0xFF94A3B8), size: 16),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  _selectedPreset.description,
                  style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 11),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  /// Builds the manual credit card input form with visual feedback.
  Widget _buildCardDetailsForm() {
    return WayPointCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Card Details',
            style: TextStyle(
              color: Colors.white,
              fontSize: 15,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 14),

          // Card Number Field
          _buildTextFormField(
            controller: _cardNumberController,
            label: 'Card Number',
            hintText: '4000 0000 0000 0001',
            prefixIcon: Icons.credit_card,
            validator: (val) => (val == null || val.isEmpty) ? 'Enter card number' : null,
          ),
          const SizedBox(height: 12),

          // Expiry and CVV in a row
          Row(
            children: [
              Expanded(
                child: _buildTextFormField(
                  controller: _expiryController,
                  label: 'Expiry Date',
                  hintText: 'MM/YY',
                  prefixIcon: Icons.calendar_today,
                  validator: (val) => (val == null || val.isEmpty) ? 'Required' : null,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _buildTextFormField(
                  controller: _cvvController,
                  label: 'CVV',
                  hintText: '123',
                  prefixIcon: Icons.lock_outline,
                  obscureText: true,
                  validator: (val) => (val == null || val.length < 3) ? '3 digits' : null,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),

          // Cardholder Name Field
          _buildTextFormField(
            controller: _nameController,
            label: 'Cardholder Name',
            hintText: 'NIMAL SILVA',
            prefixIcon: Icons.person_outline,
            validator: (val) => (val == null || val.isEmpty) ? 'Enter cardholder name' : null,
          ),
        ],
      ),
    );
  }

  Widget _buildTextFormField({
    required TextEditingController controller,
    required String label,
    required String hintText,
    required IconData prefixIcon,
    bool obscureText = false,
    String? Function(String?)? validator,
  }) {
    return TextFormField(
      controller: controller,
      obscureText: obscureText,
      style: const TextStyle(color: Colors.white, fontSize: 14),
      decoration: InputDecoration(
        labelText: label,
        labelStyle: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
        hintText: hintText,
        hintStyle: const TextStyle(color: Color(0xFF64748B), fontSize: 13),
        prefixIcon: Icon(prefixIcon, color: const Color(0xFF94A3B8), size: 18),
        filled: true,
        fillColor: const Color(0xFF0F172A),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: Color(0xFF334155)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: Color(0xFF334155)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppTheme.primaryColor, width: 1.5),
        ),
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      ),
      validator: validator,
    );
  }

  Widget _buildSecurityNote() {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFF1E293B),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: const Color(0xFF334155)),
      ),
      child: const Row(
        children: [
          Icon(Icons.verified_user_outlined, color: Color(0xFF22C55E), size: 20),
          SizedBox(width: 10),
          Expanded(
            child: Text(
              'Encrypted 256-bit sandbox simulation. No real credit card charges are made. Concurrency lock is protected server-side via IDbContextTransaction.',
              style: TextStyle(color: Color(0xFF94A3B8), fontSize: 11, height: 1.4),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSummaryRow(String label, String value, {bool isHighlight = false}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: TextStyle(
            color: isHighlight ? Colors.white : const Color(0xFF94A3B8),
            fontSize: isHighlight ? 14 : 13,
            fontWeight: isHighlight ? FontWeight.bold : FontWeight.normal,
          ),
        ),
        Text(
          value,
          style: TextStyle(
            color: isHighlight ? AppTheme.secondaryColor : Colors.white,
            fontSize: isHighlight ? 15 : 13,
            fontWeight: isHighlight ? FontWeight.w800 : FontWeight.w600,
            fontFamily: isHighlight ? 'monospace' : null,
          ),
        ),
      ],
    );
  }
}
