import { useEffect } from 'react';
import { Navigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Center, Loader } from '@mantine/core';
import { apiFetch, ApiError } from '../api/client';
import { useAppStore } from '../store/useAppStore';
import type { MeResponse } from '../api/types';

interface Props {
  children: React.ReactNode;
}

export function AuthGuard({ children }: Props) {
  const setUser = useAppStore((s) => s.setUser);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: () => apiFetch<MeResponse>('/auth/me'),
    retry: false,
    staleTime: 5 * 60 * 1000,
  });

  useEffect(() => {
    if (data) setUser(data);
  }, [data, setUser]);

  if (isLoading) {
    return (
      <Center h="100vh">
        <Loader color="cyan" />
      </Center>
    );
  }

  if (isError) {
    if (error instanceof ApiError && error.status === 401) {
      return <Navigate to="/login" replace />;
    }
    // Non-401 errors (network issues etc.) — still redirect to login
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
