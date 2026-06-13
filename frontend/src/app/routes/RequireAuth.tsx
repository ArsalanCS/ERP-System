import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useSession } from '@/shared/rbac/session';
import { RouteFallback } from './RouteFallback';

/**
 * Gate for the authenticated admin area. Redirects to /login when signed out,
 * preserving the attempted location so login can return the user to it.
 * This is UX routing only — every API call is still authorized server-side.
 */
export function RequireAuth() {
  const { status } = useSession();
  const location = useLocation();

  if (status === 'loading') return <RouteFallback />;
  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }
  return <Outlet />;
}
