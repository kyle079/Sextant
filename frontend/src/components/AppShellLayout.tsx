import {
  AppShell,
  Avatar,
  Burger,
  Group,
  Menu,
  NavLink,
  Text,
  ThemeIcon,
  Box,
  Tooltip,
  Badge,
  rem,
  Stack,
  UnstyledButton,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { useQuery } from '@tanstack/react-query';
import { Outlet, NavLink as RouterNavLink } from 'react-router-dom';
import { apiFetch } from '../api/client';
import type { SdeStatusResponse } from '../api/types';
import { useAppStore } from '../store/useAppStore';

const NAV_ITEMS = [
  { path: '/', label: 'Dashboard', icon: '◈' },
  { path: '/chain', label: 'Chain Map', icon: '⬡' },
  { path: '/members', label: 'Members', icon: '◉' },
  { path: '/kills', label: 'Kill Feed', icon: '✦' },
  { path: '/structures', label: 'Structures', icon: '▣' },
  { path: '/pi', label: 'PI Tracker', icon: '⬢' },
  { path: '/fittings', label: 'Fittings', icon: '⚙' },
  { path: '/sites', label: 'Site Log', icon: '◎' },
  { path: '/settings', label: 'Settings', icon: '⋮' },
] as const;

function CharacterMenu() {
  const { user, activeCharacterId, setActiveCharacter } = useAppStore();
  if (!user) return null;

  const active = user.characters.find((c) => c.characterId === activeCharacterId)
    ?? user.characters[0];

  if (!active) return null;

  const handleLogout = async () => {
    await fetch('/auth/logout', { method: 'POST', credentials: 'include' });
    window.location.href = '/login';
  };

  return (
    <Menu shadow="md" width={220} position="bottom-end">
      <Menu.Target>
        <UnstyledButton>
          <Group gap="xs">
            <Avatar src={active.portraitUrl} size={32} radius="xl" />
            <Box visibleFrom="sm">
              <Text size="sm" fw={600} lh={1.2}>
                {active.characterName}
              </Text>
              <Text size="xs" c="dimmed">
                {user.role}
              </Text>
            </Box>
          </Group>
        </UnstyledButton>
      </Menu.Target>

      <Menu.Dropdown>
        <Menu.Label>Characters</Menu.Label>
        {user.characters.map((c) => (
          <Menu.Item
            key={c.characterId}
            leftSection={<Avatar src={c.portraitUrl} size={20} radius="xl" />}
            rightSection={
              c.characterId === activeCharacterId ? (
                <Badge size="xs" color="cyan">
                  Active
                </Badge>
              ) : null
            }
            onClick={() => setActiveCharacter(c.characterId)}
          >
            {c.characterName}
          </Menu.Item>
        ))}

        <Menu.Divider />
        <Menu.Item component="a" href="/settings?tab=characters">
          Manage Characters
        </Menu.Item>
        <Menu.Item color="red" onClick={handleLogout}>
          Log Out
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  );
}

export function AppShellLayout() {
  const [mobileOpened, { toggle: toggleMobile }] = useDisclosure();
  const [desktopOpened, { toggle: toggleDesktop }] = useDisclosure(true);
  const { data: sdeStatus } = useQuery({
    queryKey: ['sde', 'status'],
    queryFn: () => apiFetch<SdeStatusResponse>('/admin/sde/status'),
    retry: false,
  });
  const esiColor = sdeStatus?.state === 'Succeeded'
    ? 'var(--mantine-color-green-6)'
    : sdeStatus?.state === 'Failed'
      ? 'var(--mantine-color-red-6)'
      : 'var(--mantine-color-yellow-6)';

  return (
    <AppShell
      header={{ height: 52 }}
      navbar={{
        width: 220,
        breakpoint: 'sm',
        collapsed: { mobile: !mobileOpened, desktop: !desktopOpened },
      }}
      padding={0}
    >
      {/* Top Bar */}
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group gap="sm">
            <Burger opened={mobileOpened} onClick={toggleMobile} hiddenFrom="sm" size="sm" />
            <Burger opened={desktopOpened} onClick={toggleDesktop} visibleFrom="sm" size="sm" />
            <Text fw={700} size="lg" c="cyan" style={{ letterSpacing: '0.05em' }}>
              SEXTANT
            </Text>
          </Group>

          <CharacterMenu />
        </Group>
      </AppShell.Header>

      {/* Sidebar */}
      <AppShell.Navbar p="xs">
        <Stack gap={2} flex={1}>
          {NAV_ITEMS.map((item) => (
            <RouterNavLink key={item.path} to={item.path} end={item.path === '/'}>
              {({ isActive }) => (
                <NavLink
                  active={isActive}
                  label={item.label}
                  leftSection={
                    <ThemeIcon variant={isActive ? 'filled' : 'transparent'} color="cyan" size="sm">
                      <span style={{ fontSize: rem(12) }}>{item.icon}</span>
                    </ThemeIcon>
                  }
                  variant="subtle"
                  color="cyan"
                />
              )}
            </RouterNavLink>
          ))}
        </Stack>

        {/* Status indicators */}
        <Box p="xs" style={{ borderTop: '1px solid var(--mantine-color-dark-4)' }}>
          <Group gap="xs">
            <Tooltip label={`ESI/SDE status: ${sdeStatus?.state ?? 'unknown'}`}>
              <Box w={8} h={8} style={{ borderRadius: '50%', background: esiColor }} />
            </Tooltip>
            <Text size="xs" c="dimmed">ESI</Text>
            <Tooltip label="Real-time connection">
              <Box w={8} h={8} style={{ borderRadius: '50%', background: 'var(--mantine-color-gray-6)' }} />
            </Tooltip>
            <Text size="xs" c="dimmed">Live</Text>
          </Group>
        </Box>
      </AppShell.Navbar>

      {/* Main Content */}
      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
