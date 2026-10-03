# Auth Feature

Handles passenger authentication, onboarding, and biometric setup.

## Components
- **Screens**: `passenger_auth_screen.dart` - Main entry point containing tabs for Login and Register.
- **Widgets**: `login_form.dart` and `register_form.dart` - Forms for their respective actions.
- **Services**: `auth_service.dart` - Communicates with the backend API (`/api/v1/auth/login`, `/api/v1/auth/register`).
- **Models**: `auth_models.dart` - Data structures for auth requests and responses.
