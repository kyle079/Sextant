import {
  AppShell,
  Burger,
  Group,
  NavLink,
  Text,
  ThemeIcon,
  Box,
  Tooltip,
  Badge,
  rem,
  Stack,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { Outlet, NavLink as RouterNavLink } from 'react-router-dom';
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

export function AppShellLayout() {
  const [mobileOpened, { toggle: toggleMobile }] = useDisclosure();
  const [desktopOpened, { toggle: toggleDesktop }] = useDisclosure(true);
  const user = useAppStore((s) => s.user);

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

          <Group gap="sm">
            {user ? (
              <>
                <Badge variant="dot" color="cyan" size="sm">
                  {user.characterName}
                </Badge>
                <Badge variant="outline" color="gray" size="xs">
                  {user.role}
                </Badge>
              </>
            ) : (
              <Badge variant="dot" color="gray" size="sm">
                Not logged in
              </Badge>
            )}
          </Group>
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

        {/* Status bar at bottom of sidebar */}
        <Box p="xs" style={{ borderTop: '1px solid var(--mantine-color-dark-4)' }}>
          <Group gap="xs">
            <Tooltip label="ESI connection status">
              <Box
                w={8}
                h={8}
                style={{ borderRadius: '50%', background: 'var(--mantine-color-gray-6)' }}
              />
            </Tooltip>
            <Text size="xs" c="dimmed">ESI</Text>
            <Tooltip label="Real-time connection status">
              <Box
                w={8}
                h={8}
                style={{ borderRadius: '50%', background: 'var(--mantine-color-gray-6)' }}
              />
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
