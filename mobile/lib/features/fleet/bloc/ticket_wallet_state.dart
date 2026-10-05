part of 'ticket_wallet_bloc.dart';

abstract class TicketWalletState extends Equatable {
  const TicketWalletState();

  @override
  List<Object> get props => [];
}

class TicketWalletLoading extends TicketWalletState {
  const TicketWalletLoading();
}

class TicketWalletLoaded extends TicketWalletState {
  final List<Map<String, dynamic>> activeTickets;
  final List<Map<String, dynamic>> pastTickets;

  const TicketWalletLoaded({required this.activeTickets, required this.pastTickets});

  @override
  List<Object> get props => [activeTickets, pastTickets];
}

class TicketWalletError extends TicketWalletState {
  final String message;
  const TicketWalletError(this.message);

  @override
  List<Object> get props => [message];
}
