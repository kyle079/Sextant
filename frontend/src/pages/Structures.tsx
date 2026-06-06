import { Container, Title, Text, Center, Stack } from '@mantine/core';

export default function Structures() {
  return (
    <Container size="xl" p="md">
      <Stack gap="md">
        <div>
          <Title order={3} c="cyan">Structures</Title>
          <Text c="dimmed" size="sm">Corp structure fuel and service status.</Text>
        </div>
        <Center py="xl">
          <Text c="dimmed" size="sm">Coming in Release 10. Structure fuel levels from ESI.</Text>
        </Center>
      </Stack>
    </Container>
  );
}
