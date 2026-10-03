import 'package:flutter/material.dart';
import 'core/theme/app_theme.dart';

import 'features/auth/screens/passenger_auth_screen.dart';
import 'features/booking/models/booking_models.dart';
import 'features/booking/screens/booking_history_screen.dart';
import 'features/booking/screens/payment_checkout_screen.dart';
import 'features/booking/screens/seat_picker_screen.dart';
import 'features/booking/screens/ticket_wallet_screen.dart';
import 'features/fleet/data/fleet_api_service.dart';
import 'features/fleet/screens/conductor_manifest_screen.dart';
import 'features/fleet/screens/conductor_scanner_screen.dart';
import 'features/fleet/presentation/screens/review_submission_screen.dart';
import 'core/widgets/waypoint_button.dart';
import 'core/widgets/waypoint_card.dart';
import 'core/widgets/transit_badge.dart';
import 'features/journey/models/journey_models.dart';
import 'features/journey/screens/journey_search_screen.dart';
import 'features/journey/screens/journey_comparison_screen.dart';

import 'features/disruption/models/disruption_models.dart';
import 'features/disruption/screens/disruption_alert_screen.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const WayPointApp());
}

class WayPointApp extends StatelessWidget {
  const WayPointApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'WayPoint',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.darkTheme,
      home: const MainNavigationScreen(),
      routes: {
        '/auth': (_) => const PassengerAuthScreen(),
        '/journey-search': (_) => const JourneySearchScreen(),
        '/journey-compare': (_) => JourneyComparisonScreen(
          originCity: 'Colombo',
          destinationCity: 'Ella',
          travelDate: DateTime.now().add(const Duration(days: 1)),
          candidates: [
            JourneyCandidateModel.sampleColomboToEllaDirect(),
            JourneyCandidateModel.sampleConnectingViaKandy(),
          ],
        ),
        '/seat-picker': (_) => SeatPickerScreen(
          serviceId: '0cff3495-ae17-4dda-9f5f-2d030cd016a3',
          apiService: FleetApiService(),
        ),
        '/checkout': (_) => PaymentCheckoutScreen(
          holdInfo: SeatHoldInfo.sampleColomboToElla(),
        ),
        '/wallet': (_) => const TicketWalletScreen(),
        '/booking-history': (_) => const BookingHistoryScreen(),
        '/disruption-alert': (_) => DisruptionAlertScreen(
          disruption: DisruptionAlertModel.sampleColomboToElla(),
        ),
        '/conductor-scanner': (_) => const ConductorScannerScreen(),
        '/conductor-manifest': (_) => const ConductorManifestScreen(),
        '/review-bus': (_) => const ReviewSubmissionScreen(
          entityId: 'BUS-101',
          bookingId: 'WP-BK-COMPLETED-101',
          entityType: 'bus',
          entityName: 'Super Line Luxury ND-8821',
        ),
      },
    );
  }
}

class MainNavigationScreen extends StatefulWidget {
  const MainNavigationScreen({super.key});

  @override
  State<MainNavigationScreen> createState() => _MainNavigationScreenState();
}

class _MainNavigationScreenState extends State<MainNavigationScreen> {
  int _currentIndex = 0; // Default to Journey Search for passenger flow

