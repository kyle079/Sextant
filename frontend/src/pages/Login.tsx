import {
  Stack,
  Title,
  Text,
  Button,
  Paper,
  ThemeIcon,
  Divider,
  Box,
} from '@mantine/core';

export default function Login() {
  return (
    <Box
      style={{
        minHeight: '100vh',
        background: 'var(--mantine-color-dark-9)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <Paper p="xl" radius="md" withBorder style={{ width: 360 }}>
        <Stack align="center" gap="lg">
          <ThemeIcon size={64} radius="xl" variant="light" color="cyan">
            <Text size="xl">◈</Text>
          </ThemeIcon>
          <div style={{ textAlign: 'center' }}>
            <Title order={2} c="cyan" style={{ letterSpacing: '0.1em' }}>
              SEXTANT
            </Title>
            <Text c="dimmed" size="sm" mt={4}>
              WH Corp Management
            </Text>
          </div>

          <Divider w="100%" />

          <Stack w="100%" gap="sm">
            <Button
              component="a"
              href="/auth/login"
              size="md"
              variant="filled"
              color="cyan"
              fullWidth
              leftSection={<Text size="sm">⚡</Text>}
            >
              Login with Eve Online
            </Button>
            <Text size="xs" c="dimmed" ta="center">
              Corp membership is required to access Sextant.
            </Text>
          </Stack>
        </Stack>
      </Paper>
    </Box>
  );
}
