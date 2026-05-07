import { Container, Title, Text, Center, Stack } from '@mantine/core';

export default function PiTracker() {
  return (
    <Container size="xl" p="md">
      <Stack gap="md">
        <div>
          <Title order={3} c="cyan">PI Tracker</Title>
          <Text c="dimmed" size="sm">Planet interaction colony expiry tracking.</Text>
        </div>
        <Center py="xl">
          <Text c="dimmed" size="sm">Coming in Release 8. PI colony tracking from ESI.</Text>
        </Center>
      </Stack>
    </Container>
  );
}
