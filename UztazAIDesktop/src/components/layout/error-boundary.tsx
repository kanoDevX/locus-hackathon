"use client";

import React from "react";
import { AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";

interface Props {
  children: React.ReactNode;
  title?: string;
  body?: string;
  retryLabel?: string;
}

interface State {
  hasError: boolean;
}

/** Wraps the journey shell so a runtime error anywhere below it never shows a raw stack trace or
 * a blank white screen during the live demo (§7 reliability). */
export class ErrorBoundary extends React.Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch(error: unknown) {
    console.error("UstazAI UI error:", error);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="flex min-h-[40vh] flex-col items-center justify-center gap-3 p-8 text-center">
          <span className="flex size-12 items-center justify-center rounded-full bg-[var(--warning-100)] text-[var(--warning-500)]">
            <AlertTriangle className="size-6" />
          </span>
          <p className="text-base font-semibold">{this.props.title ?? "Something went sideways"}</p>
          <p className="max-w-sm text-sm text-[var(--neutral-500)]">
            {this.props.body ?? "That didn't work as expected. Your progress is saved — try again."}
          </p>
          <Button variant="secondary" size="sm" onClick={() => this.setState({ hasError: false })}>
            {this.props.retryLabel ?? "Try again"}
          </Button>
        </div>
      );
    }
    return this.props.children;
  }
}
