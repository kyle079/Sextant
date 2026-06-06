import { Container, Title, Text, SimpleGrid, Paper, Stack, Center } from '@mantine/core';

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <Paper p="md" radius="sm" withBorder>
      <Stack gap={4}>
        <Text size="xs" c="dimmed" tt="uppercase" fw={600} style={{ letterSpacing: '0.05em' }}>
          {label}
        </Text>
        <Text size="xl" fw={700}>
          {value}
        </Text>
      </Stack>
    </Paper>
  );
}

export default function Dashboard() {
  return (
    <Container size="xl" p="md">
      <Stack gap="xl">
        <div>
          <Title order={3} c="cyan">
            Dashboard
          </Title>
          <Text c="dimmed" size="sm">
            Corp operational overview
          </Text>
        </div>

        <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }}>
          <SummaryCard label="Active Connections" value="—" />
          <SummaryCard label="Unscanned Sigs" value="—" />
          <SummaryCard label="PI Expiring Soon" value="—" />
          <SummaryCard label="Members Online" value="—" />
        </SimpleGrid>

        <Paper p="md" radius="sm" withBorder>
          <Stack gap="sm">
            <Text fw={600} size="sm">
              Activity Feed
            </Text>
            <Center py="xl">
              <Text c="dimmed" size="sm">
                No activity yet. Get started by adding your home system on the Chain Map.
              </Text>
            </Center>
          </Stack>
        </Paper>
      </Stack>
    </Container>
  );
}
