import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/conductor_manifest_bloc.dart';

/// MOB-11: Conductor Passenger Manifest Roster.
///
/// Service selector dropdown, search bar, boarding progress bar,
/// and passenger list with status badges. Supports pull-to-refresh.
class ConductorManifestScreen extends StatefulWidget {
  const ConductorManifestScreen({super.key});

  @override
  State<ConductorManifestScreen> createState() => _ConductorManifestScreenState();
}

class _ConductorManifestScreenState extends State<ConductorManifestScreen> {
  String _selectedService = 'CMB-KAN-001';
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => ConductorManifestBloc()..add(LoadManifest(_selectedService)),
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Passenger Manifest'),
          backgroundColor: const Color(0xFF0056D2),
          foregroundColor: Colors.white,
        ),
        body: Column(
          children: [
            // Controls: service selector + search
            Container(
              padding: const EdgeInsets.all(16),
              color: Colors.white,
              child: Column(
                children: [
                  DropdownButtonFormField<String>(
                    initialValue: _selectedService,
                    decoration: const InputDecoration(
                      labelText: 'Select Service',
                      border: OutlineInputBorder(),
                      contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                    ),
                    items: const [
                      DropdownMenuItem(value: 'CMB-KAN-001', child: Text('CMB-KAN-001 (Colombo → Kandy)')),
                      DropdownMenuItem(value: 'CMB-GAL-002', child: Text('CMB-GAL-002 (Colombo → Galle)')),
                      DropdownMenuItem(value: 'KAN-CMB-003', child: Text('KAN-CMB-003 (Kandy → Colombo)')),
                    ],
                    onChanged: (value) {
                      if (value != null) {
                        setState(() => _selectedService = value);
                        context.read<ConductorManifestBloc>().add(LoadManifest(value));
                      }
                    },
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _searchCtrl,
                    decoration: const InputDecoration(
                      hintText: 'Search passenger or seat...',
                      prefixIcon: Icon(Icons.search),
                      border: OutlineInputBorder(),
                      contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                    ),
                    onChanged: (v) => setState(() => _searchQuery = v.toLowerCase()),
                  ),
                ],
              ),
            ),

            // Main content
            Expanded(
              child: BlocBuilder<ConductorManifestBloc, ConductorManifestState>(
                builder: (context, state) {
                  if (state is ManifestLoading) {
                    return const Center(child: CircularProgressIndicator(color: Color(0xFF0056D2)));
                  }
                  if (state is ManifestError) {
                    return Center(child: Text(state.message));
                  }
                  if (state is ManifestLoaded) {
                    final filtered = state.passengers.where((p) {
                      final name = (p['name'] as String).toLowerCase();
                      final seat = (p['seat'] as String).toLowerCase();
                      return name.contains(_searchQuery) || seat.contains(_searchQuery);
                    }).toList();

                    return Column(
                      children: [
                        // Boarding progress bar
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                          color: Colors.grey.shade50,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  const Text('Boarding Progress', style: TextStyle(fontWeight: FontWeight.bold)),
                                  Text(
                                    '${state.boardedCount} / ${state.totalCount} boarded',
                                    style: TextStyle(color: Colors.grey.shade700, fontWeight: FontWeight.bold),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 8),
                              ClipRRect(
                                borderRadius: BorderRadius.circular(4),
                                child: LinearProgressIndicator(
                                  value: state.totalCount > 0 ? state.boardedCount / state.totalCount : 0,
                                  backgroundColor: Colors.grey.shade300,
                                  color: Colors.green,
                                  minHeight: 10,
                                ),
                              ),
                            ],
                          ),
                        ),

                        // Passenger list
                        Expanded(
                          child: RefreshIndicator(
                            onRefresh: () async {
                              context.read<ConductorManifestBloc>().add(RefreshManifest(_selectedService));
                            },
                            child: filtered.isEmpty
                                ? ListView(
                                    children: const [
                                      SizedBox(height: 100),
                                      Center(child: Text('No passengers match your search', style: TextStyle(color: Colors.grey))),
                                    ],
                                  )
                                : ListView.separated(
                                    itemCount: filtered.length,
                                    separatorBuilder: (_, __) => const Divider(height: 1),
                                    itemBuilder: (context, index) {
                                      final p = filtered[index];
                                      final isBoarded = p['status'] == 'Boarded';

                                      return ListTile(
                                        leading: CircleAvatar(
                                          backgroundColor: isBoarded ? Colors.green.shade100 : Colors.grey.shade200,
                                          child: Text(
                                            p['seat'] as String,
                                            style: TextStyle(
                                              color: isBoarded ? Colors.green.shade800 : Colors.black87,
                                              fontWeight: FontWeight.bold,
                                              fontSize: 13,
                                            ),
                                          ),
                                        ),
                                        title: Text(p['name'] as String, style: const TextStyle(fontWeight: FontWeight.bold)),
                                        subtitle: Text('Boarding: ${p['boardingPoint']}'),
                                        trailing: Container(
                                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                                          decoration: BoxDecoration(
                                            color: isBoarded ? Colors.green : Colors.amber,
                                            borderRadius: BorderRadius.circular(16),
                                          ),
                                          child: Text(
                                            p['status'] as String,
                                            style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.bold),
                                          ),
                                        ),
                                      );
                                    },
                                  ),
                          ),
                        ),
                      ],
                    );
                  }
                  return const SizedBox.shrink();
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
