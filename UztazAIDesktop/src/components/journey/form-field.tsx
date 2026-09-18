import { Label } from "@/components/ui/input";

/** Accessible field wrapper: wires aria-invalid / aria-describedby from the error message onto
 * whatever input is passed as a child, and renders the error inline next to the field in plain
 * language (§ research: inline errors next to the field, human-readable, not just a red border). */
export function FormField({
  id,
  label,
  error,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      {children}
      {error && (
        <p id={`${id}-error`} role="alert" className="text-xs text-[var(--danger-500)]">
          {error}
        </p>
      )}
    </div>
  );
}
