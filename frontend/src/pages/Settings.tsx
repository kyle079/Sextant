import { Container, Title, Text, Tabs, Stack, Center } from '@mantine/core';

export default function Settings() {
  return (
    <Container size="lg" p="md">
      <Stack gap="md">
        <div>
          <Title order={3} c="cyan">Settings</Title>
          <Text c="dimmed" size="sm">App configuration and preferences.</Text>
        </div>
        <Tabs defaultValue="characters">
          <Tabs.List>
            <Tabs.Tab value="characters">My Characters</Tabs.Tab>
            <Tabs.Tab value="sde">SDE Data</Tabs.Tab>
            <Tabs.Tab value="corp">Corp Settings</Tabs.Tab>
            <Tabs.Tab value="admin">Admin</Tabs.Tab>
          </Tabs.List>

          <Tabs.Panel value="characters" pt="md">
            <Center py="xl">
              <Text c="dimmed" size="sm">Character management coming in Release 1.</Text>
            </Center>
          </Tabs.Panel>

          <Tabs.Panel value="sde" pt="md">
            <Center py="xl">
              <Text c="dimmed" size="sm">SDE data management coming in Release 2.</Text>
            </Center>
          </Tabs.Panel>

          <Tabs.Panel value="corp" pt="md">
            <Center py="xl">
              <Text c="dimmed" size="sm">Corp settings coming in Release 11.</Text>
            </Center>
          </Tabs.Panel>

          <Tabs.Panel value="admin" pt="md">
            <Center py="xl">
              <Text c="dimmed" size="sm">Admin user management coming in Release 11.</Text>
            </Center>
          </Tabs.Panel>
        </Tabs>
      </Stack>
    </Container>
  );
}
