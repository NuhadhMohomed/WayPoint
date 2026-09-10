import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';

part 'conductor_manifest_event.dart';
part 'conductor_manifest_state.dart';

/// BLoC for MOB-11: Conductor Passenger Manifest Roster.
///
/// Loads the passenger list for a given service and tracks boarding progress.
class ConductorManifestBloc extends Bloc<ConductorManifestEvent, ConductorManifestState> {
  ConductorManifestBloc() : super(const ManifestLoading()) {
    on<LoadManifest>(_onLoad);
    on<RefreshManifest>(_onRefresh);
  }

  Future<void> _onLoad(LoadManifest event, Emitter<ConductorManifestState> emit) async {
    emit(const ManifestLoading());
    await Future.delayed(const Duration(milliseconds: 800));

    // Mock data — in production comes from GET /api/v1/services/{id}/manifest
    final passengers = [
      {'name': 'Nuhadh Mohomed', 'seat': '12A', 'status': 'Boarded', 'boardingPoint': 'Colombo Fort'},
      {'name': 'Nuhadh Mohomed', 'seat': '12B', 'status': 'Boarded', 'boardingPoint': 'Colombo Fort'},
      {'name': 'Kamal Perera', 'seat': '4C', 'status': 'Pending', 'boardingPoint': 'Kaduwela'},
      {'name': 'Amaya Silva', 'seat': '1A', 'status': 'Pending', 'boardingPoint': 'Colombo Fort'},
      {'name': 'Amaya Silva', 'seat': '1B', 'status': 'Pending', 'boardingPoint': 'Colombo Fort'},
      {'name': 'Ruwan Fernando', 'seat': '7D', 'status': 'Boarded', 'boardingPoint': 'Kadawatha'},
      {'name': 'Dilshan Jayasuriya', 'seat': '10A', 'status': 'Pending', 'boardingPoint': 'Kegalle'},
    ];

    final boardedCount = passengers.where((p) => p['status'] == 'Boarded').length;

    emit(ManifestLoaded(
      passengers: passengers,
      boardedCount: boardedCount,
      totalCount: passengers.length,
    ));
  }

  Future<void> _onRefresh(RefreshManifest event, Emitter<ConductorManifestState> emit) async {
    add(LoadManifest(event.serviceId));
  }
}
