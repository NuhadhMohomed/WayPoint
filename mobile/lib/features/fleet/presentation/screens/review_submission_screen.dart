import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import '../../../../core/constants/api_constants.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../../core/widgets/waypoint_button.dart';

class ReviewSubmissionScreen extends StatefulWidget {
  final String entityId;
  final String bookingId;
  final String entityType; // 'bus' or 'driver'
  final String entityName;
  final String? authToken;
  final http.Client? httpClient;

  const ReviewSubmissionScreen({
    super.key,
    required this.entityId,
    required this.bookingId,
    required this.entityType,
    required this.entityName,
    this.authToken,
    this.httpClient,
  });

  @override
  ReviewSubmissionScreenState createState() => ReviewSubmissionScreenState();
}

class ReviewSubmissionScreenState extends State<ReviewSubmissionScreen> {
  int _rating = 0;
  final _commentController = TextEditingController();
  final Set<String> _selectedTags = {};
  bool _isAnonymous = false;
  bool _isLoading = false;
  String? _errorMessage;

  static const List<String> _quickTags = [
    'Punctual',
    'Clean Vehicle',
    'Smooth Driving',
    'Polite Staff',
    'AC Working',
    'Safe Journey',
    'Scenic Route',
    'Spacious Seats',
  ];

  Future<void> _submitReview() async {
    if (_rating == 0) {
      setState(() => _errorMessage = 'Please select a star rating (1-5)');
      return;
    }

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final token = widget.authToken ?? await SecureStorageService.getToken();
      const baseUrl = ApiConstants.baseUrl;
      final isBus = widget.entityType.toLowerCase() == 'bus';
      final endpoint = isBus 
          ? '$baseUrl/reviews/buses' 
          : '$baseUrl/reviews/drivers';

      final userComment = _commentController.text.trim();
      final tagComment = _selectedTags.isNotEmpty ? '[Tags: ${_selectedTags.join(', ')}]' : '';
      final fullComment = [userComment, tagComment].where((s) => s.isNotEmpty).join(' ');

      final payload = isBus
          ? {
              'busId': widget.entityId,
              'bookingId': widget.bookingId,
              'rating': _rating,
              'comment': fullComment,
              'isAnonymous': _isAnonymous,
            }
          : {
              'driverId': widget.entityId,
              'bookingId': widget.bookingId,
              'rating': _rating,
              'comment': fullComment,
              'isAnonymous': _isAnonymous,
            };

      final client = widget.httpClient ?? http.Client();
      final response = await client.post(
        Uri.parse(endpoint),
        headers: {
          'Content-Type': 'application/json',
          if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
        },
        body: jsonEncode(payload),
      );

      if (response.statusCode == 200 || response.statusCode == 201) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Thank you! Review submitted successfully.'),
              backgroundColor: Color(0xFF005312),
            ),
          );
          Navigator.pop(context, true);
        }
      } else {
        String message = 'Failed to submit review';
        try {
          final decoded = jsonDecode(response.body);
          if (decoded is Map && decoded.containsKey('detail') && decoded['detail'] != null) {
            message = decoded['detail'].toString();
          } else if (decoded is Map && decoded.containsKey('message') && decoded['message'] != null) {
            message = decoded['message'].toString();
          }
        } catch (_) {}
        throw Exception(message);
      }
    } catch (e) {
      setState(() => _errorMessage = 'Could not submit review: $e');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: Text('Rate ${widget.entityType == 'bus' ? 'Transit Service' : 'Driver'}'),
        elevation: 0,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'How was your journey on ${widget.entityName}?',
              style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Your verified feedback helps fellow commuters and improves transit quality across Sri Lanka.',
              style: TextStyle(
                fontSize: 13,
                color: isDark ? Colors.grey[400] : Colors.grey[600],
              ),
            ),
            const SizedBox(height: 28),
            Center(
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(5, (index) {
                  final isFilled = index < _rating;
                  return IconButton(
                    iconSize: 44,
                    icon: Icon(
                      isFilled ? Icons.star_rounded : Icons.star_outline_rounded,
                      color: isFilled ? const Color(0xFFFEB300) : (isDark ? Colors.grey[700] : Colors.grey[300]),
                    ),
                    onPressed: () {
                      setState(() => _rating = index + 1);
                    },
                  );
                }),
              ),
            ),
            if (_rating > 0)
              Center(
                child: Padding(
                  padding: const EdgeInsets.only(top: 4.0),
                  child: Text(
                    switch (_rating) {
                      5 => 'Excellent! Highly recommended',
                      4 => 'Good service, pleasant ride',
                      3 => 'Average transit experience',
                      2 => 'Subpar service, needs improvement',
                      _ => 'Poor experience',
                    },
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: isDark ? const Color(0xFF32DE84) : const Color(0xFF005312),
                    ),
                  ),
                ),
              ),
            const SizedBox(height: 28),
            const Text(
              'Highlights (Tap all that apply)',
              style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: _quickTags.map((tag) {
                final isSelected = _selectedTags.contains(tag);
                return FilterChip(
                  label: Text(tag),
                  selected: isSelected,
                  selectedColor: const Color(0xFF32DE84).withOpacity(0.2),
                  checkmarkColor: isDark ? const Color(0xFF32DE84) : const Color(0xFF005312),
                  labelStyle: TextStyle(
                    fontSize: 12,
                    fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                    color: isSelected
                        ? (isDark ? const Color(0xFF32DE84) : const Color(0xFF005312))
                        : (isDark ? Colors.grey[300] : Colors.grey[800]),
                  ),
                  onSelected: (selected) {
                    setState(() {
                      if (selected) {
                        _selectedTags.add(tag);
                      } else {
                        _selectedTags.remove(tag);
                      }
                    });
                  },
                );
              }).toList(),
            ),
            const SizedBox(height: 24),
            const Text(
              'Write a review (optional)',
              style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _commentController,
              maxLines: 4,
              decoration: const InputDecoration(
                hintText: 'Share specifics about punctuality, comfort, stops...',
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.all(Radius.circular(12)),
                ),
              ),
            ),
            const SizedBox(height: 16),
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Post anonymously', style: TextStyle(fontSize: 14)),
              subtitle: const Text('Your profile name will not be publicly displayed', style: TextStyle(fontSize: 12)),
              value: _isAnonymous,
              activeColor: AppTheme.primaryColor,
              onChanged: (val) {
                setState(() => _isAnonymous = val ?? false);
              },
            ),
            if (_errorMessage != null) ...[
              const SizedBox(height: 16),
              Text(
                _errorMessage!,
                style: const TextStyle(color: AppTheme.errorColor, fontSize: 13),
              ),
            ],
            const SizedBox(height: 24),
            _isLoading
                ? const Center(child: CircularProgressIndicator(color: Color(0xFF32DE84)))
                : WayPointButton(
                    text: 'Submit Verified Review',
                    onPressed: _submitReview,
                  ),
          ],
        ),
      ),
    );
  }
}
