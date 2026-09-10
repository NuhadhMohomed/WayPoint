import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';

part 'ticket_wallet_event.dart';
part 'ticket_wallet_state.dart';

/// BLoC for MOB-07: Digital QR Ticket Wallet.
///
/// Loads active & past tickets. In production would fetch from API + local cache.
/// Mock data is used for initial development.
class TicketWalletBloc extends Bloc<TicketWalletEvent, TicketWalletState> {
  TicketWalletBloc() : super(const TicketWalletLoading()) {
    on<LoadTickets>(_onLoadTickets);
    on<RefreshTickets>(_onRefreshTickets);
  }

  Future<void> _onLoadTickets(LoadTickets event, Emitter<TicketWalletState> emit) async {
    emit(const TicketWalletLoading());
    await Future.delayed(const Duration(milliseconds: 800)); // Simulate API

    final activeTickets = [
      {
        'id': 'TK-12345',
        'serviceCode': 'CMB-KAN-001',
        'route': 'Colombo → Kandy',
        'departureTime': DateTime.now().add(const Duration(hours: 2)).toIso8601String(),
        'seats': ['12A', '12B'],
        'qrPayload': 'waypoint://ticket/TK-12345',
        'status': 'Confirmed',
      },
    ];
    final pastTickets = [
      {
        'id': 'TK-00987',
        'serviceCode': 'CMB-GAL-002',
        'route': 'Colombo → Galle',
        'departureTime': DateTime.now().subtract(const Duration(days: 2)).toIso8601String(),
        'seats': ['4C'],
        'qrPayload': 'waypoint://ticket/TK-00987',
        'status': 'Completed',
      },
      {
        'id': 'TK-00456',
        'serviceCode': 'KAN-CMB-003',
        'route': 'Kandy → Colombo',
        'departureTime': DateTime.now().subtract(const Duration(days: 7)).toIso8601String(),
        'seats': ['1A'],
        'qrPayload': 'waypoint://ticket/TK-00456',
        'status': 'Completed',
      },
    ];

    emit(TicketWalletLoaded(activeTickets: activeTickets, pastTickets: pastTickets));
  }

  Future<void> _onRefreshTickets(RefreshTickets event, Emitter<TicketWalletState> emit) async {
    add(const LoadTickets());
  }
}
