import { Badge, type BadgeTone } from '@/shared/ui';
import { TaskStatusCategory } from './types';

const CATEGORY_TONE: Record<TaskStatusCategory, BadgeTone> = {
  [TaskStatusCategory.Open]: 'blue',
  [TaskStatusCategory.InProgress]: 'clay',
  [TaskStatusCategory.Waiting]: 'amber',
  [TaskStatusCategory.Review]: 'violet',
  [TaskStatusCategory.Completed]: 'green',
  [TaskStatusCategory.Cancelled]: 'neutral',
  [TaskStatusCategory.Rejected]: 'red',
};

/** Renders a custom status name with a tone derived from its reporting category. */
export function TaskStatusBadge({ name, category }: { name: string; category: TaskStatusCategory }) {
  return (
    <Badge tone={CATEGORY_TONE[category]} dot>
      {name}
    </Badge>
  );
}
