import { Routes, Route, Navigate } from 'react-router-dom';
import { AppShellLayout } from './components/AppShellLayout';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import ChainMap from './pages/ChainMap';
import PiTracker from './pages/PiTracker';
import Fittings from './pages/Fittings';
import SiteLog from './pages/SiteLog';
import Structures from './pages/Structures';
import KillFeed from './pages/KillFeed';
import Members from './pages/Members';
import Settings from './pages/Settings';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route element={<AppShellLayout />}>
        <Route index element={<Dashboard />} />
        <Route path="/chain" element={<ChainMap />} />
        <Route path="/pi" element={<PiTracker />} />
        <Route path="/fittings" element={<Fittings />} />
        <Route path="/sites" element={<SiteLog />} />
        <Route path="/structures" element={<Structures />} />
        <Route path="/kills" element={<KillFeed />} />
        <Route path="/members" element={<Members />} />
        <Route path="/settings" element={<Settings />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
