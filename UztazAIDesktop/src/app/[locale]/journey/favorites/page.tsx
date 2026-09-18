"use client";

import { useTranslations } from "next-intl";
import { Star, Trash2 } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useFavorites, useRemoveFavorite } from "@/lib/api/favorites";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

export default function FavoritesPage() {
  const t = useTranslations("favorites");
  const profileId = useAuthStore((s) => s.activeProfileId);
  const { data: favorites, isLoading } = useFavorites(profileId);
  const removeFavorite = useRemoveFavorite(profileId);

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>

      <div className="mt-6 flex flex-col gap-3">
        {isLoading && Array.from({ length: 2 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}

        {!isLoading && favorites?.length === 0 && (
          <Card>
            <CardContent className="p-8 text-center text-sm text-[var(--neutral-500)]">{t("empty")}</CardContent>
          </Card>
        )}

        {favorites?.map((fav) => (
          <Card key={fav.favoriteId}>
            <CardContent className="flex items-center justify-between gap-3 p-4">
              <div className="flex items-center gap-3">
                <Star className="size-4 fill-[var(--warning-500)] text-[var(--warning-500)]" />
                <div>
                  <p className="text-sm font-semibold">{fav.program.programName}</p>
                  <p className="text-xs text-[var(--neutral-500)]">
                    {fav.program.universityName} · {fav.program.country}
                  </p>
                  {fav.note && <p className="mt-1 text-xs italic text-[var(--neutral-400)]">{t("note")}: {fav.note}</p>}
                </div>
              </div>
              <Button
                variant="ghost"
                size="icon"
                aria-label={t("remove")}
                onClick={() => removeFavorite.mutate(fav.program.programId)}
              >
                <Trash2 className="size-4 text-[var(--danger-500)]" />
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
