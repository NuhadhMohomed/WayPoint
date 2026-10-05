import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/seat_reservation_bar.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../fleet/bloc/seat_picker_bloc.dart';
import '../../fleet/data/fleet_api_service.dart';
import '../models/booking_models.dart';
import 'payment_checkout_screen.dart';
import '../../auth/bloc/auth_cubit.dart';
import '../../auth/bloc/auth_state.dart';
import '../../auth/screens/passenger_auth_screen.dart';
import '../widgets/auth_prompt_modal.dart';

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
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return BlocProvider(
      create: (_) => SeatPickerBloc(api: apiService)..add(LoadSeatMap(serviceId)),
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Select Seats'),
          elevation: 0,
        ),
        body: BlocConsumer<SeatPickerBloc, SeatPickerState>(
          listener: (context, state) {
            if (state.status == SeatPickerStatus.holdExpired) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Seat hold expired. Please reselect your seats.'),
                  backgroundColor: AppTheme.errorColor,
                ),
              );
            }
          },
          builder: (context, state) {
            if (state.status == SeatPickerStatus.loading || state.status == SeatPickerStatus.initial) {
              return const Center(
                child: CircularProgressIndicator(color: AppTheme.primaryColor),
              );
            }

            if (state.status == SeatPickerStatus.error) {
              return EmptyStateView(
                icon: Icons.error_outline,
                title: 'Unable to Load Seat Map',
                description: state.errorMessage ?? 'Please check your connection and retry.',
                actionLabel: 'Retry',
                onAction: () {
                  context.read<SeatPickerBloc>().add(LoadSeatMap(serviceId));
                },
              );
            }

            final matrix = state.seatMatrix ?? {};
            final seats = (matrix['seats'] as List<dynamic>?) ?? [];

            int maxRow = 0, maxCol = 0;
            for (final s in seats) {
              if ((s['rowIndex'] as int) > maxRow) maxRow = s['rowIndex'] as int;
              if ((s['columnIndex'] as int) > maxCol) maxCol = s['columnIndex'] as int;
            }
            final totalCols = maxCol + 1;

            return Column(
              children: [
                // Legend Bar
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  color: isDark ? const Color(0xFF131B2E) : const Color(0xFFF1F5F9),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                    children: [
                      _LegendChip(
                        color: isDark ? const Color(0xFF1E293B) : const Color(0xFFE2E8F0),
                        label: 'Available',
                        borderColor: isDark ? const Color(0xFF475569) : const Color(0xFFCBD5E1),
                      ),
                      const _LegendChip(
                        color: AppTheme.primaryColor,
                        label: 'Selected',
                        borderColor: AppTheme.primaryDark,
                      ),
                      const _LegendChip(
                        color: AppTheme.secondaryColor,
                        label: 'Held',
                        borderColor: Color(0xFFD97706),
                      ),
                      _LegendChip(
                        color: isDark ? const Color(0xFF334155) : const Color(0xFF94A3B8),
                        label: 'Booked',
                        borderColor: Colors.transparent,
                      ),
                    ],
                  ),
                ),

                // Front of Bus Indicator with Steering Wheel
                Container(
                  margin: const EdgeInsets.symmetric(vertical: 12),
                  padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 6),
                  decoration: BoxDecoration(
                    color: isDark ? const Color(0xFF1E293B) : const Color(0xFFE2E8F0),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.directions_bus, size: 16, color: isDark ? Colors.grey[400] : Colors.grey[600]),
                      const SizedBox(width: 8),
                      Text(
                        'FRONT • DRIVER CABIN',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: isDark ? Colors.grey[300] : Colors.grey[700],
                          fontSize: 11,
                          letterSpacing: 1.0,
                        ),
                      ),
                    ],
                  ),
                ),

                // Scrollable Coach Layout
                Expanded(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
                    child: Column(
                      children: List.generate(maxRow + 1, (r) {
                        return Padding(
                          padding: const EdgeInsets.only(bottom: 8),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              SizedBox(
                                width: 22,
                                child: Text(
                                  '${r + 1}',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.w600,
                                    color: isDark ? Colors.grey[500] : Colors.grey[400],
                                  ),
                                ),
                              ),
                              ...List.generate(totalCols, (c) {
                                final seat = seats.cast<Map<String, dynamic>?>().firstWhere(
                                      (s) => s != null && s['rowIndex'] == r && s['columnIndex'] == c,
                                      orElse: () => null,
                                    );

                                final aisleGap = totalCols == 4 && c == 2
                                    ? const SizedBox(width: 24)
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

                // Bottom SeatReservationBar or Quick Selection Bar
                if (state.status == SeatPickerStatus.seatsHeld)
                  SeatReservationBar(
                    remainingSeconds: state.remainingSeconds,
                    selectedSeats: state.selectedSeats,
                    totalAmount: state.selectedSeats.length * 2400.0,
                    onContinue: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) => PaymentCheckoutScreen(
                            holdInfo: SeatHoldInfo.sampleColomboToElla(),
                          ),
                        ),
                      );
                    },
                  )
                else
                  _QuickSelectionBar(state: state),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _SeatCell extends StatelessWidget {
  final Map<String, dynamic>? seat;
  final SeatPickerState state;

  const _SeatCell({required this.seat, required this.state});

  @override
  Widget build(BuildContext context) {
    if (seat == null) {
      return Container(width: 44, height: 44, margin: const EdgeInsets.symmetric(horizontal: 4));
    }

    final isDark = Theme.of(context).brightness == Brightness.dark;
    final seatNumber = seat!['seatNumber'] as String;
    final status = seat!['status'] as String;
    final isSelected = state.selectedSeats.contains(seatNumber);
    final isAvailable = status == 'Available';

    Color bg;
    Color border;
    Color textColor;

    if (isSelected) {
      bg = AppTheme.primaryColor;
      border = AppTheme.primaryDark;
      textColor = AppTheme.onPrimaryColor;
    } else if (isAvailable) {
      bg = isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC);
      border = isDark ? const Color(0xFF475569) : const Color(0xFFCBD5E1);
      textColor = isDark ? Colors.white : Colors.black87;
    } else if (status == 'Held') {
      bg = AppTheme.secondaryColor.withOpacity(0.25);
      border = AppTheme.secondaryColor;
      textColor = AppTheme.secondaryColor;
    } else {
      bg = isDark ? const Color(0xFF1A243B).withOpacity(0.5) : const Color(0xFFE2E8F0);
      border = Colors.transparent;
      textColor = isDark ? Colors.grey[600]! : Colors.grey[400]!;
    }

    return GestureDetector(
      onTap: () {
        if (!isAvailable && !isSelected) return;
        if (state.status == SeatPickerStatus.seatsHeld) return;

        HapticFeedback.lightImpact();
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
        margin: const EdgeInsets.symmetric(horizontal: 4),
        decoration: BoxDecoration(
          color: bg,
          border: Border.all(color: border, width: isSelected ? 2 : 1.2),
          borderRadius: BorderRadius.circular(10),
          boxShadow: isSelected
              ? [
                  BoxShadow(
                    color: AppTheme.primaryColor.withOpacity(0.3),
                    blurRadius: 6,
                    offset: const Offset(0, 2),
                  ),
                ]
              : null,
        ),
        child: Center(
          child: isSelected
              ? const Icon(Icons.check, size: 18, color: AppTheme.onPrimaryColor)
              : Text(
                  seatNumber,
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: textColor,
                  ),
                ),
        ),
      ),
    );
  }
}

