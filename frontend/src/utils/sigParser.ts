import { SigGroupType } from './constants';

export interface ParsedSig {
  signatureId: string;
  group: string;
  type: SigGroupType;
  name: string | null;
  scanPercentage: number;
}

function mapGroupType(group: string, name: string): SigGroupType {
  const g = group.toLowerCase();
  const n = name.toLowerCase();
  if (g.includes('wormhole') || n.includes('wormhole') || n.includes('unstable')) return SigGroupType.Wormhole;
  if (g.includes('data') || n.includes('data')) return SigGroupType.Data;
  if (g.includes('relic') || n.includes('relic') || n.includes('ruins')) return SigGroupType.Relic;
  if (g.includes('gas') || n.includes('gas') || n.includes('cloud')) return SigGroupType.Gas;
  if (g.includes('combat') || n.includes('site') || n.includes('outpost') || n.includes('hub')) return SigGroupType.Combat;
  return SigGroupType.Unknown;
}

export function parseSigClipboard(text: string): ParsedSig[] {
  const lines = text.trim().split('\n');
  const results: ParsedSig[] = [];

  for (const line of lines) {
    const cols = line.split('\t');
    if (cols.length < 3) continue;

    const signatureId = cols[0]?.trim();
    const group = cols[1]?.trim() ?? '';
    const typeName = cols[2]?.trim() ?? '';
    const name = cols[3]?.trim() || null;
    const scanRaw = cols[4]?.trim() ?? '0%';

    if (!signatureId || group.toLowerCase().includes('anomaly')) continue;

    const scanPercentage = parseFloat(scanRaw.replace('%', '')) || 0;
    const type = mapGroupType(typeName || group, name ?? '');

    results.push({ signatureId, group, type, name: name || null, scanPercentage });
  }

  return results;
}
