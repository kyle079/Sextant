import { Drawer, Stack, Text, Title } from '@mantine/core';
import { useAppStore } from '../store/useAppStore';

export function SigPanel() {
  const { sigPanelOpen, selectedSystemId, setSigPanelOpen } = useAppStore();

  return (
    <Drawer
      opened={sigPanelOpen}
      onClose={() => setSigPanelOpen(false)}
      title={<Title order={5}>Signatures — System {selectedSystemId}</Title>}
      position="right"
      size="lg"
    >
      <Stack>
        <Text c="dimmed" size="sm">
          Sig tracker coming in Release 4.
        </Text>
      </Stack>
    </Drawer>
  );
}
