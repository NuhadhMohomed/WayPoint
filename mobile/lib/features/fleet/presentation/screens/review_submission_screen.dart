import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import '../../../../core/theme/app_theme.dart';
import '../../../../core/widgets/waypoint_button.dart';

class ReviewSubmissionScreen extends StatefulWidget {
  final String entityId;
  final String entityType; // 'bus' or 'driver'
  final String entityName;

  const ReviewSubmissionScreen({
    super.key,
    required this.entityId,
    required this.entityType,
    required this.entityName,
  });

  @override
  ReviewSubmissionScreenState createState() => ReviewSubmissionScreenState();
}

class ReviewSubmissionScreenState extends State<ReviewSubmissionScreen> {
  int _rating = 0;
  final _commentController = TextEditingController();
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
      const baseUrl = 'http://10.0.2.2:5000/api/v1';
      final endpoint = widget.entityType == 'bus' 
          ? '$baseUrl/reviews/bus' 
          : '$baseUrl/reviews/driver';

      final response = await http.post(
        Uri.parse(endpoint),
        headers: {
          'Content-Type': 'application/json',
          // 'Authorization': 'Bearer <token>', // Add auth token here in real app
        },
        body: jsonEncode({
          'entityId': widget.entityId,
          'rating': _rating,
          'comment': _commentController.text.trim(),
        }),
      );

      if (response.statusCode == 200 || response.statusCode == 201) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Review submitted successfully!')),
          );
          Navigator.pop(context, true); // Return true to indicate success
        }
      } else {
        throw Exception('Failed to submit review');
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
            if (_errorMessage != null) ...[
              const SizedBox(height: 16),
              Text(
                _errorMessage!,
                style: const TextStyle(color: AppTheme.errorColor),
              ),
            ],
            const SizedBox(height: 32),
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
