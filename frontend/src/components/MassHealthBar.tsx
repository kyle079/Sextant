import { Stack, Text, Progress, Group } from '@mantine/core';

interface Props {
  consumed: number;
  max: number;
}

export function MassHealthBar({ consumed, max }: Props) {
  const pct = max > 0 ? (consumed / max) * 100 : 0;
  const color = pct >= 80 ? 'red' : pct >= 50 ? 'yellow' : 'green';

  const fmt = (n: number) => `${(n / 1_000_000).toFixed(0)}M`;

  return (
    <Stack gap={4}>
      <Group justify="space-between">
        <Text size="xs" c="dimmed">Mass consumed</Text>
        <Text size="xs" fw={600}>{fmt(consumed)} / {fmt(max)} kg</Text>
      </Group>
      <Progress value={pct} color={color} animated={pct >= 80} size="lg" radius="xs" />
    </Stack>
  );
}
