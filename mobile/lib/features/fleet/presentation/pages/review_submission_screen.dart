import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/widgets/waypoint_button.dart';
import '../../../../core/widgets/waypoint_card.dart';
import '../bloc/review_bloc.dart';
import '../bloc/review_event.dart';
import '../bloc/review_state.dart';

class ReviewSubmissionScreen extends StatefulWidget {
  final String bookingId;
  final String busId;
  final String busRegistration;
  final String driverId;
  final String driverName;

  const ReviewSubmissionScreen({
    super.key,
    required this.bookingId,
    required this.busId,
    required this.busRegistration,
    required this.driverId,
    required this.driverName,
  });

  @override
  State<ReviewSubmissionScreen> createState() => _ReviewSubmissionScreenState();
}

class _ReviewSubmissionScreenState extends State<ReviewSubmissionScreen> {
  int _busRating = 0;
  String _busComment = '';
  bool _busAnonymous = false;
  
  int _driverRating = 0;
  String _driverComment = '';
  bool _driverAnonymous = false;

  void _submitReviews() {
    final bloc = context.read<ReviewBloc>();
    
    if (_busRating > 0) {
      bloc.add(SubmitBusReview(
        busId: widget.busId,
        bookingId: widget.bookingId,
        rating: _busRating,
        comment: _busComment.isNotEmpty ? _busComment : null,
        isAnonymous: _busAnonymous,
      ));
    }

    if (_driverRating > 0) {
      bloc.add(SubmitDriverReview(
        driverId: widget.driverId,
        bookingId: widget.bookingId,
        rating: _driverRating,
        comment: _driverComment.isNotEmpty ? _driverComment : null,
        isAnonymous: _driverAnonymous,
      ));
    }
  }

  Widget _buildStarRating(int currentRating, ValueChanged<int> onRatingChanged) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: List.generate(5, (index) {
        return IconButton(
          icon: Icon(
            index < currentRating ? Icons.star : Icons.star_border,
            color: index < currentRating ? Colors.amber : Colors.grey,
            size: 32,
          ),
          onPressed: () => onRatingChanged(index + 1),
        );
      }),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Review Trip'),
      ),
      body: BlocConsumer<ReviewBloc, ReviewState>(
        listener: (context, state) {
          if (state is ReviewSubmittedSuccess) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.message)),
            );
            // In a real flow, we'd only pop when both are submitted or handle state per-review
            Navigator.of(context).pop();
          } else if (state is ReviewSubmissionError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.errorMessage, style: const TextStyle(color: Colors.white)), backgroundColor: Colors.red),
            );
          }
        },
        builder: (context, state) {
          final isLoading = state is ReviewSubmitting;
          return SingleChildScrollView(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                WayPointCard(
                  child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Rate Bus: ${widget.busRegistration}', style: Theme.of(context).textTheme.titleMedium),
                        const SizedBox(height: 16),
                        _buildStarRating(_busRating, (val) => setState(() => _busRating = val)),
                        TextField(
                          decoration: const InputDecoration(
                            labelText: 'Comment (Optional)',
                            border: OutlineInputBorder(),
                          ),
                          maxLines: 3,
                          onChanged: (val) => _busComment = val,
                        ),
                        Row(
                          children: [
                            Checkbox(
                              value: _busAnonymous,
                              onChanged: (val) => setState(() => _busAnonymous = val ?? false),
                            ),
                            const Text('Submit Anonymously'),
                          ],
                        )
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),
                WayPointCard(
                  child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Rate Driver: ${widget.driverName}', style: Theme.of(context).textTheme.titleMedium),
                        const SizedBox(height: 16),
                        _buildStarRating(_driverRating, (val) => setState(() => _driverRating = val)),
                        TextField(
                          decoration: const InputDecoration(
                            labelText: 'Comment (Optional)',
                            border: OutlineInputBorder(),
                          ),
                          maxLines: 3,
                          onChanged: (val) => _driverComment = val,
                        ),
                        Row(
                          children: [
                            Checkbox(
                              value: _driverAnonymous,
                              onChanged: (val) => setState(() => _driverAnonymous = val ?? false),
                            ),
                            const Text('Submit Anonymously'),
                          ],
                        )
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 24),
                WayPointButton(
                  text: 'Submit Reviews',
                  onPressed: (_busRating > 0 || _driverRating > 0) && !isLoading
                      ? _submitReviews
                      : null, // Need at least one rating to submit
                ),
                if (isLoading)
                  const Padding(
                    padding: EdgeInsets.only(top: 16.0),
                    child: Center(child: CircularProgressIndicator()),
                  )
              ],
            ),
          );
        },
      ),
    );
  }
}
