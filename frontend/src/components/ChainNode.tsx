import { Handle, Position, type NodeProps } from 'reactflow';
import { Paper, Text, Badge, Stack, Group } from '@mantine/core';
import { WH_CLASS_COLORS } from '../utils/constants';

export interface ChainNodeData {
  systemName: string;
  wormholeClass?: number;
  effect?: string;
  sigCount?: number;
  isHome?: boolean;
}

export function ChainNode({ data }: NodeProps<ChainNodeData>) {
  const classColor = data.wormholeClass ? WH_CLASS_COLORS[data.wormholeClass] : '#868e96';

  return (
    <>
      <Handle type="target" position={Position.Left} />
      <Paper
        p="xs"
        radius="sm"
        withBorder
        style={{
          minWidth: 120,
          borderColor: data.isHome ? 'var(--mantine-color-cyan-5)' : undefined,
          borderWidth: data.isHome ? 2 : 1,
          background: 'var(--mantine-color-dark-7)',
        }}
      >
        <Stack gap={4}>
          <Group justify="space-between" gap={4}>
            <Text size="xs" fw={700} style={{ fontFamily: 'monospace' }}>
              {data.systemName}
            </Text>
            {data.wormholeClass && (
              <Badge size="xs" variant="filled" style={{ background: classColor }}>
                C{data.wormholeClass}
              </Badge>
            )}
          </Group>
          {data.effect && (
            <Text size="xs" c="dimmed">
              {data.effect}
            </Text>
          )}
          {data.sigCount !== undefined && data.sigCount > 0 && (
            <Badge size="xs" variant="outline" color="gray">
              {data.sigCount} sigs
            </Badge>
          )}
        </Stack>
      </Paper>
      <Handle type="source" position={Position.Right} />
    </>
  );
}
