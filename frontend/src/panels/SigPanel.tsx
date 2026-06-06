import { useEffect, useMemo, useRef, useState } from 'react';
import {
  ActionIcon,
  Badge,
  Button,
  Drawer,
  Group,
  NumberInput,
  Select,
  Stack,
  Table,
  Text,
  TextInput,
  Textarea,
  Title,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import type { ChainSystemResponse, SignatureResponse } from '../api/types';
import { useAppStore } from '../store/useAppStore';
import { SigGroupType } from '../utils/constants';
import { parseSigClipboard } from '../utils/sigParser';
import { EolCountdown } from '../components/EolCountdown';

const sigTypeOptions = Object.values(SigGroupType).map((value) => ({ value, label: value }));

export function SigPanel() {
  const queryClient = useQueryClient();
  const pasteRef = useRef<HTMLTextAreaElement>(null);
  const { sigPanelOpen, selectedSystemId, setSigPanelOpen } = useAppStore();
  const [pasteText, setPasteText] = useState('');
  const [manualSig, setManualSig] = useState<{ signatureId: string; group: string; type: SigGroupType; name: string; scanPercentage: number }>({ signatureId: '', group: '', type: SigGroupType.Unknown, name: '', scanPercentage: 0 });

  const { data: systems = [] } = useQuery({
    queryKey: ['chain', 'systems'],
    queryFn: () => apiFetch<ChainSystemResponse[]>('/chain/systems'),
    enabled: sigPanelOpen,
  });
  const system = systems.find((s) => s.id === selectedSystemId);
  const { data: sigs = [] } = useQuery({
    queryKey: ['sigs', selectedSystemId],
    queryFn: () => apiFetch<SignatureResponse[]>(`/sigs/${selectedSystemId}`),
    enabled: sigPanelOpen && selectedSystemId !== null,
  });

  const preview = useMemo(() => parseSigClipboard(pasteText), [pasteText]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['sigs', selectedSystemId] });
    queryClient.invalidateQueries({ queryKey: ['chain', 'systems'] });
  };

  const pasteMutation = useMutation({
    mutationFn: () => apiFetch<{ added: number; updated: number; signatures: SignatureResponse[] }>(`/sigs/${selectedSystemId}/paste`, {
      method: 'POST',
      body: JSON.stringify({ text: pasteText }),
    }),
    onSuccess: ({ added, updated }) => {
      setPasteText('');
      invalidate();
      notifications.show({ color: 'green', message: `Imported ${added} new and updated ${updated} signatures` });
    },
  });

  const addMutation = useMutation({
    mutationFn: () => apiFetch<SignatureResponse>(`/sigs/${selectedSystemId}`, {
      method: 'POST',
      body: JSON.stringify(manualSig),
    }),
    onSuccess: () => {
      setManualSig({ signatureId: '', group: '', type: SigGroupType.Unknown, name: '', scanPercentage: 0 });
      invalidate();
    },
  });

  const updateMutation = useMutation({
    mutationFn: (sig: SignatureResponse) => apiFetch<SignatureResponse>(`/sigs/${sig.id}`, {
      method: 'PUT',
      body: JSON.stringify(sig),
    }),
    onSuccess: invalidate,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => apiFetch<void>(`/sigs/${id}`, { method: 'DELETE' }),
    onSuccess: invalidate,
  });

  useEffect(() => {
    const handler = (event: KeyboardEvent) => {
      if (sigPanelOpen && event.key.toLowerCase() === 'v' && !event.ctrlKey && !event.metaKey) pasteRef.current?.focus();
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [sigPanelOpen]);

  return (
    <Drawer
      opened={sigPanelOpen}
      onClose={() => setSigPanelOpen(false)}
      title={<Title order={5}>Signatures — {system?.name ?? `System ${selectedSystemId}`}</Title>}
      position="right"
      size="xl"
    >
      <Stack>
        {system && (
          <Group gap="xs">
            <Badge color="cyan">{system.class}</Badge>
            {system.effect && <Badge variant="outline">{system.effect}</Badge>}
            <Text size="xs" c="dimmed">Statics: {system.statics.join(', ') || '—'}</Text>
          </Group>
        )}

        <Textarea
          ref={pasteRef}
          label="Paste signatures"
          placeholder="Paste EVE probe scanner rows here"
          minRows={4}
          value={pasteText}
          onChange={(event) => setPasteText(event.currentTarget.value)}
        />
        <Group justify="space-between">
          <Text size="xs" c="dimmed">Preview: {preview.length} signatures</Text>
          <Button size="xs" onClick={() => pasteMutation.mutate()} loading={pasteMutation.isPending} disabled={!pasteText.trim()}>Import</Button>
        </Group>

        <Group align="end" grow>
          <TextInput label="ID" placeholder="ABC-123" value={manualSig.signatureId} onChange={(e) => setManualSig((s) => ({ ...s, signatureId: e.currentTarget.value }))} />
          <Select label="Type" data={sigTypeOptions} value={manualSig.type} onChange={(v) => setManualSig((s) => ({ ...s, type: (v ?? SigGroupType.Unknown) as SigGroupType }))} />
          <TextInput label="Name" value={manualSig.name} onChange={(e) => setManualSig((s) => ({ ...s, name: e.currentTarget.value }))} />
          <NumberInput label="Scan %" min={0} max={100} value={manualSig.scanPercentage} onChange={(v) => setManualSig((s) => ({ ...s, scanPercentage: Number(v) || 0 }))} />
          <Button onClick={() => addMutation.mutate()} disabled={!manualSig.signatureId}>Add</Button>
        </Group>

        <Table striped highlightOnHover withTableBorder>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>ID</Table.Th>
              <Table.Th>Type</Table.Th>
              <Table.Th>Name</Table.Th>
              <Table.Th>Scan</Table.Th>
              <Table.Th>By</Table.Th>
              <Table.Th>EOL</Table.Th>
              <Table.Th />
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {sigs.map((sig) => (
              <Table.Tr key={sig.id}>
                <Table.Td><Text fw={600} size="sm">{sig.signatureId}</Text></Table.Td>
                <Table.Td>
                  <Select size="xs" data={sigTypeOptions} value={sig.type} onChange={(type) => updateMutation.mutate({ ...sig, type: (type ?? SigGroupType.Unknown) as SignatureResponse['type'] })} />
                </Table.Td>
                <Table.Td>
                  <TextInput size="xs" defaultValue={sig.name ?? ''} onBlur={(event) => updateMutation.mutate({ ...sig, name: event.currentTarget.value })} placeholder="Unknown" />
                </Table.Td>
                <Table.Td>{sig.scanPercentage}%</Table.Td>
                <Table.Td><Text size="xs">{sig.scannedByCharacterName}</Text></Table.Td>
                <Table.Td>{sig.type === SigGroupType.Wormhole ? <EolCountdown eolAt={sig.eolAt ?? null} /> : null}</Table.Td>
                <Table.Td>
                  <Group gap={4} justify="flex-end">
                    {sig.type === SigGroupType.Wormhole && (
                      <Button
                        size="compact-xs"
                        variant="subtle"
                        onClick={() => window.dispatchEvent(new CustomEvent('sextant:add-connection', { detail: { sourceSystemId: sig.chainSystemId, signatureId: sig.signatureId } }))}
                      >
                        Add Connection
                      </Button>
                    )}
                    <ActionIcon color="red" variant="subtle" onClick={() => deleteMutation.mutate(sig.id)}>✕</ActionIcon>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Stack>
    </Drawer>
  );
}
