import { Container, Title, Text, Center, Stack } from '@mantine/core';

export default function Fittings() {
  return (
    <Container size="xl" p="md">
      <Stack gap="md">
        <div>
          <Title order={3} c="cyan">Fittings</Title>
          <Text c="dimmed" size="sm">Doctrine fit library.</Text>
        </div>
        <Center py="xl">
          <Text c="dimmed" size="sm">Coming in Release 9. Store and share corp doctrine fits.</Text>
        </Center>
      </Stack>
    </Container>
  );
}
