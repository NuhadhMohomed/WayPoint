import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:waypoint_mobile/features/journey/widgets/ai_insights_banner.dart';

void main() {
  testWidgets('AiInsightsBanner displays agent reasoning and match badge when active', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: AiInsightsBanner(
            agentReasoning: 'Selected EX-08 for shortest travel time and guaranteed AC seats.',
            isAiFallback: false,
          ),
        ),
      ),
    );

    expect(find.text('AI Recommendation Insights'), findsOneWidget);
    expect(find.textContaining('Selected EX-08 for shortest travel time'), findsOneWidget);
    expect(find.text('AI Verified'), findsOneWidget);
  });

  testWidgets('AiInsightsBanner displays fallback notice when isAiFallback is true', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: AiInsightsBanner(
            agentReasoning: '',
            isAiFallback: true,
          ),
        ),
      ),
    );

    expect(find.text('AI Offline — Safe Fallback'), findsOneWidget);
    expect(find.textContaining('Showing verified direct and connecting routes'), findsOneWidget);
  });
}
