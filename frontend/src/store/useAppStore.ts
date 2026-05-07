import { create } from 'zustand';

interface User {
  id: number;
  characterName: string;
  primaryCharacterId: number;
  role: 'Admin' | 'Member' | 'ReadOnly';
}

interface AppState {
  user: User | null;
  activeCharacterId: number | null;
  selectedSystemId: number | null;
  selectedConnectionId: number | null;
  sigPanelOpen: boolean;
  massPanelOpen: boolean;
  sidebarCollapsed: boolean;

  setUser: (user: User | null) => void;
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

  setUser: (user) => set({ user }),
  setActiveCharacter: (id) => set({ activeCharacterId: id }),
  selectSystem: (id) =>
    set({ selectedSystemId: id, sigPanelOpen: id !== null, selectedConnectionId: null, massPanelOpen: false }),
  selectConnection: (id) =>
    set({ selectedConnectionId: id, massPanelOpen: id !== null, selectedSystemId: null, sigPanelOpen: false }),
  setSigPanelOpen: (open) => set({ sigPanelOpen: open }),
  setMassPanelOpen: (open) => set({ massPanelOpen: open }),
  toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
}));
