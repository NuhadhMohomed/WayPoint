part of 'ticket_wallet_bloc.dart';

abstract class TicketWalletEvent extends Equatable {
  const TicketWalletEvent();

  @override
  List<Object> get props => [];
}

class LoadTickets extends TicketWalletEvent {
  const LoadTickets();
}

class RefreshTickets extends TicketWalletEvent {
  const RefreshTickets();
}
