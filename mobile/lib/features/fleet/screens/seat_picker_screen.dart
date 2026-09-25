import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/seat_picker_bloc.dart';
import '../data/fleet_api_service.dart';

/// MOB-05: Interactive Seat Picker & 10-minute Hold Screen.
///
/// Renders a 2D bus seat grid from the API seat matrix.
/// Color legend: Green = Available, Blue = Selected, Amber = Held, Red = Booked.
class SeatPickerScreen extends StatelessWidget {
  final String serviceId;
  final FleetApiService apiService;

  const SeatPickerScreen({
    super.key,
    required this.serviceId,
    required this.apiService,
  });

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => SeatPickerBloc(api: apiService)..add(LoadSeatMap(serviceId)),
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Select Your Seats'),
          backgroundColor: const Color(0xFF0056D2), // Lanka Blue
          foregroundColor: Colors.white,
        ),
        body: BlocConsumer<SeatPickerBloc, SeatPickerState>(
          listener: (context, state) {
            if (state.status == SeatPickerStatus.holdExpired) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Seat hold expired. Please select again.'),
                  backgroundColor: Colors.red,
                ),
              );
            }
          },
          builder: (context, state) {
            if (state.status == SeatPickerStatus.loading || state.status == SeatPickerStatus.initial) {
              return const Center(child: CircularProgressIndicator(color: Color(0xFF0056D2)));
            }

            if (state.status == SeatPickerStatus.error) {
              return Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(Icons.error_outline, size: 48, color: Colors.red),
                    const SizedBox(height: 12),
                    Text(state.errorMessage ?? 'Failed to load seats'),
                  ],
                ),
              );
            }

            final matrix = state.seatMatrix!;
            final seats = (matrix['seats'] as List<dynamic>?) ?? [];

            int maxRow = 0, maxCol = 0;
            for (final s in seats) {
              if ((s['rowIndex'] as int) > maxRow) maxRow = s['rowIndex'] as int;
              if ((s['columnIndex'] as int) > maxCol) maxCol = s['columnIndex'] as int;
            }
            final totalCols = maxCol + 1;

            return Column(
              children: [
                // Hold countdown bar
                if (state.status == SeatPickerStatus.seatsHeld) _HoldCountdownBar(state.remainingSeconds),

                // Legend row
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                    children: [
                      _LegendChip(color: Colors.green.shade200, label: 'Available'),
                      const _LegendChip(color: Color(0xFF0056D2), label: 'Selected'),
                      _LegendChip(color: Colors.amber.shade300, label: 'Held'),
                      _LegendChip(color: Colors.red.shade300, label: 'Booked'),
                    ],
                  ),
                ),

                // "FRONT" indicator
                Container(
                  margin: const EdgeInsets.only(bottom: 8),
                  padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 4),
                  decoration: BoxDecoration(
                    color: Colors.grey.shade200,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: const Text('FRONT', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.grey, fontSize: 12)),
                ),

                // Seat grid
                Expanded(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: Column(
                      children: List.generate(maxRow + 1, (r) {
                        return Padding(
                          padding: const EdgeInsets.only(bottom: 6),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              // Row number
                              SizedBox(
                                width: 20,
                                child: Text('${r + 1}', style: TextStyle(fontSize: 10, color: Colors.grey.shade500)),
                              ),
                              ...List.generate(totalCols, (c) {
                                final seat = seats.cast<Map<String, dynamic>?>().firstWhere(
                                      (s) => s != null && s['rowIndex'] == r && s['columnIndex'] == c,
                                      orElse: () => null,
                                    );

                                // Aisle gap for 4-column (2+2) layouts
                                final aisleGap = totalCols == 4 && c == 2
                                    ? const SizedBox(width: 20)
                                    : const SizedBox.shrink();

                                return Row(
                                  children: [
                                    aisleGap,
                                    _SeatCell(seat: seat, state: state),
                                  ],
                                );
                              }),
                            ],
                          ),
                        );
                      }),
                    ),
                  ),
                ),

                // Bottom action bar
                _BottomActionBar(),
              ],
            );
          },
        ),
      ),
    );
  }
}