class _LegendChip extends StatelessWidget {
  final Color color;
  final String label;
  final Color borderColor;

  const _LegendChip({
    required this.color,
    required this.label,
    required this.borderColor,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 14,
          height: 14,
          decoration: BoxDecoration(
            color: color,
            border: Border.all(color: borderColor),
            borderRadius: BorderRadius.circular(4),
          ),
        ),
        const SizedBox(width: 5),
        Text(
          label,
          style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w500),
        ),
      ],
    );
  }
}

class _QuickSelectionBar extends StatelessWidget {
  final SeatPickerState state;

  const _QuickSelectionBar({required this.state});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final count = state.selectedSeats.length;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF131B2E) : Colors.white,
        border: Border(
          top: BorderSide(
            color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
          ),
        ),
      ),
      child: SafeArea(
        top: false,
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    count == 0 ? 'No seats selected' : '$count Seat${count == 1 ? '' : 's'} Selected',
                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                  ),
                  if (state.selectedSeats.isNotEmpty)
                    Text(
                      'Seats: ${state.selectedSeats.join(', ')}',
                      style: const TextStyle(color: AppTheme.primaryColor, fontSize: 12, fontWeight: FontWeight.w600),
                    ),
                ],
              ),
            ),
            ElevatedButton(
              onPressed: state.selectedSeats.isEmpty || state.status == SeatPickerStatus.seatsHolding
                  ? null
                  : () {
                      HapticFeedback.mediumImpact();
                      final authState = context.read<AuthCubit>().state;
                      if (authState is! Authenticated) {
                        showModalBottomSheet(
                          context: context,
                          backgroundColor: Colors.transparent,
                          isScrollControlled: true,
                          builder: (bottomSheetContext) => AuthPromptModal(
                            onSignIn: () {
                              Navigator.pop(bottomSheetContext);
                              Navigator.push(
                                context,
                                MaterialPageRoute(builder: (_) => const PassengerAuthScreen()),
                              );
                            },
                            onCancel: () => Navigator.pop(bottomSheetContext),
                          ),
                        );
                        return;
                      }
                      context.read<SeatPickerBloc>().add(const HoldSeats());
                    },
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.primaryColor,
                foregroundColor: AppTheme.onPrimaryColor,
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
              child: state.status == SeatPickerStatus.seatsHolding
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2, color: AppTheme.onPrimaryColor),
                    )
                  : const Text(
                      'Hold Seats (10m)',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}
