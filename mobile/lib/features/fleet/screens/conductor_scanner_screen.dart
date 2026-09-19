import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../bloc/conductor_scanner_bloc.dart';

/// MOB-10: Conductor QR Boarding Scanner.
///
/// Full-screen camera viewfinder. On QR detection:
/// - Success → full green overlay with "BOARDED", passenger name, seat numbers.
/// - Failure → full red overlay with "INVALID TICKET" and reason.
/// - "Scan Next" resets the scanner for the next passenger.
class ConductorScannerScreen extends StatefulWidget {
  const ConductorScannerScreen({super.key});

  @override
  State<ConductorScannerScreen> createState() => _ConductorScannerScreenState();
}

class _ConductorScannerScreenState extends State<ConductorScannerScreen> {
  late MobileScannerController _controller;

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
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => ConductorScannerBloc(),
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Scan Boarding Pass'),
          backgroundColor: const Color(0xFF0056D2),
          foregroundColor: Colors.white,
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
                        context.read<ConductorScannerBloc>().add(ScanQrCode(payload));
                      }
                    }
                  },
                ),

                // Scan target overlay
                if (state is ScannerReady)
                  Center(
                    child: Container(
                      width: 250,
                      height: 250,
                      decoration: BoxDecoration(
                        border: Border.all(color: Colors.white.withValues(alpha: 0.6), width: 3),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: const Column(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [
                          Padding(
                            padding: EdgeInsets.only(bottom: 12),
                            child: Text('Point at QR code', style: TextStyle(color: Colors.white, fontSize: 14)),
                          ),
                        ],
                      ),
                    ),
                  ),

                // Loading overlay
                if (state is VerifyingTicket)
                  Container(
                    color: Colors.black.withValues(alpha: 0.6),
                    child: const Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          CircularProgressIndicator(color: Colors.white),
                          SizedBox(height: 16),
                          Text('Verifying ticket...', style: TextStyle(color: Colors.white, fontSize: 16)),
                        ],
                      ),
                    ),
                  ),

                // Success overlay (green)
                if (state is TicketValid)
                  _ResultOverlay(
                    color: Colors.green,
                    icon: Icons.check_circle,
                    title: 'BOARDED',
                    details: [
                      state.passengerName,
                      'Seats: ${state.seatNumbers.join(', ')}',
                    ],
                    onReset: () => context.read<ConductorScannerBloc>().add(const ResetScanner()),
                  ),

                // Failure overlay (red)
                if (state is TicketInvalid)
                  _ResultOverlay(
                    color: Colors.red,
                    icon: Icons.error,
                    title: 'INVALID TICKET',
                    details: [state.reason],
                    onReset: () => context.read<ConductorScannerBloc>().add(const ResetScanner()),
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
  final IconData icon;
  final String title;
  final List<String> details;
  final VoidCallback onReset;

  const _ResultOverlay({
    required this.color,
    required this.icon,
    required this.title,
    required this.details,
    required this.onReset,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      color: color.withValues(alpha: 0.92),
      padding: const EdgeInsets.all(32),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, color: Colors.white, size: 80),
            const SizedBox(height: 24),
            Text(title, style: const TextStyle(color: Colors.white, fontSize: 32, fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            ...details.map((d) => Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Text(d, style: const TextStyle(color: Colors.white, fontSize: 20), textAlign: TextAlign.center),
                )),
            const SizedBox(height: 48),
            ElevatedButton.icon(
              onPressed: onReset,
              icon: const Icon(Icons.qr_code_scanner),
              label: const Text('Scan Next Ticket', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.white,
                foregroundColor: color,
                padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 16),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
