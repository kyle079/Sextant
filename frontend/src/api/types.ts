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
