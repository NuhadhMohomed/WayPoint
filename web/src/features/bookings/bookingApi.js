import { apiClient } from '@/api/client'

export const bookingApi = {
  holdSeat: async (holdData) => {
    const res = await apiClient.post('/bookings/hold', holdData)
    return res.data
  },
  releaseHold: async (holdId) => {
    const res = await apiClient.delete(`/bookings/hold/${holdId}`)
    return res.data
  },
  confirmPayment: async (chargeData) => {
    const res = await apiClient.post('/payments/sandbox-charge', chargeData)
    return res.data
  },
  getBookings: async (params) => {
    const res = await apiClient.get('/bookings', { params })
    return res.data
  },
  getBookingById: async (id) => {
    const res = await apiClient.get(`/bookings/${id}`)
    return res.data
  },
  checkout: async (checkoutData) => {
    const res = await apiClient.post('/bookings/checkout', checkoutData)
    return res.data
  },
  cancelBooking: async (cancelData) => {
    const res = await apiClient.post('/bookings/cancel', cancelData)
    return res.data
  },
  verifyTicketQr: async (qrCodePayload) => {
    const res = await apiClient.post('/tickets/verify-qr', { qrCodePayload })
    return res.data
  },
}
