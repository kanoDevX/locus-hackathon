import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { MapPin, GraduationCap, Calendar } from "lucide-react";

export function EntryPreviewCard() {
  return (
    <Card className="relative overflow-hidden">
      <div className="absolute right-3 top-3 z-10">
        <Badge variant="brand">Preview</Badge>
      </div>
      <CardHeader className="opacity-90">
        <div className="flex items-center gap-2 text-sm font-medium">
          <GraduationCap className="size-4 text-[var(--brand-600)] dark:text-[var(--brand-300)]" />
          Computer Science — Nazarbayev University
        </div>
      </CardHeader>
      <CardContent className="space-y-3 opacity-80">
        <div className="flex items-center gap-4 text-xs text-[var(--neutral-500)]">
          <span className="flex items-center gap-1">
            <MapPin className="size-3.5" /> Astana, Kazakhstan
          </span>
          <span className="flex items-center gap-1">
            <Calendar className="size-3.5" /> 5 months left
          </span>
        </div>
        <Skeleton className="h-3 w-11/12" />
        <Skeleton className="h-3 w-4/5" />
        <div className="flex gap-2 pt-1">
          <Skeleton className="h-6 w-20 rounded-full" />
          <Skeleton className="h-6 w-24 rounded-full" />
        </div>
      </CardContent>
    </Card>
  );
}
