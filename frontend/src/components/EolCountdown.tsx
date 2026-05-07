import { useEffect, useState } from 'react';
import { Badge } from '@mantine/core';
import { EOL_WARNING_HOURS } from '../utils/constants';

interface Props {
  eolAt: string | null;
}

function formatDuration(ms: number): string {
  if (ms <= 0) return 'EOL';
  const h = Math.floor(ms / 3_600_000);
  const m = Math.floor((ms % 3_600_000) / 60_000);
  return `${h}h ${m}m`;
}

export function EolCountdown({ eolAt }: Props) {
  const [now, setNow] = useState(Date.now());

  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 60_000);
    return () => clearInterval(id);
  }, []);

  if (!eolAt) return null;

  const remaining = new Date(eolAt).getTime() - now;
  const hoursLeft = remaining / 3_600_000;
  const isWarning = hoursLeft < EOL_WARNING_HOURS;
  const isExpired = remaining <= 0;

  return (
    <Badge color={isExpired ? 'red' : isWarning ? 'orange' : 'gray'} variant="outline" size="sm">
      {formatDuration(remaining)}
    </Badge>
  );
}
