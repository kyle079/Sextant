export const UserRole = {
  Admin: 'Admin',
  Member: 'Member',
  ReadOnly: 'ReadOnly',
} as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const ActivityEventType = {
  Sig: 'Sig',
  Chain: 'Chain',
  Mass: 'Mass',
  Rolling: 'Rolling',
  PI: 'PI',
  Kill: 'Kill',
  Fit: 'Fit',
  Structure: 'Structure',
  Site: 'Site',
  Auth: 'Auth',
  System: 'System',
} as const;
export type ActivityEventType = (typeof ActivityEventType)[keyof typeof ActivityEventType];

export const SigGroupType = {
  Wormhole: 'Wormhole',
  Data: 'Data',
  Relic: 'Relic',
  Gas: 'Gas',
  Combat: 'Combat',
  Unknown: 'Unknown',
} as const;
export type SigGroupType = (typeof SigGroupType)[keyof typeof SigGroupType];

export const MassStatus = {
  Fresh: 'Fresh',
  Half: 'Half',
  Critical: 'Critical',
} as const;
export type MassStatus = (typeof MassStatus)[keyof typeof MassStatus];

export const LifeStatus = {
  Stable: 'Stable',
  EOL: 'EOL',
} as const;
export type LifeStatus = (typeof LifeStatus)[keyof typeof LifeStatus];

export const MASS_HALF_THRESHOLD = 0.5;
export const MASS_CRITICAL_THRESHOLD = 0.8;
export const EOL_WARNING_HOURS = 4;

export const WH_CLASS_COLORS: Record<number, string> = {
  1: '#4dabf7',
  2: '#69db7c',
  3: '#ffd43b',
  4: '#ff8787',
  5: '#cc5de8',
  6: '#f06595',
};

export const EFFECT_COLORS: Record<string, string> = {
  Pulsar: '#74c0fc',
  'Wolf-Rayet': '#f03e3e',
  Magnetar: '#f08c00',
  'Black Hole': '#495057',
  Cataclysmic: '#f76707',
  'Red Giant': '#e03131',
};
