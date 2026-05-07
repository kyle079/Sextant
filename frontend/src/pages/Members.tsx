import { Container, Title, Text, Center, Stack } from '@mantine/core';

export default function Members() {
  return (
    <Container size="xl" p="md">
      <Stack gap="md">
        <div>
          <Title order={3} c="cyan">Members</Title>
          <Text c="dimmed" size="sm">Corp member online status and location.</Text>
        </div>
        <Center py="xl">
          <Text c="dimmed" size="sm">Coming in Release 10. Member status from ESI.</Text>
        </Center>
      </Stack>
    </Container>
  );
}
