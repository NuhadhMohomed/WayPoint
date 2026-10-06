import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../../../../core/theme/app_theme.dart';
import '../bloc/conductor_scanner_bloc.dart';

class ConductorScannerScreen extends StatefulWidget {
  const ConductorScannerScreen({super.key});

  @override
  State<ConductorScannerScreen> createState() => _ConductorScannerScreenState();
}

class _ConductorScannerScreenState extends State<ConductorScannerScreen> {
  late MobileScannerController _controller;
  bool _isTorchOn = false;
  final _manualInputCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _controller = MobileScannerController(
      detectionSpeed: DetectionSpeed.normal,
      facing: CameraFacing.back,
    );
  }

  @override
  void dispose() {
    _manualInputCtrl.dispose();
    _controller.dispose();
    super.dispose();
  }

  void _showManualEntryDialog(BuildContext blocContext) {
    _manualInputCtrl.clear();
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Manual Ticket Entry', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Enter the 8-character booking reference printed on physical pass or SMS:',
              style: TextStyle(fontSize: 13, color: Colors.grey),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _manualInputCtrl,
              autofocus: true,
              textCapitalization: TextCapitalization.characters,
              decoration: const InputDecoration(
                hintText: 'e.g. TK-12345',
                prefixIcon: Icon(Icons.confirmation_number_outlined),
                border: OutlineInputBorder(borderRadius: BorderRadius.all(Radius.circular(10))),
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: AppTheme.primaryColor,
              foregroundColor: AppTheme.onPrimaryColor,
            ),
            onPressed: () {
              final ref = _manualInputCtrl.text.trim();
              if (ref.isNotEmpty) {
                Navigator.pop(ctx);
                HapticFeedback.mediumImpact();
                blocContext.read<ConductorScannerBloc>().add(ScanQrCode(ref));
              }
            },
            child: const Text('Verify Ticket', style: TextStyle(fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => ConductorScannerBloc(),
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Boarding Pass Scanner', style: TextStyle(fontWeight: FontWeight.bold)),
          elevation: 0,
          actions: [
            IconButton(
              icon: Icon(_isTorchOn ? Icons.flash_on : Icons.flash_off),
              tooltip: 'Toggle Flashlight',
              onPressed: () async {
                await _controller.toggleTorch();
                setState(() => _isTorchOn = !_isTorchOn);
              },
            ),
            IconButton(
              icon: const Icon(Icons.flip_camera_ios_outlined),
              tooltip: 'Switch Camera',
              onPressed: () => _controller.switchCamera(),
            ),
          ],
        ),
        body: BlocBuilder<ConductorScannerBloc, ConductorScannerState>(
          builder: (context, state) {
            return Stack(
              children: [
                // Camera viewfinder
                MobileScanner(
                  controller: _controller,
                  onDetect: (capture) {
                    final barcodes = capture.barcodes;
                    if (barcodes.isNotEmpty && state is ScannerReady) {
                      final payload = barcodes.first.rawValue ?? '';
                      if (payload.isNotEmpty) {
                        HapticFeedback.lightImpact();
                        context.read<ConductorScannerBloc>().add(ScanQrCode(payload));
                      }
                    }
                  },
                ),

                // Scan target overlay with corner reticles
                if (state is ScannerReady)
                  SafeArea(
                    child: Center(
                      child: SingleChildScrollView(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            const SizedBox(height: 12),
                            Container(
                              margin: const EdgeInsets.symmetric(horizontal: 24),
                              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                              decoration: BoxDecoration(
                                color: Colors.black.withValues(alpha: 0.65),
                                borderRadius: BorderRadius.circular(20),
                              ),
                              child: const Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Icon(Icons.qr_code_2, color: Color(0xFF32DE84), size: 20),
                                  SizedBox(width: 8),
                                  Text(
                                    'Align digital or printed QR within frame',
                                    style: TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w500),
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(height: 16),
                            Center(
                              child: Container(
                                width: 220,
                                height: 220,
                                decoration: BoxDecoration(
                                  border: Border.all(color: const Color(0xFF32DE84), width: 2.5),
                                  borderRadius: BorderRadius.circular(20),
                                  boxShadow: [
                                    BoxShadow(
                                      color: const Color(0xFF32DE84).withValues(alpha: 0.2),
                                      blurRadius: 20,
                                      spreadRadius: 2,
                                    ),
                                  ],
                                ),
                              ),
                            ),
                            const SizedBox(height: 16),
                            // Manual reference entry fallback button
                            Padding(
                              padding: const EdgeInsets.only(bottom: 16.0),
                              child: ElevatedButton.icon(
                                onPressed: () => _showManualEntryDialog(context),
                                icon: const Icon(Icons.keyboard_outlined, size: 20),
                                label: const Text('Enter Booking Ref Manually'),
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: Colors.black.withValues(alpha: 0.75),
                                  foregroundColor: Colors.white,
                                  side: const BorderSide(color: Color(0xFF32DE84), width: 1.5),
                                  padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),

                // Loading overlay
                if (state is VerifyingTicket)
                  Container(
                    color: Colors.black.withValues(alpha: 0.75),
                    child: const Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          CircularProgressIndicator(color: Color(0xFF32DE84)),
                          SizedBox(height: 18),
                          Text(
                            'Verifying cryptographic transit pass...',
                            style: TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
                          ),
                        ],
                      ),
                    ),
                  ),

                // Success overlay
                if (state is TicketValid)
                  _ResultOverlay(
                    color: const Color(0xFF005312),
                    accentColor: const Color(0xFF32DE84),
                    icon: Icons.check_circle_rounded,
                    title: 'BOARDING CONFIRMED',
                    passengerName: state.passengerName,
                    seatsText: 'Seat(s): ${state.seatNumbers.join(', ')}',
                    onReset: () {
                      HapticFeedback.selectionClick();
                      context.read<ConductorScannerBloc>().add(const ResetScanner());
                    },
                  ),

                // Failure overlay
                if (state is TicketInvalid)
                  _ResultOverlay(
                    color: const Color(0xFF7F1D1D),
                    accentColor: const Color(0xFFF87171),
                    icon: Icons.cancel_rounded,
                    title: 'INVALID TICKET',
                    passengerName: 'Verification Denied',
                    seatsText: state.reason,
                    isFailure: true,
                    onReset: () {
                      HapticFeedback.selectionClick();
                      context.read<ConductorScannerBloc>().add(const ResetScanner());
                    },
                  ),
              ],
            );
          },
        ),
      ),
    );
  }
}

