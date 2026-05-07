import { useCallback, useEffect, useMemo, useState } from 'react';
import ReactFlow, {
  Background,
  Controls,
  MiniMap,
  ReactFlowProvider,
  addEdge,
  useEdgesState,
  useNodesState,
  useReactFlow,
  type Connection,
  type Edge,
  type Node,
} from 'reactflow';
import {
  ActionIcon,
  Button,
  Checkbox,
  Group,
  Modal,
  Paper,
  Select,
  Stack,
  Text,
  TextInput,
  Tooltip,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import type {
  ChainConnectionResponse,
  ChainSystemResponse,
  WormholeSystemSearchResponse,
  WormholeTypeResponse,
} from '../api/types';
import { ChainNode, type ChainNodeData } from '../components/ChainNode';
import { ChainEdge, type ChainEdgeData } from '../components/ChainEdge';
import { SigPanel } from '../panels/SigPanel';
import { useAppStore } from '../store/useAppStore';

const nodeTypes = { chainNode: ChainNode };
const edgeTypes = { chainEdge: ChainEdge };

function ChainMapInner() {
  const reactFlow = useReactFlow();
  const queryClient = useQueryClient();
  const selectSystem = useAppStore((s) => s.selectSystem);
  const nodePositions = useAppStore((s) => s.nodePositions);
  const setNodePosition = useAppStore((s) => s.setNodePosition);
  const [systemModalOpen, systemModal] = useDisclosure(false);
  const [connectionModalOpen, connectionModal] = useDisclosure(false);
  const [systemSearch, setSystemSearch] = useState('');
  const [selectedSdeSystem, setSelectedSdeSystem] = useState<string | null>(null);
  const [isHome, setIsHome] = useState(false);
  const [connectionDraft, setConnectionDraft] = useState({ sourceSystemId: '', targetSystemId: '', wormholeTypeCode: '' });
  const [now, setNow] = useState(0);

  useEffect(() => {
    const first = setTimeout(() => setNow(Date.now()), 0);
    const interval = setInterval(() => setNow(Date.now()), 60_000);
    return () => { clearTimeout(first); clearInterval(interval); };
  }, []);

  const { data: systems = [] } = useQuery({
    queryKey: ['chain', 'systems'],
    queryFn: () => apiFetch<ChainSystemResponse[]>('/chain/systems'),
  });
  const { data: connections = [] } = useQuery({
    queryKey: ['chain', 'connections'],
    queryFn: () => apiFetch<ChainConnectionResponse[]>('/chain/connections'),
  });
  const { data: sdeSystems = [] } = useQuery({
    queryKey: ['sde', 'systems', systemSearch],
    queryFn: () => apiFetch<WormholeSystemSearchResponse[]>(`/sde/systems?q=${encodeURIComponent(systemSearch)}`),
    enabled: systemSearch.length >= 2,
  });
  const { data: whTypes = [] } = useQuery({
    queryKey: ['sde', 'wormhole-types'],
    queryFn: () => apiFetch<WormholeTypeResponse[]>('/sde/wormhole-types'),
  });

  const systemOptions = sdeSystems.map((s) => ({ value: String(s.solarSystemId), label: `${s.name} — ${s.class}${s.effect ? ` ${s.effect}` : ''}` }));
  const chainSystemOptions = systems.map((s) => ({ value: String(s.id), label: s.name }));
  const whTypeOptions = whTypes.map((t) => ({ value: t.code, label: `${t.code} → ${t.destinationType ?? 'WH'}${t.destinationClass ? ` C${t.destinationClass}` : ''}` }));

  const addSystemMutation = useMutation({
    mutationFn: () => {
      const sde = sdeSystems.find((s) => String(s.solarSystemId) === selectedSdeSystem);
      return apiFetch<ChainSystemResponse>('/chain/systems', {
        method: 'POST',
        body: JSON.stringify({
          solarSystemId: sde?.solarSystemId ?? 0,
          name: sde?.name ?? systemSearch,
          class: sde?.class,
          classNumber: sde?.classNumber,
          effect: sde?.effect,
          statics: sde?.statics,
          isHome,
        }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chain'] });
      systemModal.close();
      setSelectedSdeSystem(null);
      setSystemSearch('');
      setIsHome(false);
      notifications.show({ color: 'green', message: 'System added to chain' });
    },
  });

  const addConnectionMutation = useMutation({
    mutationFn: () => apiFetch<ChainConnectionResponse>('/chain/connections', {
      method: 'POST',
      body: JSON.stringify({
        sourceSystemId: Number(connectionDraft.sourceSystemId),
        targetSystemId: Number(connectionDraft.targetSystemId),
        wormholeTypeCode: connectionDraft.wormholeTypeCode,
      }),
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chain', 'connections'] });
      connectionModal.close();
      notifications.show({ color: 'green', message: 'Connection added' });
    },
  });

  const deleteSystemMutation = useMutation({
    mutationFn: (id: number) => apiFetch<void>(`/chain/systems/${id}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['chain'] }),
  });
  const deleteConnectionMutation = useMutation({
    mutationFn: (id: number) => apiFetch<void>(`/chain/connections/${id}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['chain', 'connections'] }),
  });

  const initialNodes = useMemo<Node<ChainNodeData>[]>(() => systems.map((system, index) => {
    const id = String(system.id);
    return {
    id,
    type: 'chainNode',
    position: nodePositions[id] ?? { x: (index % 4) * 220 + 80, y: Math.floor(index / 4) * 150 + 80 },
    data: {
      systemName: system.name,
      wormholeClass: system.classNumber ?? undefined,
      effect: system.effect ?? undefined,
      sigCount: system.sigCount,
      isHome: system.isHome,
    },
  };
  }), [systems, nodePositions]);

  const initialEdges = useMemo<Edge<ChainEdgeData>[]>(() => connections.map((connection) => ({
    id: String(connection.id),
    source: String(connection.sourceSystemId),
    target: String(connection.targetSystemId),
    type: 'chainEdge',
    data: {
      wormholeTypeCode: connection.wormholeTypeCode,
      massStatus: connection.massStatus,
      lifeStatus: connection.eolAt && now !== 0 && new Date(connection.eolAt).getTime() - now < 4 * 3_600_000 ? 'EOL' : 'Stable',
    },
  })), [connections, now]);

  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

  useEffect(() => { setNodes(initialNodes); }, [initialNodes, setNodes]);
  useEffect(() => { setEdges(initialEdges); }, [initialEdges, setEdges]);

  const openConnectionModal = useCallback((sourceSystemId = '', targetSystemId = '', wormholeTypeCode = '') => {
    setConnectionDraft({ sourceSystemId, targetSystemId, wormholeTypeCode });
    connectionModal.open();
  }, [connectionModal]);

  useEffect(() => {
    const handler = (event: Event) => {
      const detail = (event as CustomEvent<{ sourceSystemId: number; wormholeTypeCode?: string }>).detail;
      openConnectionModal(String(detail.sourceSystemId), '', detail.wormholeTypeCode ?? '');
    };
    window.addEventListener('sextant:add-connection', handler);
    return () => window.removeEventListener('sextant:add-connection', handler);
  }, [openConnectionModal]);

  const onConnect = (connection: Connection) => {
    if (connection.source && connection.target) openConnectionModal(connection.source, connection.target);
    setEdges((eds) => addEdge(connection, eds));
  };

  return (
    <div style={{ height: 'calc(100vh - 52px)', display: 'flex', flexDirection: 'column' }}>
      <Paper p="xs" radius={0} withBorder style={{ borderLeft: 0, borderRight: 0, borderTop: 0 }}>
        <Group gap="xs">
          <Button size="xs" color="cyan" onClick={systemModal.open}>+ System</Button>
          <Button size="xs" variant="outline" color="cyan" disabled={systems.length < 2} onClick={() => openConnectionModal()}>+ Connection</Button>
          <Button size="xs" variant="subtle" onClick={() => reactFlow.fitView()}>Fit View</Button>
          <Button
            size="xs"
            variant="subtle"
            onClick={() => {
              const home = systems.find((s) => s.isHome);
              const node = home ? nodes.find((n) => n.id === String(home.id)) : undefined;
              if (node) reactFlow.setCenter(node.position.x, node.position.y, { zoom: 1.2 });
            }}
          >
            Center Home
          </Button>
          <Text size="xs" c="dimmed">{systems.length} systems · {connections.length} connections</Text>
        </Group>
      </Paper>

      <div style={{ flex: 1 }}>
        {systems.length === 0 ? (
          <Stack align="center" justify="center" h="100%" gap="sm">
            <Text c="dimmed">No systems mapped yet. Click + System to add your home.</Text>
            <Button color="cyan" onClick={systemModal.open}>+ System</Button>
          </Stack>
        ) : (
          <ReactFlow
            nodes={nodes}
            edges={edges}
            nodeTypes={nodeTypes}
            edgeTypes={edgeTypes}
            onNodesChange={onNodesChange}
            onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            onNodeClick={(_, node) => selectSystem(Number(node.id))}
            onNodeDragStop={(_, node) => setNodePosition(node.id, node.position)}
            onNodeContextMenu={(event, node) => { event.preventDefault(); deleteSystemMutation.mutate(Number(node.id)); }}
            onEdgeContextMenu={(event, edge) => { event.preventDefault(); deleteConnectionMutation.mutate(Number(edge.id)); }}
            fitView
          >
            <Background />
            <MiniMap />
            <Controls />
          </ReactFlow>
        )}
      </div>

      <Modal opened={systemModalOpen} onClose={systemModal.close} title="Add system">
        <Stack>
          <Select
            label="J-system"
            placeholder="Search J100001"
            searchable
            data={systemOptions}
            searchValue={systemSearch}
            onSearchChange={setSystemSearch}
            value={selectedSdeSystem}
            onChange={setSelectedSdeSystem}
          />
          <TextInput label="Manual system name" value={systemSearch} onChange={(e) => setSystemSearch(e.currentTarget.value)} />
          <Checkbox label="Home system" checked={isHome} onChange={(e) => setIsHome(e.currentTarget.checked)} />
          <Group justify="flex-end">
            <Button onClick={() => addSystemMutation.mutate()} loading={addSystemMutation.isPending} disabled={!selectedSdeSystem && !systemSearch}>Add</Button>
          </Group>
        </Stack>
      </Modal>

      <Modal opened={connectionModalOpen} onClose={connectionModal.close} title="Add connection">
        <Stack>
          <Select label="Source" data={chainSystemOptions} value={connectionDraft.sourceSystemId} onChange={(v) => setConnectionDraft((d) => ({ ...d, sourceSystemId: v ?? '' }))} />
          <Select label="Destination" data={chainSystemOptions} value={connectionDraft.targetSystemId} onChange={(v) => setConnectionDraft((d) => ({ ...d, targetSystemId: v ?? '' }))} />
          <Select label="Wormhole type" searchable data={whTypeOptions} value={connectionDraft.wormholeTypeCode} onChange={(v) => setConnectionDraft((d) => ({ ...d, wormholeTypeCode: v ?? '' }))} />
          <Group justify="space-between">
            <Tooltip label="Right-click an edge to collapse it">
              <ActionIcon variant="subtle">?</ActionIcon>
            </Tooltip>
            <Button onClick={() => addConnectionMutation.mutate()} loading={addConnectionMutation.isPending} disabled={!connectionDraft.sourceSystemId || !connectionDraft.targetSystemId}>Add</Button>
          </Group>
        </Stack>
      </Modal>

      <SigPanel />
    </div>
  );
}

export default function ChainMap() {
  return (
    <ReactFlowProvider>
      <ChainMapInner />
    </ReactFlowProvider>
  );
}
