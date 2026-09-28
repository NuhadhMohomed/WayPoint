import 'package:flutter/material.dart';
import 'core/theme/app_theme.dart';

import 'features/booking/models/booking_models.dart';
import 'features/booking/screens/booking_history_screen.dart';
import 'features/booking/screens/payment_checkout_screen.dart';
import 'features/booking/screens/ticket_wallet_screen.dart';
import 'core/widgets/waypoint_button.dart';
import 'core/widgets/waypoint_card.dart';
import 'core/widgets/transit_badge.dart';
import 'features/journey/screens/journey_search_screen.dart';

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
    );
  }
}

class MainNavigationScreen extends StatefulWidget {
  const MainNavigationScreen({super.key});

  @override
  State<MainNavigationScreen> createState() => _MainNavigationScreenState();
}

class _MainNavigationScreenState extends State<MainNavigationScreen> {
  int _currentIndex = 3; // Default to Component 4 for testing

  @override
  Widget build(BuildContext context) {
    final screens = [
      const JourneySearchScreen(),
      const Center(child: Text('Component 2: Seat Picker & Fleet (Nuhadh)', style: TextStyle(fontSize: 16))),
      _buildComponent3Hub(context),
      _buildComponent4Hub(context),
    ];

    return Scaffold(
      appBar: AppBar(
        title: const Text('WayPoint Transit'),
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

