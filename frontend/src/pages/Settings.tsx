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
  SimpleGrid,
  Alert,
} from '@mantine/core';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { apiFetch } from '../api/client';
import type { MeResponse, SdeStatusResponse } from '../api/types';

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

function SdeDataTab() {
  const queryClient = useQueryClient();
  const { data: me } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: () => apiFetch<MeResponse>('/auth/me'),
  });
  const { data: status, isLoading } = useQuery({
    queryKey: ['sde', 'status'],
    queryFn: () => apiFetch<SdeStatusResponse>('/admin/sde/status'),
  });

  const refresh = useMutation({
    mutationFn: () => apiFetch<SdeStatusResponse>('/admin/sde/refresh', { method: 'POST' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['sde', 'status'] }),
  });

  const isAdmin = me?.role === 'Admin';

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-start">
        <div>
          <Text fw={600}>Wormhole reference data</Text>
          <Text size="sm" c="dimmed">
            Loaded from EVE static data sources and cached in Sextant for chain-map autocomplete and wormhole mass/lifetime lookups.
          </Text>
        </div>
        <Button
          size="sm"
          color="cyan"
          disabled={!isAdmin}
          loading={refresh.isPending}
          onClick={() => refresh.mutate()}
        >
          Force Refresh
        </Button>
      </Group>

      {!isAdmin && (
        <Alert color="yellow" variant="light">
          Only admins can force an SDE refresh. Current status is visible to all signed-in members.
        </Alert>
      )}

      {status?.error && <Alert color="red">Last refresh error: {status.error}</Alert>}

      <SimpleGrid cols={{ base: 1, sm: 2, md: 4 }}>
        <Paper withBorder p="md" radius="sm">
          <Text size="xs" c="dimmed" tt="uppercase">Status</Text>
          <Badge color={status?.state === 'Succeeded' ? 'green' : status?.state === 'Failed' ? 'red' : 'yellow'}>
            {isLoading ? 'Loading' : status?.state ?? 'Unknown'}
          </Badge>
        </Paper>
        <Paper withBorder p="md" radius="sm">
          <Text size="xs" c="dimmed" tt="uppercase">WH systems</Text>
          <Text fw={700}>{status?.wormholeSystemCount ?? '—'}</Text>
        </Paper>
        <Paper withBorder p="md" radius="sm">
          <Text size="xs" c="dimmed" tt="uppercase">WH types</Text>
          <Text fw={700}>{status?.wormholeTypeCount ?? '—'}</Text>
        </Paper>
        <Paper withBorder p="md" radius="sm">
          <Text size="xs" c="dimmed" tt="uppercase">Last refresh</Text>
          <Text size="sm">
            {status?.lastRefreshCompletedAt
              ? new Date(status.lastRefreshCompletedAt).toLocaleString()
              : 'Never'}
          </Text>
        </Paper>
      </SimpleGrid>
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
            <SdeDataTab />
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
