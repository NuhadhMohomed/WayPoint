import 'package:flutter/material.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../../core/widgets/waypoint_button.dart';
import '../../../../core/widgets/waypoint_logo.dart';
import '../../../../core/storage/local_cache_service.dart';

class OnboardingScreen extends StatefulWidget {
  final VoidCallback? onFinish;

  const OnboardingScreen({super.key, this.onFinish});

  @override
  State<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends State<OnboardingScreen> {
  final PageController _pageController = PageController();
  int _currentPage = 0;
  final LocalCacheService _cacheService = LocalCacheService();

  final List<Map<String, dynamic>> _pages = [
    {
      'title': 'Explore Sri Lanka',
      'subtitle': 'Discover express and luxury bus services connecting Colombo to Ella, Kandy, Galle, and Jaffna with live schedules.',
      'icon': Icons.map_rounded,
    },
    {
      'title': 'Live 10-Minute Seat Holds',
      'subtitle': 'Pick your preferred window or aisle seats in real time. Enjoy guaranteed holds while you complete checkout without rush.',
      'icon': Icons.event_seat_rounded,
    },
    {
      'title': 'Digital QR Boarding Passes',
      'subtitle': 'Scan and ride effortlessly with cryptographic offline passes. No paper tickets needed, even without cellular reception.',
      'icon': Icons.qr_code_2_rounded,
    },
  ];

  Future<void> _completeOnboarding() async {
    if (widget.onFinish != null) {
      widget.onFinish!();
    }
    try {
      await _cacheService.setFirstRunCompleted(true);
    } catch (_) {}
    if (widget.onFinish == null && mounted) {
      Navigator.of(context).pushReplacementNamed('/auth');
    }
  }

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: isDark ? AppTheme.darkBackground : AppTheme.lightBackground,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 16.0),
          child: Column(
            children: [
              // Top Bar with Brand and Skip
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const WayPointLogo(size: 36, showText: true),
                  TextButton(
                    onPressed: _completeOnboarding,
                    child: Text(
                      'Skip',
                      style: TextStyle(
                        color: isDark ? AppTheme.textMutedDark : AppTheme.textMutedLight,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ),
              const Spacer(),

              // Page Content Carousel
              SizedBox(
                height: 380,
                child: PageView.builder(
                  controller: _pageController,
                  itemCount: _pages.length,
                  onPageChanged: (index) => setState(() => _currentPage = index),
                  itemBuilder: (context, index) {
                    final item = _pages[index];
                    return Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Container(
                          width: 120,
                          height: 120,
                          decoration: BoxDecoration(
                            color: (isDark ? AppTheme.primaryContainerDark : AppTheme.primaryContainerLight).withOpacity(0.8),
                            shape: BoxShape.circle,
                          ),
                          child: Center(
                            child: Icon(
                              item['icon'] as IconData,
                              size: 56,
                              color: isDark ? AppTheme.primaryColor : const Color(0xFF042611),
                            ),
                          ),
                        ),
                        const SizedBox(height: 36),
                        Text(
                          item['title'] as String,
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.bold,
                            color: isDark ? AppTheme.textPrimaryDark : AppTheme.textPrimaryLight,
                            letterSpacing: -0.5,
                          ),
                        ),
                        const SizedBox(height: 14),
                        Text(
                          item['subtitle'] as String,
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 15,
                            color: isDark ? AppTheme.textMutedDark : AppTheme.textMutedLight,
                            height: 1.5,
                          ),
                        ),
                      ],
                    );
                  },
                ),
              ),

              const Spacer(),

              // Dot Indicators
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(
                  _pages.length,
                  (index) => AnimatedContainer(
                    duration: const Duration(milliseconds: 250),
                    margin: const EdgeInsets.symmetric(horizontal: 4),
                    width: _currentPage == index ? 24 : 8,
                    height: 8,
                    decoration: BoxDecoration(
                      color: _currentPage == index
                          ? AppTheme.primaryColor
                          : (isDark ? AppTheme.darkBorderColor : AppTheme.lightBorderColor),
                      borderRadius: BorderRadius.circular(4),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 32),

              // Bottom Button
              WayPointButton(
                text: _currentPage == _pages.length - 1 ? 'Get Started' : 'Next',
                onPressed: () {
                  if (_currentPage == _pages.length - 1) {
                    _completeOnboarding();
                  } else {
                    _pageController.nextPage(
                      duration: const Duration(milliseconds: 300),
                      curve: Curves.easeInOut,
                    );
                  }
                },
              ),
              const SizedBox(height: 12),
            ],
          ),
        ),
      ),
    );
  }
}
