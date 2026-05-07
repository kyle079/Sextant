import {
  Container,
  Title,
  Text,
  Tabs,
  Stack,
  Center,
  Group,
  Avatar,
  Badge,
  Button,
  Paper,
  ActionIcon,
  Tooltip,
} from '@mantine/core';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { apiFetch } from '../api/client';
import type { MeResponse } from '../api/types';

function MyCharactersTab() {
  const queryClient = useQueryClient();
  const { data: me } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: () => apiFetch<MeResponse>('/auth/me'),
  });

  const addAlt = useMutation({
    mutationFn: () => apiFetch<{ url: string }>('/auth/characters', { method: 'POST' }),
    onSuccess: ({ url }) => { window.location.href = url; },
  });

  const removeAlt = useMutation({
    mutationFn: (characterId: number) =>
      apiFetch<void>(`/auth/characters/${characterId}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['auth', 'me'] }),
  });

  if (!me) return null;

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <Text size="sm" c="dimmed">
          Linked characters share the same account. Switch between them from the top bar.
        </Text>
        <Button
          size="sm"
          variant="outline"
          color="cyan"
          onClick={() => addAlt.mutate()}
          loading={addAlt.isPending}
        >
          + Add Alt
        </Button>
      </Group>

      {me.characters.map((c) => (
        <Paper key={c.characterId} p="sm" radius="sm" withBorder>
          <Group justify="space-between">
            <Group gap="sm">
              <Avatar src={c.portraitUrl} size={40} radius="xl" />
              <div>
                <Group gap="xs">
                  <Text size="sm" fw={600}>
                    {c.characterName}
                  </Text>
                  {c.characterId === me.primaryCharacterId && (
                    <Badge size="xs" color="cyan">
                      Primary
                    </Badge>
                  )}
                </Group>
                <Text size="xs" c="dimmed">
                  Corp ID: {c.corporationId}
                </Text>
              </div>
            </Group>

            {c.characterId !== me.primaryCharacterId && (
              <Tooltip label="Remove alt">
                <ActionIcon
                  color="red"
                  variant="subtle"
                  onClick={() => removeAlt.mutate(c.characterId)}
                  loading={removeAlt.isPending}
                >
                  ✕
                </ActionIcon>
              </Tooltip>
            )}
          </Group>
        </Paper>
      ))}
    </Stack>
  );
}

export default function Settings() {
  const [searchParams] = useSearchParams();
  const defaultTab = searchParams.get('tab') ?? 'characters';

  return (
    <Container size="lg" p="md">
      <Stack gap="md">
        <div>
          <Title order={3} c="cyan">Settings</Title>
          <Text c="dimmed" size="sm">App configuration and preferences.</Text>
        </div>
        <Tabs defaultValue={defaultTab}>
          <Tabs.List>
            <Tabs.Tab value="characters">My Characters</Tabs.Tab>
            <Tabs.Tab value="sde">SDE Data</Tabs.Tab>
            <Tabs.Tab value="corp">Corp Settings</Tabs.Tab>
            <Tabs.Tab value="admin">Admin</Tabs.Tab>
          </Tabs.List>

          <Tabs.Panel value="characters" pt="md">
            <MyCharactersTab />
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
