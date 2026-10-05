import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/widgets/waypoint_button.dart';
import '../../../core/widgets/waypoint_logo.dart';
import '../bloc/auth_cubit.dart';
import '../bloc/auth_state.dart';

class PassengerAuthScreen extends StatefulWidget {
  const PassengerAuthScreen({super.key});

  @override
  State<PassengerAuthScreen> createState() => _PassengerAuthScreenState();
}

class _PassengerAuthScreenState extends State<PassengerAuthScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  // Login controllers
  final _loginEmailController = TextEditingController();
  final _loginPasswordController = TextEditingController();
  final _loginFormKey = GlobalKey<FormState>();
  bool _loginObscure = true;

  // Register controllers
  final _regNameController = TextEditingController();
  final _regEmailController = TextEditingController();
  final _regPhoneController = TextEditingController();
  final _regPasswordController = TextEditingController();
  final _regFormKey = GlobalKey<FormState>();
  bool _regObscure = true;
  String _selectedRole = 'Passenger';

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    _loginEmailController.dispose();
    _loginPasswordController.dispose();
    _regNameController.dispose();
    _regEmailController.dispose();
    _regPhoneController.dispose();
    _regPasswordController.dispose();
    super.dispose();
  }

  void _fillDemoLogin(String email, String password) {
    _loginEmailController.text = email;
    _loginPasswordController.text = password;
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: isDark ? const Color(0xFF0A1B17) : const Color(0xFFF4F7F5),
      body: SafeArea(
        child: BlocConsumer<AuthCubit, AuthState>(
          listener: (context, state) {
            if (state is AuthError) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(state.message),
                  backgroundColor: AppTheme.errorColor,
                  behavior: SnackBarBehavior.floating,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
              );
            }
          },
          builder: (context, state) {
            final isLoading = state is AuthLoading;

            return SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const SizedBox(height: 16),
                  Center(
                    child: WayPointLogo(
                      size: 64,
                      variant: WayPointLogoVariant.stacked,
                      isDark: isDark,
                    ),
                  ),
                  const SizedBox(height: 28),

                  // Tab switcher (Sign In vs Register)
                  Container(
                    decoration: BoxDecoration(
                      color: isDark ? const Color(0xFF132B25) : Colors.grey[200],
                      borderRadius: BorderRadius.circular(16),
                    ),
                    padding: const EdgeInsets.all(4),
                    child: TabBar(
                      controller: _tabController,
                      indicator: BoxDecoration(
                        color: isDark ? AppTheme.primaryColor : Colors.white,
                        borderRadius: BorderRadius.circular(12),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withOpacity(0.06),
                            blurRadius: 8,
                            offset: const Offset(0, 2),
                          ),
                        ],
                      ),
                      labelColor: isDark ? Colors.white : AppTheme.primaryColor,
                      unselectedLabelColor: Colors.grey,
                      labelStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 14),
                      indicatorSize: TabBarIndicatorSize.tab,
                      dividerColor: Colors.transparent,
                      tabs: const [
                        Tab(text: 'Sign In'),
                        Tab(text: 'Register'),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Tab content
                  SizedBox(
                    height: 460,
                    child: TabBarView(
                      controller: _tabController,
                      children: [
                        // 1. Sign In View
                        _buildSignInView(context, isLoading, isDark),
                        // 2. Register View
                        _buildRegisterView(context, isLoading, isDark),
                      ],
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }

  Widget _buildSignInView(BuildContext context, bool isLoading, bool isDark) {
    return Form(
      key: _loginFormKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            key: const Key('login_email_field'),
            controller: _loginEmailController,
            keyboardType: TextInputType.emailAddress,
            decoration: InputDecoration(
              labelText: 'Email Address',
              hintText: 'passenger@waypoint.lk',
              prefixIcon: const Icon(Icons.email_outlined),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)),
            ),
            validator: (v) {
              if (v == null || v.trim().isEmpty) return 'Email is required';
              if (!v.contains('@')) return 'Enter a valid email';
              return null;
            },
          ),
          const SizedBox(height: 16),
          TextFormField(
            key: const Key('login_password_field'),
            controller: _loginPasswordController,
            obscureText: _loginObscure,
            decoration: InputDecoration(
              labelText: 'Password',
              hintText: '••••••••',
              prefixIcon: const Icon(Icons.lock_outline),
              suffixIcon: IconButton(
                icon: Icon(_loginObscure ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                onPressed: () => setState(() => _loginObscure = !_loginObscure),
              ),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)),
            ),
            validator: (v) {
              if (v == null || v.isEmpty) return 'Password is required';
              return null;
            },
          ),
          const SizedBox(height: 20),
          WayPointButton(
            key: const Key('login_submit_button'),
            text: 'Sign In to WayPoint',
            icon: Icons.login_rounded,
            isLoading: isLoading,
            onPressed: () {
              if (_loginFormKey.currentState?.validate() ?? false) {
                context.read<AuthCubit>().login(
                      _loginEmailController.text,
                      _loginPasswordController.text,
                    );
              }
            },
          ),
          const SizedBox(height: 20),
          const Divider(),
          const SizedBox(height: 8),
          Text(
            'Quick Test Accounts:',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: isDark ? Colors.grey[400] : Colors.grey[700],
            ),
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            children: [
              ActionChip(
                label: const Text('Passenger', style: TextStyle(fontSize: 11)),
                avatar: const Icon(Icons.person, size: 14),
                onPressed: () => _fillDemoLogin('passenger@waypoint.lk', 'Password123!'),
              ),
              ActionChip(
                label: const Text('Conductor', style: TextStyle(fontSize: 11)),
                avatar: const Icon(Icons.qr_code_scanner, size: 14),
                onPressed: () => _fillDemoLogin('conductor@waypoint.lk', 'Password123!'),
              ),
              ActionChip(
                label: const Text('Admin', style: TextStyle(fontSize: 11)),
                avatar: const Icon(Icons.shield, size: 14),
                onPressed: () => _fillDemoLogin('admin@waypoint.lk', 'Password123!'),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildRegisterView(BuildContext context, bool isLoading, bool isDark) {
    return Form(
      key: _regFormKey,
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              key: const Key('reg_name_field'),
              controller: _regNameController,
              decoration: InputDecoration(
                labelText: 'Full Name',
                hintText: 'Kasun Bandara',
                prefixIcon: const Icon(Icons.person_outline),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)),
              ),
              validator: (v) => (v == null || v.trim().isEmpty) ? 'Name is required' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              key: const Key('reg_email_field'),
              controller: _regEmailController,
              keyboardType: TextInputType.emailAddress,
              decoration: InputDecoration(
                labelText: 'Email Address',
                hintText: 'kasun@gmail.com',
                prefixIcon: const Icon(Icons.email_outlined),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)),
              ),
              validator: (v) {
                if (v == null || v.trim().isEmpty) return 'Email is required';
                if (!v.contains('@')) return 'Enter a valid email';
                return null;
              },
            ),
            const SizedBox(height: 12),
            TextFormField(
              key: const Key('reg_phone_field'),
              controller: _regPhoneController,
              keyboardType: TextInputType.phone,
              decoration: InputDecoration(
                labelText: 'Phone Number (SMS alerts)',
                hintText: '+94 77 123 4567',
                prefixIcon: const Icon(Icons.phone_outlined),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)),
              ),
            ),
            const SizedBox(height: 12),
            TextFormField(
              key: const Key('reg_password_field'),
              controller: _regPasswordController,
              obscureText: _regObscure,
              decoration: InputDecoration(
                labelText: 'Password (min 6 chars)',
                prefixIcon: const Icon(Icons.lock_outline),
                suffixIcon: IconButton(
                  icon: Icon(_regObscure ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                  onPressed: () => setState(() => _regObscure = !_regObscure),
                ),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)),
              ),
              validator: (v) {
                if (v == null || v.length < 6) return 'At least 6 characters required';
                return null;
              },
            ),
            const SizedBox(height: 16),
            WayPointButton(
              key: const Key('reg_submit_button'),
              text: 'Create Passenger Account',
              icon: Icons.person_add_rounded,
              isLoading: isLoading,
              onPressed: () {
                if (_regFormKey.currentState?.validate() ?? false) {
                  context.read<AuthCubit>().register(
                        email: _regEmailController.text,
                        password: _regPasswordController.text,
                        fullName: _regNameController.text,
                        phoneNumber: _regPhoneController.text,
                        role: _selectedRole,
                      );
                }
              },
            ),
          ],
        ),
      ),
    );
  }
}
