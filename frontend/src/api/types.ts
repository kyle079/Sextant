export interface CharacterInfo {
  characterId: number;
  characterName: string;
  corporationId: number;
  isActive: boolean;
  portraitUrl: string;
}

export interface MeResponse {
  id: number;
  characterName: string;
  primaryCharacterId: number;
  role: 'Admin' | 'Member' | 'ReadOnly';
  characters: CharacterInfo[];
}


export interface SdeStatusResponse {
  lastRefreshStartedAt?: string | null;
  lastRefreshCompletedAt?: string | null;
  state: string;
  wormholeSystemCount: number;
  wormholeTypeCount: number;
  error?: string | null;
}
