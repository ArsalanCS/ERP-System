import { useTranslation } from 'react-i18next';
import { Badge, type BadgeTone } from '@/shared/ui';
import { UserStatus } from './types';

const TONE: Record<UserStatus, BadgeTone> = {
  [UserStatus.PendingInvitation]: 'amber',
  [UserStatus.Active]: 'green',
  [UserStatus.Inactive]: 'neutral',
  [UserStatus.Suspended]: 'red',
  [UserStatus.Locked]: 'red',
  [UserStatus.Archived]: 'neutral',
};

export function UserStatusBadge({ status }: { status: UserStatus }) {
  const { t } = useTranslation();
  return (
    <Badge tone={TONE[status]} dot>
      {t(`users.status.${status}`)}
    </Badge>
  );
}
