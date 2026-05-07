import { Stack, Text, Center, Paper } from '@mantine/core';
import { ActivityEventType } from '../utils/constants';

interface Props {
  typeFilter?: ActivityEventType[];
  systemFilter?: number;
  limit?: number;
}

export function ActivityFeed({ typeFilter: _typeFilter, systemFilter: _systemFilter, limit: _limit }: Props) {
  return (
    <Paper p="md" radius="sm" withBorder>
      <Stack gap="sm">
        <Text fw={600} size="sm">Activity Feed</Text>
        <Center py="md">
          <Text c="dimmed" size="sm">No activity yet.</Text>
        </Center>
      </Stack>
    </Paper>
  );
}
