import { lazy } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AdminLayout } from './layouts/AdminLayout';
import { AuthLayout } from './layouts/AuthLayout';
import { RequireAuth } from './routes/RequireAuth';
import { PublicOnly } from './routes/PublicOnly';
import { NotFound } from './pages/NotFound';
import { LoginPage } from '@/features/auth/pages/LoginPage';
import { ForgotPasswordPage } from '@/features/auth/pages/ForgotPasswordPage';
import { ResetPasswordPage } from '@/features/auth/pages/ResetPasswordPage';
import { VerifyEmailPage } from '@/features/auth/pages/VerifyEmailPage';
import { SignupPage } from '@/features/auth/pages/SignupPage';
import { LandingPage } from '@/features/marketing/LandingPage';

// Admin feature pages are code-split so the initial (auth) bundle stays small.
const DashboardPage = lazy(() =>
  import('@/features/dashboard/DashboardPage').then((m) => ({ default: m.DashboardPage })),
);
const UsersPage = lazy(() =>
  import('@/features/users/UsersPage').then((m) => ({ default: m.UsersPage })),
);
const AccessControlPage = lazy(() =>
  import('@/features/access-control/AccessControlPage').then((m) => ({ default: m.AccessControlPage })),
);
const BusinessStructurePage = lazy(() =>
  import('@/features/structure/BusinessStructurePage').then((m) => ({ default: m.BusinessStructurePage })),
);
const TasksPage = lazy(() => import('@/features/tasks/TasksPage').then((m) => ({ default: m.TasksPage })));
const TaskDashboardPage = lazy(() =>
  import('@/features/tasks/TaskDashboardPage').then((m) => ({ default: m.TaskDashboardPage })),
);
const TaskSettingsPage = lazy(() =>
  import('@/features/tasks/TaskSettingsPage').then((m) => ({ default: m.TaskSettingsPage })),
);
const TaskReportsPage = lazy(() =>
  import('@/features/tasks/TaskReportsPage').then((m) => ({ default: m.TaskReportsPage })),
);
const TaskDetailsPage = lazy(() =>
  import('@/features/tasks/TaskDetailsPage').then((m) => ({ default: m.TaskDetailsPage })),
);
const MailPage = lazy(() => import('@/features/mail/MailPage').then((m) => ({ default: m.MailPage })));
const SecurityPage = lazy(() =>
  import('@/features/security/SecurityPage').then((m) => ({ default: m.SecurityPage })),
);
const AuditPage = lazy(() =>
  import('@/features/audit/AuditPage').then((m) => ({ default: m.AuditPage })),
);
const SettingsPage = lazy(() =>
  import('@/features/settings/SettingsPage').then((m) => ({ default: m.SettingsPage })),
);
const ProfilePage = lazy(() =>
  import('@/features/profile/ProfilePage').then((m) => ({ default: m.ProfilePage })),
);

/**
 * App routes. Public auth pages sit under {@link PublicOnly}; the admin shell
 * sits under {@link RequireAuth}. The seven primary pages map 1:1 to the
 * Identity spec §18 navigation summary.
 */
export const router = createBrowserRouter([
  // Public marketing landing — reachable by everyone (signed-in users see a
  // "go to dashboard" CTA instead of sign-up).
  { path: '/', element: <LandingPage /> },
  {
    // Always accessible — a signed-in admin may click an invite/reset/verify
    // link, and the recipient may still have a stale session. NOT behind PublicOnly.
    element: <AuthLayout />,
    children: [
      { path: '/reset-password', element: <ResetPasswordPage /> },
      { path: '/verify-email', element: <VerifyEmailPage /> },
    ],
  },
  {
    element: <PublicOnly />,
    children: [
      // Signup carries its own full-page chrome (nav + stepper), so it sits
      // outside AuthLayout's centered card.
      { path: '/signup', element: <SignupPage /> },
      {
        element: <AuthLayout />,
        children: [
          { path: '/login', element: <LoginPage /> },
          { path: '/forgot-password', element: <ForgotPasswordPage /> },
        ],
      },
    ],
  },
  {
    element: <RequireAuth />,
    children: [
      {
        path: '/admin',
        element: <AdminLayout />,
        children: [
          { index: true, element: <Navigate to="overview" replace /> },
          { path: 'overview', element: <DashboardPage />, handle: { crumbKey: 'nav.overview' } },
          { path: 'users', element: <UsersPage />, handle: { crumbKey: 'nav.users' } },
          {
            path: 'access-control',
            element: <AccessControlPage />,
            handle: { crumbKey: 'nav.accessControl' },
          },
          {
            path: 'business-structure',
            element: <BusinessStructurePage />,
            handle: { crumbKey: 'nav.businessStructure' },
          },
          { path: 'tasks', element: <TasksPage />, handle: { crumbKey: 'nav.tasks' } },
          { path: 'tasks/dashboard', element: <TaskDashboardPage />, handle: { crumbKey: 'nav.tasks' } },
          { path: 'tasks/reports', element: <TaskReportsPage />, handle: { crumbKey: 'nav.tasks' } },
          { path: 'tasks/settings', element: <TaskSettingsPage />, handle: { crumbKey: 'nav.tasks' } },
          { path: 'tasks/:id', element: <TaskDetailsPage />, handle: { crumbKey: 'nav.tasks' } },
          { path: 'mail', element: <MailPage />, handle: { crumbKey: 'nav.mail' } },
          { path: 'security', element: <SecurityPage />, handle: { crumbKey: 'nav.security' } },
          { path: 'audit', element: <AuditPage />, handle: { crumbKey: 'nav.audit' } },
          { path: 'settings', element: <SettingsPage />, handle: { crumbKey: 'nav.settings' } },
          { path: 'profile', element: <ProfilePage />, handle: { crumbKey: 'nav.profile' } },
        ],
      },
    ],
  },
  { path: '*', element: <NotFound /> },
]);
