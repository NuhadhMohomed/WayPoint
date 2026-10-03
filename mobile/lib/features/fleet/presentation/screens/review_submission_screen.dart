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
  bool _isAnonymous = false;
  bool _isLoading = false;
  String? _errorMessage;

  Future<void> _submitReview() async {
    if (_rating == 0) {
      setState(() => _errorMessage = 'Please select a rating');
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

      final payload = isBus
          ? {
              'busId': widget.entityId,
              'bookingId': widget.bookingId,
              'rating': _rating,
              'comment': _commentController.text.trim(),
              'isAnonymous': _isAnonymous,
            }
          : {
              'driverId': widget.entityId,
              'bookingId': widget.bookingId,
              'rating': _rating,
              'comment': _commentController.text.trim(),
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
            const SnackBar(content: Text('Review submitted successfully!')),
          );
          Navigator.pop(context, true); // Return true to indicate success
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
    return Scaffold(
      appBar: AppBar(
        title: Text('Rate ${widget.entityType == 'bus' ? 'Bus' : 'Driver'}'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'How was your experience with ${widget.entityName}?',
              style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 32),
            Center(
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(5, (index) {
                  return IconButton(
                    iconSize: 48,
                    icon: Icon(
                      index < _rating ? Icons.star : Icons.star_border,
                      color: index < _rating ? AppTheme.secondaryColor : Colors.grey,
                    ),
                    onPressed: () {
                      setState(() => _rating = index + 1);
                    },
                  );
                }),
              ),
            ),
            const SizedBox(height: 32),
            const Text(
              'Write a review (optional)',
              style: TextStyle(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _commentController,
              maxLines: 4,
              decoration: const InputDecoration(
                hintText: 'Share details of your experience...',
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.all(Radius.circular(12)),
                ),
              ),
            ),
            const SizedBox(height: 16),
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Post anonymously'),
              subtitle: const Text('Your name will not be shown to other passengers or operators'),
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
                style: const TextStyle(color: AppTheme.errorColor),
              ),
            ],
            const SizedBox(height: 24),
            _isLoading
                ? const Center(child: CircularProgressIndicator())
                : WayPointButton(
                    text: 'Submit Review',
                    onPressed: _submitReview,
                  ),
          ],
        ),
      ),
    );
  }
}
