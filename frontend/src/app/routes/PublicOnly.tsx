import { Navigate, Outlet } from 'react-router-dom';
import { useSession } from '@/shared/rbac/session';
import { RouteFallback } from './RouteFallback';

/** Wraps the auth pages: signed-in users are bounced to the admin area. */
export function PublicOnly() {
  const { status } = useSession();
  if (status === 'loading') return <RouteFallback />;
  if (status === 'authenticated') return <Navigate to="/admin/overview" replace />;
  return <Outlet />;
}
