"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { CheckCircle2, Lightbulb } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useReviewEssay } from "@/lib/api/essays";
import { ApiError } from "@/lib/api/client";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input, Label, Textarea } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

export default function EssaysPage() {
  const t = useTranslations("essays");
  const profileId = useAuthStore((s) => s.activeProfileId);
  const review = useReviewEssay(profileId);
  const [prompt, setPrompt] = useState("");
  const [essayText, setEssayText] = useState("");

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    try {
      await review.mutateAsync({ essayText, prompt: prompt || undefined });
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Could not review essay");
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>

      <form onSubmit={onSubmit} className="mt-6 flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label>{t("prompt")}</Label>
          <Input value={prompt} onChange={(e) => setPrompt(e.target.value)} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>{t("essayText")}</Label>
          <Textarea rows={10} required minLength={50} value={essayText} onChange={(e) => setEssayText(e.target.value)} />
        </div>
        <Button type="submit" disabled={review.isPending} className="self-start">
          {t("submit")}
        </Button>
      </form>

      {review.data && (
        <Card className="mt-6">
          <CardHeader className="flex-row items-center justify-between">
            <CardTitle className="text-sm">Feedback</CardTitle>
            <Badge variant={review.data.fallbackUsed ? "warning" : "brand"}>
              {review.data.fallbackUsed ? "Simplified" : "AI"}
            </Badge>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <div>
              <p className="mb-1.5 flex items-center gap-1.5 text-sm font-medium">
                <CheckCircle2 className="size-4 text-[var(--success-500)]" /> {t("strengths")}
              </p>
              <ul className="list-disc pl-5 text-sm text-[var(--neutral-600)] dark:text-[var(--neutral-300)]">
                {review.data.strengths.map((s, i) => (
                  <li key={i}>{s}</li>
                ))}
              </ul>
            </div>
            <div>
              <p className="mb-1.5 flex items-center gap-1.5 text-sm font-medium">
                <Lightbulb className="size-4 text-[var(--warning-500)]" /> {t("improvements")}
              </p>
              <ul className="list-disc pl-5 text-sm text-[var(--neutral-600)] dark:text-[var(--neutral-300)]">
                {review.data.suggestedImprovements.map((s, i) => (
                  <li key={i}>{s}</li>
                ))}
              </ul>
            </div>
            <p className="text-sm">
              <span className="font-medium">{t("clarity")}: </span>
              {review.data.clarityFeedback}
            </p>
            <p className="text-sm">
              <span className="font-medium">{t("structure")}: </span>
              {review.data.structureFeedback}
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
