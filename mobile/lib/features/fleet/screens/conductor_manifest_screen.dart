import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/theme/app_theme.dart';
import '../bloc/conductor_manifest_bloc.dart';

class ConductorManifestScreen extends StatefulWidget {
  const ConductorManifestScreen({super.key});

  @override
  State<ConductorManifestScreen> createState() => _ConductorManifestScreenState();
}

class _ConductorManifestScreenState extends State<ConductorManifestScreen> {
  String _selectedService = 'CMB-KAN-001';
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';
  String _statusFilter = 'All'; // 'All', 'Boarded', 'Pending'
  final Map<String, String> _localStatusOverrides = {};

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final surfaceColor = isDark ? const Color(0xFF131B2E) : Colors.white;
    final borderColor = isDark ? const Color(0xFF23304D) : const Color(0xFFE2E8F0);

    return BlocProvider(
      create: (_) => ConductorManifestBloc()..add(LoadManifest(_selectedService)),
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Passenger Manifest', style: TextStyle(fontWeight: FontWeight.bold)),
          elevation: 0,
        ),
        body: Column(
          children: [
            // Controls: service selector + search + filter pills
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: surfaceColor,
                border: Border(bottom: BorderSide(color: borderColor)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Builder(
                    builder: (ctx) => DropdownButtonFormField<String>(
                      initialValue: _selectedService,
                      decoration: const InputDecoration(
                        labelText: 'Active Service Route',
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.all(Radius.circular(10)),
                        ),
                        contentPadding: EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                      ),
                      items: const [
                        DropdownMenuItem(value: 'CMB-KAN-001', child: Text('CMB-KAN-001 (Colombo → Kandy)')),
                        DropdownMenuItem(value: 'CMB-GAL-002', child: Text('CMB-GAL-002 (Colombo → Galle)')),
                        DropdownMenuItem(value: 'KAN-CMB-003', child: Text('KAN-CMB-003 (Kandy → Colombo)')),
                      ],
                      onChanged: (value) {
                        if (value != null) {
                          setState(() {
                            _selectedService = value;
                            _localStatusOverrides.clear();
                          });
                          ctx.read<ConductorManifestBloc>().add(LoadManifest(value));
                        }
                      },
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _searchCtrl,
                    decoration: InputDecoration(
                      hintText: 'Search passenger name or seat (e.g. 12A)...',
                      prefixIcon: const Icon(Icons.search, size: 20),
                      suffixIcon: _searchQuery.isNotEmpty
                          ? IconButton(
                              icon: const Icon(Icons.clear, size: 18),
                              onPressed: () {
                                _searchCtrl.clear();
                                setState(() => _searchQuery = '');
                              },
                            )
                          : null,
                      border: const OutlineInputBorder(
                        borderRadius: BorderRadius.all(Radius.circular(10)),
                      ),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                    ),
                    onChanged: (v) => setState(() => _searchQuery = v.trim().toLowerCase()),
                  ),
                  const SizedBox(height: 12),
                  // Filter Pills: All, Boarded, Pending
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: ['All', 'Boarded', 'Pending'].map((filter) {
                        final isSelected = _statusFilter == filter;
                        return Padding(
                          padding: const EdgeInsets.only(right: 8.0),
                          child: FilterChip(
                            label: Text(filter),
                            selected: isSelected,
                            selectedColor: const Color(0xFF32DE84).withOpacity(0.2),
                            checkmarkColor: isDark ? const Color(0xFF32DE84) : const Color(0xFF005312),
                            labelStyle: TextStyle(
                              fontSize: 12,
                              fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                              color: isSelected
                                  ? (isDark ? const Color(0xFF32DE84) : const Color(0xFF005312))
                                  : (isDark ? Colors.grey[300] : Colors.grey[700]),
                            ),
                            onSelected: (_) {
                              setState(() => _statusFilter = filter);
                            },
                          ),
                        );
                      }).toList(),
                    ),
                  ),
                ],
              ),
            ),

            // Main content
            Expanded(
              child: BlocBuilder<ConductorManifestBloc, ConductorManifestState>(
                builder: (context, state) {
                  if (state is ManifestLoading) {
                    return const Center(child: CircularProgressIndicator(color: AppTheme.primaryColor));
                  }
                  if (state is ManifestError) {
                    return Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.error_outline, size: 48, color: AppTheme.errorColor),
                          const SizedBox(height: 12),
                          Text(state.message),
                          const SizedBox(height: 12),
                          ElevatedButton(
                            onPressed: () => context.read<ConductorManifestBloc>().add(LoadManifest(_selectedService)),
                            child: const Text('Retry'),
                          ),
                        ],
                      ),
                    );
                  }
                  if (state is ManifestLoaded) {
                    final modifiedList = state.passengers.map((p) {
                      final seat = p['seat'] as String;
                      final currentStatus = _localStatusOverrides[seat] ?? (p['status'] as String);
                      return {
                        ...p,
                        'status': currentStatus,
                      };
                    }).toList();

                    final filtered = modifiedList.where((p) {
                      final name = (p['name'] as String).toLowerCase();
                      final seat = (p['seat'] as String).toLowerCase();
                      final matchesSearch = _searchQuery.isEmpty || name.contains(_searchQuery) || seat.contains(_searchQuery);

                      final status = p['status'] as String;
                      final matchesFilter = _statusFilter == 'All' || status.toLowerCase() == _statusFilter.toLowerCase();

                      return matchesSearch && matchesFilter;
                    }).toList();

                    final currentBoardedCount = modifiedList.where((p) => p['status'] == 'Boarded').length;
                    final total = modifiedList.length;

                    return Column(
                      children: [
                        // Boarding progress bar
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                          decoration: BoxDecoration(
                            color: isDark ? const Color(0xFF0F172A) : const Color(0xFFF1F5F9),
                            border: Border(bottom: BorderSide(color: borderColor)),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  const Text('Manifest Boarding Progress', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                                  Text(
                                    '$currentBoardedCount / $total Boarded (${(total > 0 ? (currentBoardedCount / total * 100) : 0).toStringAsFixed(0)}%)',
                                    style: TextStyle(
                                      color: isDark ? const Color(0xFF32DE84) : const Color(0xFF005312),
                                      fontWeight: FontWeight.bold,
                                      fontSize: 13,
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 8),
                              ClipRRect(
                                borderRadius: BorderRadius.circular(4),
                                child: LinearProgressIndicator(
                                  value: total > 0 ? currentBoardedCount / total : 0,
                                  backgroundColor: isDark ? Colors.grey[800] : Colors.grey[300],
                                  color: AppTheme.primaryColor,
                                  minHeight: 8,
                                ),
                              ),
                            ],
                          ),
                        ),

                        // Passenger list
                        Expanded(
                          child: RefreshIndicator(
                            color: AppTheme.primaryColor,
                            onRefresh: () async {
                              context.read<ConductorManifestBloc>().add(RefreshManifest(_selectedService));
                            },
                            child: filtered.isEmpty
                                ? ListView(
                                    children: [
                                      const SizedBox(height: 80),
                                      Center(
                                        child: Column(
                                          children: [
                                            Icon(Icons.person_search_outlined, size: 48, color: Colors.grey[400]),
                                            const SizedBox(height: 8),
                                            Text(
                                              'No passengers found matching criteria',
                                              style: TextStyle(color: Colors.grey[500], fontSize: 14),
                                            ),
                                          ],
                                        ),
                                      ),
                                    ],
                                  )
                                : ListView.separated(
                                    padding: const EdgeInsets.symmetric(vertical: 8),
                                    itemCount: filtered.length,
                                    separatorBuilder: (_, __) => Divider(height: 1, color: borderColor),
                                    itemBuilder: (context, index) {
                                      final p = filtered[index];
                                      final seat = p['seat'] as String;
                                      final isBoarded = p['status'] == 'Boarded';

                                      return ListTile(
                                        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                                        leading: Container(
                                          width: 48,
                                          height: 48,
                                          alignment: Alignment.center,
                                          decoration: BoxDecoration(
                                            color: isBoarded
                                                ? const Color(0xFF32DE84).withOpacity(0.18)
                                                : (isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9)),
                                            borderRadius: BorderRadius.circular(10),
                                            border: Border.all(
                                              color: isBoarded
                                                  ? const Color(0xFF32DE84)
                                                  : (isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1)),
                                            ),
                                          ),
                                          child: Text(
                                            seat,
                                            style: TextStyle(
                                              color: isBoarded
                                                  ? (isDark ? const Color(0xFF32DE84) : const Color(0xFF005312))
                                                  : (isDark ? Colors.grey[300] : Colors.grey[800]),
                                              fontWeight: FontWeight.bold,
                                              fontSize: 14,
                                            ),
                                          ),
                                        ),
                                        title: Text(
                                          p['name'] as String,
                                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                                        ),
                                        subtitle: Text(
                                          'Boarding Point: ${p['boardingPoint']}',
                                          style: TextStyle(fontSize: 12, color: Colors.grey[500]),
                                        ),
                                        trailing: InkWell(
                                          borderRadius: BorderRadius.circular(20),
                                          onTap: () {
                                            HapticFeedback.lightImpact();
                                            setState(() {
                                              _localStatusOverrides[seat] = isBoarded ? 'Pending' : 'Boarded';
                                            });
                                            ScaffoldMessenger.of(context).showSnackBar(
                                              SnackBar(
                                                content: Text(
                                                  '${p['name']} ($seat) marked as ${_localStatusOverrides[seat]}',
                                                ),
                                                duration: const Duration(seconds: 1),
                                              ),
                                            );
                                          },
                                          child: Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                                            decoration: BoxDecoration(
                                              color: isBoarded
                                                  ? const Color(0xFF005312)
                                                  : const Color(0xFFFEB300).withOpacity(0.2),
                                              borderRadius: BorderRadius.circular(16),
                                              border: Border.all(
                                                color: isBoarded
                                                    ? const Color(0xFF32DE84)
                                                    : const Color(0xFFFEB300),
                                              ),
                                            ),
                                            child: Row(
                                              mainAxisSize: MainAxisSize.min,
                                              children: [
                                                Icon(
                                                  isBoarded ? Icons.check_circle : Icons.hourglass_empty,
                                                  size: 14,
                                                  color: isBoarded ? const Color(0xFF32DE84) : const Color(0xFFFEB300),
                                                ),
                                                const SizedBox(width: 4),
                                                Text(
                                                  p['status'] as String,
                                                  style: TextStyle(
                                                    color: isBoarded ? Colors.white : (isDark ? const Color(0xFFFEB300) : const Color(0xFF8A5B00)),
                                                    fontSize: 12,
                                                    fontWeight: FontWeight.bold,
                                                  ),
                                                ),
                                              ],
                                            ),
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
