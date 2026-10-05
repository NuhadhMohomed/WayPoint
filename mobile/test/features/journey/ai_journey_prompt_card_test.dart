import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/journey/widgets/ai_journey_prompt_card.dart';

void main() {
  testWidgets('AiJourneyPromptCard renders textfield, preset chips, and triggers search', (WidgetTester tester) async {
    String? submittedObjective;
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: AiJourneyPromptCard(
          isSearching: false,
          onSubmit: (objective) => submittedObjective = objective,
        ),
      ),
    ));

    expect(find.text('AI Trip Planner'), findsOneWidget);
    expect(find.byType(TextField), findsOneWidget);
    expect(find.textContaining('Scenic route to Ella'), findsOneWidget);

    // Tap preset chip
    await tester.tap(find.textContaining('Scenic route to Ella'));
    await tester.pump();

    // Verify text field populated
    expect(find.text('Scenic route to Ella with AC'), findsOneWidget);

    // Tap Ask AI button
    await tester.tap(find.text('Ask AI'));
    await tester.pump();

    expect(submittedObjective, equals('Scenic route to Ella with AC'));
  });

  testWidgets('AiJourneyPromptCard shows loading state when isSearching is true', (WidgetTester tester) async {
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: AiJourneyPromptCard(
          isSearching: true,
          onSubmit: (_) {},
        ),
      ),
    ));

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });
}
