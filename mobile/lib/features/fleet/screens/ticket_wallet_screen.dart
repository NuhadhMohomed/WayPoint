import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:intl/intl.dart';
import '../bloc/ticket_wallet_bloc.dart';

/// MOB-07: Digital QR Ticket Wallet.
///
/// Two-tab layout: Active Tickets (with QR codes) and Past Tickets.
/// QR codes are generated from ticket payloads for conductor scanning.
class TicketWalletScreen extends StatelessWidget {
  const TicketWalletScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => TicketWalletBloc()..add(const LoadTickets()),
      child: DefaultTabController(
        length: 2,
        child: Scaffold(
          appBar: AppBar(
            title: const Text('My Tickets'),
            backgroundColor: const Color(0xFF0056D2),
            foregroundColor: Colors.white,
            bottom: const TabBar(
              indicatorColor: Colors.white,
              labelColor: Colors.white,
              unselectedLabelColor: Colors.white70,
              tabs: [
                Tab(icon: Icon(Icons.confirmation_num), text: 'Active'),
                Tab(icon: Icon(Icons.history), text: 'Past'),
              ],
            ),
          ),
          body: BlocBuilder<TicketWalletBloc, TicketWalletState>(
            builder: (context, state) {
              if (state is TicketWalletLoading) {
                return const Center(child: CircularProgressIndicator(color: Color(0xFF0056D2)));
              }
              if (state is TicketWalletError) {
                return Center(child: Text(state.message));
              }
              if (state is TicketWalletLoaded) {
                return TabBarView(
                  children: [
                    _TicketList(tickets: state.activeTickets, isActive: true),
                    _TicketList(tickets: state.pastTickets, isActive: false),
                  ],
                );
              }
              return const SizedBox.shrink();
            },
          ),
        ),
      ),
    );
  }
}

class _TicketList extends StatelessWidget {
  final List<Map<String, dynamic>> tickets;
  final bool isActive;

  const _TicketList({required this.tickets, required this.isActive});

  @override
  Widget build(BuildContext context) {
    if (tickets.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.confirmation_num_outlined, size: 64, color: Colors.grey.shade400),
            const SizedBox(height: 16),
            Text(
              isActive ? 'No active tickets' : 'No past tickets',
              style: TextStyle(color: Colors.grey.shade600, fontSize: 16),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () async => context.read<TicketWalletBloc>().add(const RefreshTickets()),
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: tickets.length,
        itemBuilder: (context, index) {
          final ticket = tickets[index];
          final departure = DateTime.parse(ticket['departureTime']);
          final formattedDate = DateFormat('MMM dd, yyyy • hh:mm a').format(departure);
          final seats = (ticket['seats'] as List).join(', ');

          return Card(
            margin: const EdgeInsets.only(bottom: 16),
            elevation: 2,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  // Header row
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                  decoration: BoxDecoration(
                                    color: isActive ? const Color(0xFF0056D2) : Colors.grey,
                                    borderRadius: BorderRadius.circular(6),
                                  ),
                                  child: Text(
                                    ticket['status'],
                                    style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Text(ticket['id'], style: TextStyle(color: Colors.grey.shade500, fontSize: 12)),
                              ],
                            ),
                            const SizedBox(height: 8),
                            Text(ticket['route'] ?? ticket['serviceCode'],
                                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
                            const SizedBox(height: 4),
                            Text(formattedDate, style: TextStyle(color: Colors.grey.shade700)),
                            const SizedBox(height: 4),
                            Text('Seats: $seats',
                                style: const TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF0056D2))),
                          ],
                        ),
                      ),
                      // QR Code for active tickets
                      if (isActive)
                        Container(
                          padding: const EdgeInsets.all(6),
                          decoration: BoxDecoration(
                            border: Border.all(color: Colors.grey.shade300),
                            borderRadius: BorderRadius.circular(10),
                          ),
                          child: QrImageView(
                            data: ticket['qrPayload'],
                            version: QrVersions.auto,
                            size: 100.0,
                          ),
                        ),
                    ],
                  ),
                  if (isActive) ...[
                    const Divider(height: 24),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.qr_code_scanner, size: 16, color: Colors.grey.shade500),
                        const SizedBox(width: 6),
                        Text('Show this QR to the conductor when boarding',
                            style: TextStyle(fontSize: 12, color: Colors.grey.shade500)),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