// ─── Seat Cell ───

class _SeatCell extends StatelessWidget {
  final Map<String, dynamic>? seat;
  final SeatPickerState state;

  const _SeatCell({required this.seat, required this.state});

  @override
  Widget build(BuildContext context) {
    if (seat == null) {
      return Container(width: 44, height: 44, margin: const EdgeInsets.symmetric(horizontal: 3));
    }

    final seatNumber = seat!['seatNumber'] as String;
    final status = seat!['status'] as String;
    final isSelected = state.selectedSeats.contains(seatNumber);
    final isAvailable = status == 'Available';

    Color bg, border;
    Color textColor = Colors.black87;

    if (isSelected) {
      bg = const Color(0xFF0056D2);
      border = const Color(0xFF003DA5);
      textColor = Colors.white;
    } else if (status == 'Available') {
      bg = Colors.green.shade100;
      border = Colors.green.shade400;
    } else if (status == 'Held') {
      bg = Colors.amber.shade200;
      border = Colors.amber.shade400;
    } else {
      bg = Colors.red.shade200;
      border = Colors.red.shade400;
    }

    return GestureDetector(
      onTap: () {
        if (!isAvailable && !isSelected) return;
        if (state.status == SeatPickerStatus.seatsHeld) return;

        final bloc = context.read<SeatPickerBloc>();
        if (isSelected) {
          bloc.add(DeselectSeat(seatNumber));
        } else {
          bloc.add(SelectSeat(seatNumber));
        }
      },
      child: Container(
        width: 44,
        height: 44,
        margin: const EdgeInsets.symmetric(horizontal: 3),
        decoration: BoxDecoration(
          color: bg,
          border: Border.all(color: border, width: 2),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Center(
          child: Text(seatNumber, style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: textColor)),
        ),
      ),
    );
  }
}

// ─── Hold Countdown Bar ───

class _HoldCountdownBar extends StatelessWidget {
  final int remainingSeconds;
  const _HoldCountdownBar(this.remainingSeconds);

  @override
  Widget build(BuildContext context) {
    final mins = remainingSeconds ~/ 60;
    final secs = remainingSeconds % 60;
    final isUrgent = remainingSeconds < 120;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(vertical: 10),
      color: isUrgent ? Colors.red : Colors.amber,
      child: Text(
        'Hold expires in $mins:${secs.toString().padLeft(2, '0')}',
        textAlign: TextAlign.center,
        style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.white, fontSize: 14),
      ),
    );
  }
}

// ─── Legend Chip ───

class _LegendChip extends StatelessWidget {
  final Color color;
  final String label;
  const _LegendChip({required this.color, required this.label});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(width: 14, height: 14, decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(4))),
        const SizedBox(width: 4),
        Text(label, style: const TextStyle(fontSize: 11)),
      ],
    );
  }
}

// ─── Bottom Action Bar ───

class _BottomActionBar extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final state = context.watch<SeatPickerBloc>().state;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.05), offset: const Offset(0, -4), blurRadius: 10)],
      ),
      child: SafeArea(
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('${state.selectedSeats.length} Seat${state.selectedSeats.length == 1 ? '' : 's'} Selected',
                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                  if (state.selectedSeats.isNotEmpty)
                    Text(state.selectedSeats.join(', '), style: const TextStyle(color: Colors.grey, fontSize: 12)),
                ],
              ),
            ),
            ElevatedButton(
              onPressed: state.selectedSeats.isEmpty || state.status == SeatPickerStatus.seatsHolding
                  ? null
                  : () {
                      if (state.status == SeatPickerStatus.seatsHeld) {
                        // Navigate to payment checkout
                        Navigator.pushNamed(context, '/checkout');
                      } else {
                        context.read<SeatPickerBloc>().add(const HoldSeats());
                      }
                    },
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF0056D2),
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
              child: state.status == SeatPickerStatus.seatsHolding
                  ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                  : Text(
                      state.status == SeatPickerStatus.seatsHeld ? 'Checkout' : 'Hold Seats',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}
