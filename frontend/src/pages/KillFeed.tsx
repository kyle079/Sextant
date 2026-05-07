import { Container, Title, Text, Center, Stack } from '@mantine/core';

export default function KillFeed() {
  return (
    <Container size="xl" p="md">
      <Stack gap="md">
        <div>
          <Title order={3} c="cyan">Kill Feed</Title>
          <Text c="dimmed" size="sm">Recent corp kills and losses.</Text>
        </div>
        <Center py="xl">
          <Text c="dimmed" size="sm">Coming in Release 10. Corp kill feed from zKillboard.</Text>
        </Center>
      </Stack>
    </Container>
  );
}
