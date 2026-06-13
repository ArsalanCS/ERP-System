import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { cn } from '@/shared/lib/cn';

type Variant = 'primary' | 'outline' | 'ghost' | 'dark';
type Size = 'md' | 'sm' | 'lg';

const VARIANT_CLASS: Record<Variant, string> = {
  primary: 'btn-primary',
  outline: 'btn-outline',
  ghost: 'btn-ghost',
  dark: 'btn-dark',
};

const SIZE_CLASS: Record<Size, string> = {
  md: '',
  sm: 'btn-sm',
  lg: 'btn-lg',
};

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  block?: boolean;
  leadingIcon?: ReactNode;
}

export function Button({
  variant = 'primary',
  size = 'md',
  block = false,
  leadingIcon,
  className,
  children,
  type = 'button',
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      className={cn('btn', VARIANT_CLASS[variant], SIZE_CLASS[size], block && 'btn-block', className)}
      {...props}
    >
      {leadingIcon}
      {children}
    </button>
  );
}
