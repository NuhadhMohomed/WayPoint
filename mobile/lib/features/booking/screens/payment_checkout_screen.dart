import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/transit_badge.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../../../core/widgets/waypoint_card.dart';
import '../models/booking_models.dart';
import '../services/booking_api_service.dart';
import '../widgets/hold_countdown_bar.dart';
import 'ticket_wallet_screen.dart';

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
  late int _remainingSeconds;
  Timer? _countdownTimer;
  bool _isHoldExpired = false;

  PaymentSandboxCard _selectedPreset = PaymentSandboxCard.successCard;
  final _cardNumberController = TextEditingController();
  final _expiryController = TextEditingController();
  final _cvvController = TextEditingController();
  final _nameController = TextEditingController();
  final _promoController = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  bool _promoApplied = false;
  bool _includeInsurance = false;
  bool _isProcessing = false;
  final _currencyFormatter = NumberFormat.currency(
    locale: 'en_LK',
    symbol: 'Rs. ',
    decimalDigits: 2,
  );

  double get _discountAmount => _promoApplied ? (widget.holdInfo.subtotal * 0.20) : 0.0;
  double get _insuranceAmount => _includeInsurance ? (widget.holdInfo.seatCount * 150.0) : 0.0;
  double get _totalPayable => (widget.holdInfo.subtotal - _discountAmount) + widget.holdInfo.serviceFee + _insuranceAmount;

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
    _promoController.dispose();
    super.dispose();
  }

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

  void _populateCardFields(PaymentSandboxCard card) {
    _cardNumberController.text = card.cardNumber;
    _expiryController.text = card.expiryDate;
    _cvvController.text = card.cvv;
    _nameController.text = card.cardholderName;
  }

  void _showHoldExpiredDialog() {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        backgroundColor: isDark ? const Color(0xFF1E293B) : Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        icon: const Icon(
          Icons.alarm_off_rounded,
          color: AppTheme.errorColor,
          size: 48,
        ),
        title: const Text(
          'Seat Hold Expired',
          style: TextStyle(
            fontWeight: FontWeight.bold,
            fontSize: 20,
          ),
        ),
        content: const Text(
          'Your 10-minute temporary seat reservation has ended. Seats have been returned to public inventory.',
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

    final apiService = BookingApiService();
    final chargeResult = await apiService.processSandboxCharge(
      cardNumber: _cardNumberController.text,
      amount: _totalPayable,
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
          content: Text(
            'Gateway Timeout (HTTP 504): Simulated network delay. Please retry transaction.',
            style: TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
          ),
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: AppTheme.errorColor,
          behavior: SnackBarBehavior.floating,
          content: Text(
            chargeResult['declineReason']?.toString() ?? 'Card Declined by Bank. Try another preset.',
            style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
          ),
        ),
      );
    }
  }

  void _showSuccessDialog(String reference, String qrPayload) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => Dialog(
        backgroundColor: isDark ? const Color(0xFF131B2E) : Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 64,
                height: 64,
                decoration: BoxDecoration(
                  color: AppTheme.primaryColor.withValues(alpha: 0.2),
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.check_circle_rounded,
                  color: AppTheme.primaryColor,
                  size: 40,
                ),
              ),
              const SizedBox(height: 16),
              const Text(
                'Payment Confirmed!',
                style: TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Your seats are confirmed and your digital boarding pass is ready in your wallet.',
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 13, color: isDark ? Colors.grey[400] : Colors.grey[600]),
              ),
              const SizedBox(height: 20),
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Column(
                  children: [
                    _buildSummaryRow('Booking Reference', reference, isHighlight: true),
                    const Divider(height: 16),
                    _buildSummaryRow('Service', widget.holdInfo.serviceCode),
                    const SizedBox(height: 6),
                    _buildSummaryRow('Reserved Seats', widget.holdInfo.seatNumbers.join(', ')),
                    const SizedBox(height: 6),
                    _buildSummaryRow('Total Paid', _currencyFormatter.format(_totalPayable)),
                  ],
                ),
              ),
              const SizedBox(height: 24),
              WayPointButton(
                text: 'View Ticket in Wallet',
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
                            passengerName: _nameController.text.isNotEmpty ? _nameController.text : 'Nimal Silva',
                            totalFare: _totalPayable,
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
      ),
    );
  }

  void _applyPromoCode() {
    final code = _promoController.text.trim().toUpperCase();
    if (code == 'WAYPOINT20') {
      HapticFeedback.mediumImpact();
      setState(() {
        _promoApplied = true;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Promo Code WAYPOINT20 Applied! 20% discount added.'),
          backgroundColor: AppTheme.primaryColor,
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Invalid promo code. Try WAYPOINT20.'),
          backgroundColor: AppTheme.errorColor,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: const Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.lock_outline_rounded, color: AppTheme.primaryColor, size: 18),
            SizedBox(width: 8),
            Text('Secure Checkout'),
          ],
        ),
        elevation: 0,
      ),
      body: Column(
        children: [
          HoldCountdownBar(
            remainingSeconds: _remainingSeconds,
            totalSeconds: widget.holdInfo.totalHoldSeconds,
            isExpired: _isHoldExpired,
          ),
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _buildJourneyCard(),
                    const SizedBox(height: 16),
                    _buildStopTimeline(),
                    const SizedBox(height: 16),
                    _buildFareCard(),
                    const SizedBox(height: 16),
                    _buildPromoCodeCard(),
                    const SizedBox(height: 20),
                    _buildSandboxPresetSelector(),
                    const SizedBox(height: 16),
                    _buildCardDetailsForm(),
                    const SizedBox(height: 16),
                    _buildSecurityNote(),
                    const SizedBox(height: 90),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
      bottomSheet: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: isDark ? const Color(0xFF131B2E) : Colors.white,
          border: Border(
            top: BorderSide(
              color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
            ),
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
                    Text(
                      'TOTAL PAYABLE',
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.bold,
                        color: isDark ? Colors.grey[400] : Colors.grey[600],
                      ),
                    ),
                    Text(
                      _currencyFormatter.format(_totalPayable),
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.primaryColor,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: WayPointButton(
                  text: _isProcessing ? 'Authorizing...' : 'Pay & Confirm',
                  icon: Icons.shield,
                  isLoading: _isProcessing,
                  onPressed: _isProcessing || _isHoldExpired ? null : _processPayment,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

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
                  color: AppTheme.primaryColor,
                  fontWeight: FontWeight.bold,
                  fontSize: 13,
                  fontFamily: 'monospace',
                ),
              ),
              const TransitBadge(status: TransitStatus.held, customLabel: 'Hold Active'),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            widget.holdInfo.routeTitle,
            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              const Icon(Icons.departure_board, size: 16, color: AppTheme.primaryColor),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  '${widget.holdInfo.originCity}  ➔  ${widget.holdInfo.destinationCity}',
                  style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w500),
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              const Icon(Icons.schedule, size: 16, color: Colors.grey),
              const SizedBox(width: 8),
              Text(
                dateFormat.format(widget.holdInfo.departureTime),
                style: const TextStyle(color: Colors.grey, fontSize: 12),
              ),
            ],
          ),
          const Divider(height: 24),
          Row(
            children: [
              const Text('Reserved Seats: ', style: TextStyle(fontSize: 13, color: Colors.grey)),
              const SizedBox(width: 6),
              Wrap(
                spacing: 6,
                children: widget.holdInfo.seatNumbers.map((seat) {
                  return Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: AppTheme.primaryColor.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(6),
                      border: Border.all(color: AppTheme.primaryColor),
                    ),
                    child: Text(
                      'Seat $seat',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                        color: AppTheme.primaryColor,
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

  Widget _buildStopTimeline() {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return WayPointCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Route Stops & Pickup Timeline',
            style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 14),
          _buildTimelineItem(
            station: widget.holdInfo.originCity,
            detail: 'Platform 4 • Departure Terminal',
            time: '07:30 AM',
            isFirst: true,
            isDark: isDark,
          ),
          _buildTimelineItem(
            station: 'Avissawella Highway Interchange',
            detail: 'Quick passenger boarding stop',
            time: '08:45 AM',
            isDark: isDark,
          ),
          _buildTimelineItem(
            station: 'Ratnapura Bus Terminal',
            detail: '15 min refreshment stop',
            time: '10:15 AM',
            isDark: isDark,
          ),
          _buildTimelineItem(
            station: widget.holdInfo.destinationCity,
            detail: 'Arrival at main terminal',
            time: '01:00 PM',
            isLast: true,
            isDark: isDark,
          ),
        ],
      ),
    );
  }

  Widget _buildTimelineItem({
    required String station,
    required String detail,
    required String time,
    bool isFirst = false,
    bool isLast = false,
    required bool isDark,
  }) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Column(
          children: [
            Container(
              width: 12,
              height: 12,
              decoration: BoxDecoration(
                color: (isFirst || isLast) ? AppTheme.primaryColor : Colors.grey,
                shape: BoxShape.circle,
              ),
            ),
            if (!isLast)
              Container(
                width: 2,
                height: 36,
                color: isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1),
              ),
          ],
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                station,
                style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
              ),
              Text(
                detail,
                style: TextStyle(fontSize: 11, color: isDark ? Colors.grey[400] : Colors.grey[600]),
              ),
            ],
          ),
        ),
        Text(
          time,
          style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold),
        ),
      ],
    );
  }

  Widget _buildFareCard() {
    return WayPointCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Fare Breakdown',
            style: TextStyle(
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
            'Service & Platform Fee',
            _currencyFormatter.format(widget.holdInfo.serviceFee),
          ),
          if (_promoApplied) ...[
            const SizedBox(height: 8),
            _buildSummaryRow(
              'Promo Discount (WAYPOINT20)',
              '- ${_currencyFormatter.format(_discountAmount)}',
              textColor: AppTheme.primaryColor,
            ),
          ],
          if (_includeInsurance) ...[
            const SizedBox(height: 8),
            _buildSummaryRow(
              'Passenger Travel Insurance',
              '+ ${_currencyFormatter.format(_insuranceAmount)}',
            ),
          ],
          const Divider(height: 20),
          _buildSummaryRow(
            'Grand Total (LKR)',
            _currencyFormatter.format(_totalPayable),
            isHighlight: true,
          ),
        ],
      ),
    );
  }

  Widget _buildPromoCodeCard() {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return WayPointCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Promotions & Add-ons',
            style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _promoController,
                  textCapitalization: TextCapitalization.characters,
                  decoration: const InputDecoration(
                    hintText: 'Enter promo (e.g. WAYPOINT20)',
                    contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                    border: OutlineInputBorder(),
                  ),
                ),
              ),
              const SizedBox(width: 10),
              ElevatedButton(
                onPressed: _applyPromoCode,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.primaryColor,
                  foregroundColor: AppTheme.onPrimaryColor,
                ),
                child: const Text('Apply'),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Checkbox(
                value: _includeInsurance,
                activeColor: AppTheme.primaryColor,
                onChanged: (val) {
                  setState(() {
                    _includeInsurance = val ?? false;
                  });
                },
              ),
              Expanded(
                child: GestureDetector(
                  onTap: () {
                    setState(() {
                      _includeInsurance = !_includeInsurance;
                    });
                  },
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Add Sri Lanka Transit Insurance (+Rs. 150/seat)', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
                      Text(
                        'Covers accidental loss, delay refunds & medical coverage',
                        style: TextStyle(fontSize: 11, color: isDark ? Colors.grey[400] : Colors.grey[600]),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

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
                fontSize: 15,
                fontWeight: FontWeight.bold,
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: BoxDecoration(
                color: AppTheme.primaryColor.withValues(alpha: 0.2),
                borderRadius: BorderRadius.circular(4),
              ),
              child: const Text(
                'TEST MODE',
                style: TextStyle(
                  color: AppTheme.primaryColor,
                  fontSize: 10,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 6),
        const Text(
          'Select a test card scenario or simulate immediately:',
          style: TextStyle(fontSize: 12, color: Colors.grey),
        ),
        const SizedBox(height: 12),
        Row(
          children: PaymentSandboxCard.allPresets.map((preset) {
            final isSelected = _selectedPreset == preset;
            Color chipColor;
            switch (preset.expectedOutcome) {
              case SandboxOutcome.success:
                chipColor = AppTheme.primaryColor;
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
                    color: isSelected ? chipColor.withValues(alpha: 0.18) : Colors.transparent,
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                      color: isSelected ? chipColor : Colors.grey.withValues(alpha: 0.3),
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
                        style: const TextStyle(fontSize: 11, fontWeight: FontWeight.bold),
                        textAlign: TextAlign.center,
                      ),
                    ],
                  ),
                ),
              ),
            );
          }).toList(),
        ),
        const SizedBox(height: 12),
        WayPointButton(
          text: 'Simulate Sandbox Payment',
          icon: Icons.play_arrow,
          variant: WayPointButtonVariant.outline,
          isLoading: _isProcessing,
          onPressed: _isProcessing || _isHoldExpired ? null : _processPayment,
        ),
      ],
    );
  }

  Widget _buildCardDetailsForm() {
    return WayPointCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Card Details',
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 14),
          _buildTextFormField(
            controller: _cardNumberController,
            label: 'Card Number',
            hintText: '4000 0000 0000 0001',
            prefixIcon: Icons.credit_card,
            validator: (val) => (val == null || val.isEmpty) ? 'Enter card number' : null,
          ),
          const SizedBox(height: 12),
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
      decoration: InputDecoration(
        labelText: label,
        hintText: hintText,
        prefixIcon: Icon(prefixIcon, size: 18),
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      ),
      validator: validator,
    );
  }

  Widget _buildSecurityNote() {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppTheme.primaryColor.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: AppTheme.primaryColor.withValues(alpha: 0.25)),
      ),
      child: const Row(
        children: [
          Icon(Icons.verified_user_outlined, color: AppTheme.primaryColor, size: 20),
          SizedBox(width: 10),
          Expanded(
            child: Text(
              'Encrypted 256-bit sandbox environment. No actual charge is incurred. Concurrency lock is protected server-side via EF Core DbContextTransaction.',
              style: TextStyle(fontSize: 11, height: 1.4),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSummaryRow(
    String label,
    String value, {
    bool isHighlight = false,
    Color? textColor,
  }) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: TextStyle(
            fontSize: isHighlight ? 14 : 13,
            fontWeight: isHighlight ? FontWeight.bold : FontWeight.normal,
          ),
        ),
        Text(
          value,
          style: TextStyle(
            color: textColor ?? (isHighlight ? AppTheme.primaryColor : null),
            fontSize: isHighlight ? 15 : 13,
            fontWeight: isHighlight ? FontWeight.w800 : FontWeight.w600,
            fontFamily: isHighlight ? 'monospace' : null,
          ),
        ),
      ],
    );
  }
}
