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
  setBuses: (data) => set({
    buses: data.items,
    busPagination: {
      pageNumber: data.pageNumber,
      pageSize: data.pageSize,
      totalCount: data.totalCount,
      totalPages: data.totalPages,
    },
  }),
  setBusesLoading: (loading) => set({ busesLoading: loading }),

  setDrivers: (data) => set({
    drivers: data.items,
    driverPagination: {
      pageNumber: data.pageNumber,
      pageSize: data.pageSize,
      totalCount: data.totalCount,
      totalPages: data.totalPages,
    },
  }),
  setDriversLoading: (loading) => set({ driversLoading: loading }),

  setSeatLayouts: (layouts) => set({ seatLayouts: layouts }),
  setLayoutsLoading: (loading) => set({ layoutsLoading: loading }),

  setActiveTab: (tab) => set({ activeTab: tab }),
}))
