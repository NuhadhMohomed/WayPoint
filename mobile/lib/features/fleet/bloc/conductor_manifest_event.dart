part of 'conductor_manifest_bloc.dart';

abstract class ConductorManifestEvent extends Equatable {
  const ConductorManifestEvent();

  @override
  List<Object> get props => [];
}

/// Load the passenger manifest for a specific service.
class LoadManifest extends ConductorManifestEvent {
  final String serviceId;
  const LoadManifest(this.serviceId);

  @override
  List<Object> get props => [serviceId];
}

/// Pull-to-refresh the manifest.
class RefreshManifest extends ConductorManifestEvent {
  final String serviceId;
  const RefreshManifest(this.serviceId);

  @override
  List<Object> get props => [serviceId];
}