/// Full-screen result overlay shown after QR verification.
class _ResultOverlay extends StatelessWidget {
  final Color color;
  final Color accentColor;
  final IconData icon;
  final String title;
  final String passengerName;
  final String seatsText;
  final bool isFailure;
  final VoidCallback onReset;

  const _ResultOverlay({
    required this.color,
    required this.accentColor,
    required this.icon,
    required this.title,
    required this.passengerName,
    required this.seatsText,
    this.isFailure = false,
    required this.onReset,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      color: color.withValues(alpha: 0.95),
      padding: const EdgeInsets.all(32),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: accentColor.withValues(alpha: 0.2),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, color: accentColor, size: 72),
            ),
            const SizedBox(height: 24),
            Text(
              title,
              style: TextStyle(
                color: accentColor,
                fontSize: 24,
                fontWeight: FontWeight.w900,
                letterSpacing: 1.2,
              ),
            ),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.black.withValues(alpha: 0.3),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                children: [
                  Text(
                    passengerName,
                    style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    seatsText,
                    style: TextStyle(
                      color: isFailure ? Colors.red[200] : const Color(0xFF32DE84),
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                    ),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            ),
            const SizedBox(height: 36),
            SizedBox(
              width: double.infinity,
              height: 52,
              child: ElevatedButton.icon(
                onPressed: onReset,
                icon: const Icon(Icons.qr_code_scanner, size: 22),
                label: const Text('Scan Next Ticket', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                style: ElevatedButton.styleFrom(
                  backgroundColor: isFailure ? Colors.white : const Color(0xFF32DE84),
                  foregroundColor: isFailure ? Colors.red[900] : const Color(0xFF042611),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  elevation: 4,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
