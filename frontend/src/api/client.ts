// Thin wrapper around the NSwag-generated client.
// Run `nswag run` from the project root to regenerate client.generated.ts.

const baseUrl = import.meta.env.VITE_API_URL ?? '';

// Placeholder until NSwag generation produces client.generated.ts
export const apiBase = baseUrl;

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
    credentials: 'include',
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}
