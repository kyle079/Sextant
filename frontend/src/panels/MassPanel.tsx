import { Drawer, Stack, Text, Title } from '@mantine/core';
import { useAppStore } from '../store/useAppStore';

export function MassPanel() {
  const { massPanelOpen, selectedConnectionId, setMassPanelOpen } = useAppStore();

  return (
    <Drawer
      opened={massPanelOpen}
      onClose={() => setMassPanelOpen(false)}
      title={<Title order={5}>Mass Tracker — Connection {selectedConnectionId}</Title>}
      position="right"
      size="lg"
    >
      <Stack>
        <Text c="dimmed" size="sm">
          Mass tracking coming in Release 6.
        </Text>
      </Stack>
    </Drawer>
  );
}
