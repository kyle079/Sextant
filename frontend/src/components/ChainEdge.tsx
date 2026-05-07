import { getBezierPath, EdgeLabelRenderer, type EdgeProps } from 'reactflow';
import { Badge } from '@mantine/core';
import { MassStatus } from '../utils/constants';

export interface ChainEdgeData {
  wormholeTypeCode?: string;
  massStatus?: MassStatus;
  lifeStatus?: string;
}

const MASS_COLORS: Record<MassStatus, string> = {
  [MassStatus.Fresh]: '#40c057',
  [MassStatus.Half]: '#fab005',
  [MassStatus.Critical]: '#fa5252',
};

export function ChainEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  data,
}: EdgeProps<ChainEdgeData>) {
  const [edgePath, labelX, labelY] = getBezierPath({ sourceX, sourceY, sourcePosition, targetX, targetY, targetPosition });
  const color = data?.massStatus ? MASS_COLORS[data.massStatus] : '#495057';

  return (
    <>
      <path id={id} className="react-flow__edge-path" d={edgePath} stroke={color} strokeWidth={2} fill="none" />
      {data?.wormholeTypeCode && (
        <EdgeLabelRenderer>
          <div
            style={{
              position: 'absolute',
              transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`,
              pointerEvents: 'all',
            }}
          >
            <Badge size="xs" variant="filled" color={data.lifeStatus === 'EOL' ? 'orange' : 'dark'}>
              {data.wormholeTypeCode}
              {data.lifeStatus === 'EOL' ? ' ⚠' : ''}
            </Badge>
          </div>
        </EdgeLabelRenderer>
      )}
    </>
  );
}
