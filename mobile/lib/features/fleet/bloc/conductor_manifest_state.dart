part of 'conductor_manifest_bloc.dart';

abstract class ConductorManifestState extends Equatable {
  const ConductorManifestState();

  @override
  List<Object> get props => [];
}

class ManifestLoading extends ConductorManifestState {
  const ManifestLoading();
}

class ManifestLoaded extends ConductorManifestState {
  final List<Map<String, dynamic>> passengers;
  final int boardedCount;
  final int totalCount;

  const ManifestLoaded({
    required this.passengers,
    required this.boardedCount,
    required this.totalCount,
  });

  @override
  List<Object> get props => [passengers, boardedCount, totalCount];
}

class ManifestError extends ConductorManifestState {
  final String message;
  const ManifestError(this.message);

  @override
  List<Object> get props => [message];
}
