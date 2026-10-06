import { create } from 'zustand'

export const useFleetStore = create((set) => ({
  // Bus state
  buses: [],
  busesLoading: false,
  busPagination: { pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 },

  // Driver state
  drivers: [],
  driversLoading: false,
  driverPagination: { pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 },

  // Seat layout state
  seatLayouts: [],
  layoutsLoading: false,

  // Active tab
  activeTab: 'buses',

  // Actions
  setBuses: (data) => {
    const items = Array.isArray(data) ? data : (data?.items || [])
    set({
      buses: items,
      busPagination: {
        pageNumber: data?.pageNumber || 1,
        pageSize: data?.pageSize || 20,
        totalCount: data?.totalCount ?? items.length,
        totalPages: data?.totalPages || 1,
      },
    })
  },
  setBusesLoading: (loading) => set({ busesLoading: loading }),

  setDrivers: (data) => {
    const items = Array.isArray(data) ? data : (data?.items || [])
    set({
      drivers: items,
      driverPagination: {
        pageNumber: data?.pageNumber || 1,
        pageSize: data?.pageSize || 20,
        totalCount: data?.totalCount ?? items.length,
        totalPages: data?.totalPages || 1,
      },
    })
  },
  setDriversLoading: (loading) => set({ driversLoading: loading }),

  setSeatLayouts: (layouts) => {
    const items = Array.isArray(layouts) ? layouts : (layouts?.items || [])
    set({ seatLayouts: items })
  },
  setLayoutsLoading: (loading) => set({ layoutsLoading: loading }),

  setActiveTab: (tab) => set({ activeTab: tab }),
}))
