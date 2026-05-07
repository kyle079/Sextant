import { Container, Title, Text, Center, Stack } from '@mantine/core';

export default function SiteLog() {
  return (
    <Container size="xl" p="md">
      <Stack gap="md">
        <div>
          <Title order={3} c="cyan">Site Log</Title>
          <Text c="dimmed" size="sm">Track completed sites and ISK income.</Text>
        </div>
        <Center py="xl">
          <Text c="dimmed" size="sm">Coming in Release 11. Log combat sites, gas, and more.</Text>
        </Center>
      </Stack>
    </Container>
  );
}
