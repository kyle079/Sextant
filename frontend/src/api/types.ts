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

export interface ChainSystemResponse {
  id: number;
  solarSystemId: number;
  name: string;
  class: string;
  classNumber?: number | null;
  effect?: string | null;
  statics: string[];
  notes: string;
  isHome: boolean;
  sigCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ChainConnectionResponse {
  id: number;
  sourceSystemId: number;
  targetSystemId: number;
  wormholeTypeCode: string;
  status: string;
  massStatus: 'Fresh' | 'Half' | 'Critical';
  maxMassKg?: number | null;
  maxJumpMassKg?: number | null;
  lifetimeHours?: number | null;
  scannedAt: string;
  eolAt?: string | null;
  signatureId?: string | null;
  notes: string;
  createdAt: string;
  updatedAt: string;
}

export interface WormholeSystemSearchResponse {
  id: number;
  solarSystemId: number;
  name: string;
  secondaryName?: string | null;
  class: string;
  classNumber?: number | null;
  effect?: string | null;
  statics: string[];
  isShattered: boolean;
}

export interface WormholeTypeResponse {
  id: number;
  code: string;
  destinationType?: string | null;
  destinationClass?: number | null;
  lifetimeHours: number;
  maxMassKg?: number | null;
  maxJumpMassKg?: number | null;
  scanStrength?: number | null;
}

export interface SignatureResponse {
  id: number;
  chainSystemId: number;
  signatureId: string;
  group: string;
  type: 'Wormhole' | 'Data' | 'Relic' | 'Gas' | 'Combat' | 'Unknown';
  name?: string | null;
  scanPercentage: number;
  scannedByCharacterId: number;
  scannedByCharacterName: string;
  eolAt?: string | null;
  createdAt: string;
  updatedAt: string;
}
