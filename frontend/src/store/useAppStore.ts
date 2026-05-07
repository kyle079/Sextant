import { create } from 'zustand';
import type { MeResponse } from '../api/types';

interface AppState {
  user: MeResponse | null;
  activeCharacterId: number | null;
  selectedSystemId: number | null;
  selectedConnectionId: number | null;
  sigPanelOpen: boolean;
  massPanelOpen: boolean;
  sidebarCollapsed: boolean;

  setUser: (user: MeResponse | null) => void;
  setActiveCharacter: (id: number) => void;
  selectSystem: (id: number | null) => void;
  selectConnection: (id: number | null) => void;
  setSigPanelOpen: (open: boolean) => void;
  setMassPanelOpen: (open: boolean) => void;
  toggleSidebar: () => void;
}

export const useAppStore = create<AppState>((set) => ({
  user: null,
  activeCharacterId: null,
  selectedSystemId: null,
  selectedConnectionId: null,
  sigPanelOpen: false,
  massPanelOpen: false,
  sidebarCollapsed: false,

  setUser: (user) =>
    set({ user, activeCharacterId: user?.primaryCharacterId ?? null }),
  setActiveCharacter: (id) => set({ activeCharacterId: id }),
  selectSystem: (id) =>
    set({ selectedSystemId: id, sigPanelOpen: id !== null, selectedConnectionId: null, massPanelOpen: false }),
  selectConnection: (id) =>
    set({ selectedConnectionId: id, massPanelOpen: id !== null, selectedSystemId: null, sigPanelOpen: false }),
  setSigPanelOpen: (open) => set({ sigPanelOpen: open }),
  setMassPanelOpen: (open) => set({ massPanelOpen: open }),
  toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
}));
