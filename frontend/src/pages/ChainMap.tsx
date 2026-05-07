import { Center, Stack, Text, Button, Title } from '@mantine/core';

export default function ChainMap() {
  return (
    <div style={{ height: 'calc(100vh - 52px)', display: 'flex', flexDirection: 'column' }}>
      <Center style={{ flex: 1 }}>
        <Stack align="center" gap="md">
          <Title order={3} c="dimmed">
            Chain Map
          </Title>
          <Text c="dimmed" size="sm">
            Coming in Release 3. Visual wormhole chain map with React Flow.
          </Text>
          <Button variant="outline" color="cyan" disabled>
            + Add System
          </Button>
        </Stack>
      </Center>
    </div>
  );
}
