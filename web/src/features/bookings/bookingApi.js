import { apiClient } from '@/api/client'

const unwrap = (res) => (res && res.data !== undefined && !res.items && !Array.isArray(res) ? res.data : res)

export const bookingApi = {
  holdSeat: async (holdData) => unwrap(await apiClient.post('/bookings/hold', holdData)),
  releaseHold: async (holdId) => unwrap(await apiClient.delete(`/bookings/hold/${holdId}`)),
  confirmPayment: async (chargeData) => unwrap(await apiClient.post('/payments/confirm-sandbox-charge', chargeData)),
  getBookings: async (params) => unwrap(await apiClient.get('/bookings', { params })),
  getBookingById: async (id) => unwrap(await apiClient.get(`/bookings/${id}`)),
  checkout: async (checkoutData) => unwrap(await apiClient.post('/bookings/checkout', checkoutData)),
  cancelBooking: async (cancelData) => unwrap(await apiClient.post('/bookings/cancel', cancelData)),
  verifyTicketQr: async (qrCodePayload) => unwrap(await apiClient.post('/tickets/verify', { qrCodePayload })),
}