  @override
  Widget build(BuildContext context) {
    final screens = [
      const JourneySearchScreen(),
      _buildComponent2Hub(context),
      _buildComponent3Hub(context),
      _buildComponent4Hub(context),
    ];

    return Scaffold(
      appBar: AppBar(
        title: const Text('WayPoint Transit'),
        actions: [
          IconButton(
            icon: const Icon(Icons.account_circle_outlined),
            tooltip: 'Passenger Account (MOB-01)',
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const PassengerAuthScreen()),
              );
            },
          ),
        ],
      ),
      body: screens[_currentIndex],
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        onTap: (index) => setState(() => _currentIndex = index),
        type: BottomNavigationBarType.fixed,
        backgroundColor: const Color(0xFF0F172A),
        selectedItemColor: const Color(0xFF818CF8),
        unselectedItemColor: Colors.grey,
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.explore), label: 'Journeys'),
          BottomNavigationBarItem(icon: Icon(Icons.event_seat), label: 'Seats'),
          BottomNavigationBarItem(icon: Icon(Icons.confirmation_number), label: 'Wallet'),
          BottomNavigationBarItem(icon: Icon(Icons.warning_amber_rounded), label: 'Alerts'),
        ],
      ),
    );
  }

  Widget _buildComponent2Hub(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              TransitBadge(status: TransitStatus.express, customLabel: 'Component 2'),
              SizedBox(width: 8),
              Text(
                'Nuhadh',
                style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13, fontWeight: FontWeight.bold),
              ),
            ],
          ),
          const SizedBox(height: 12),
          const Text(
            'Fleet & Operations Hub',
            style: TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 6),
          const Text(
            'Seat map matrix, conductor QR ticket scanner, passenger boarding manifest, and post-trip bus reviews.',
            style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
          ),
          const SizedBox(height: 24),

          // Component 2 Action Card
          WayPointCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'SEAT & CONDUCTOR MODULES',
                      style: TextStyle(color: Color(0xFF38BDF8), fontSize: 11, fontWeight: FontWeight.bold),
                    ),
                    TransitBadge(status: TransitStatus.express, customLabel: 'Fleet Hub'),
                  ],
                ),
                const SizedBox(height: 10),
                const Text(
                  'Super Line Luxury ND-8821',
                  style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 6),
                const Text(
                  'Route: CMB-KAN-001 (Colombo → Kandy Express)',
                  style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                ),
                const SizedBox(height: 16),
                WayPointButton(
                  text: 'Open MOB-05 Interactive Seat Picker',
                  icon: Icons.event_seat,
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => SeatPickerScreen(
                          serviceId: '0cff3495-ae17-4dda-9f5f-2d030cd016a3',
                          apiService: FleetApiService(),
                        ),
                      ),
                    );
                  },
                ),
                const SizedBox(height: 10),
                WayPointButton(
                  text: 'Open MOB-10 Conductor QR Scanner',
                  variant: WayPointButtonVariant.outline,
                  icon: Icons.qr_code_scanner,
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const ConductorScannerScreen(),
                      ),
                    );
                  },
                ),
                const SizedBox(height: 10),
                WayPointButton(
                  text: 'Open MOB-11 Conductor Manifest Roster',
                  variant: WayPointButtonVariant.outline,
                  icon: Icons.list_alt,
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const ConductorManifestScreen(),
                      ),
                    );
                  },
                ),
                const SizedBox(height: 10),
                WayPointButton(
                  text: 'Open SCR-FLEET-100 Review Bus',
                  variant: WayPointButtonVariant.outline,
                  icon: Icons.rate_review_outlined,
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const ReviewSubmissionScreen(
                          entityId: 'BUS-101',
                          bookingId: 'WP-BK-COMPLETED-101',
                          entityType: 'bus',
                          entityName: 'Super Line Luxury ND-8821',
                        ),
                      ),
                    );
                  },
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildComponent3Hub(BuildContext context) {
    final sampleHold = SeatHoldInfo.sampleColomboToElla();

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              TransitBadge(status: TransitStatus.luxury, customLabel: 'Component 3'),
              SizedBox(width: 8),
              Text(
                'Mithila',
                style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13, fontWeight: FontWeight.bold),
              ),
            ],
          ),
          const SizedBox(height: 12),
          const Text(
            'Booking & Ticketing Hub',
            style: TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 6),
          const Text(
            'Test temporary seat holds, payment sandbox checkout, and e-ticket issuance.',
            style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
          ),
          const SizedBox(height: 24),

          // Active Hold Card
          WayPointCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'ACTIVE SEAT HOLD (10 MIN)',
                      style: TextStyle(color: AppTheme.secondaryColor, fontSize: 11, fontWeight: FontWeight.bold),
                    ),
                    TransitBadge(status: TransitStatus.held, customLabel: 'Hold: 10m'),
                  ],
                ),
                const SizedBox(height: 10),
                Text(
                  sampleHold.routeTitle,
                  style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 6),
                Text(
                  'Service: ${sampleHold.serviceCode} • Seats ${sampleHold.seatNumbers.join(", ")}',
                  style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                ),
                const SizedBox(height: 8),
                Text(
                  'Total Payable: Rs. ${sampleHold.totalAmount.toStringAsFixed(2)}',
                  style: const TextStyle(color: Color(0xFF22C55E), fontSize: 15, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 12),
                WayPointButton(
                  text: 'Open MOB-06 Payment Checkout',
                  icon: Icons.credit_card,
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => PaymentCheckoutScreen(
                          holdInfo: SeatHoldInfo.sampleColomboToElla(),
                          onBookingSuccess: () {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(
                                content: Text('Booking confirmed! Ready for MOB-07 Wallet.'),
                                backgroundColor: Color(0xFF22C55E),
                              ),
                            );
                          },
                        ),
                      ),
                    );
                  },
                ),
                const SizedBox(height: 10),
                WayPointButton(
                  text: 'Open MOB-07 Digital Ticket Wallet',
                  variant: WayPointButtonVariant.outline,
                  icon: Icons.confirmation_number_outlined,
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const TicketWalletScreen(),
                      ),
                    );
                  },
                ),
                const SizedBox(height: 10),
                WayPointButton(
                  text: 'Open MOB-08 Booking History & Refunds',
                  variant: WayPointButtonVariant.outline,
                  icon: Icons.history,
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const BookingHistoryScreen(),
                      ),
                    );
                  },
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildComponent4Hub(BuildContext context) {
    final sampleAlert = DisruptionAlertModel.sampleColomboToElla();

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              TransitBadge(status: TransitStatus.disrupted, customLabel: 'Component 4'),
              SizedBox(width: 8),
              Text(
                'Dineth',
                style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13, fontWeight: FontWeight.bold),
              ),
            ],
          ),
          const SizedBox(height: 12),
          const Text(
            'Disruption Recovery Hub',
            style: TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 6),
          const Text(
            'Live incident monitoring, multi-agent AI rebooking proposals, passenger compensation, and automated schedule remedies.',
            style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
          ),
          const SizedBox(height: 24),

          // Active Disruption Alert Card
          WayPointCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'ACTIVE INCIDENT ALERT',
                      style: TextStyle(color: Color(0xFFF59E0B), fontSize: 11, fontWeight: FontWeight.bold),
                    ),
                    TransitBadge(status: TransitStatus.delayed, customLabel: 'Mechanical Delay'),
                  ],
                ),
                const SizedBox(height: 10),
                const Text(
                  'Colombo (Bastion Hill) → Ella Town',
                  style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 6),
                const Text(
                  'Service: EX-08 (06:30 AM) • Bus ND-8821 Mechanical Failure',
                  style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                ),
                const SizedBox(height: 8),
                const Text(
                  'AI Remedy: Replacement Super Line Luxury Coach Ready',
                  style: TextStyle(color: Color(0xFF60A5FA), fontSize: 14, fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 16),
                WayPointButton(
                  text: 'Open MOB-09 Disruption Push Alert',
                  icon: Icons.warning_amber_rounded,
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => DisruptionAlertScreen(disruption: sampleAlert),
                      ),
                    );
                  },
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

