"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import { ReactorProvider, ReactorView, useReactor } from "@reactor-team/js-sdk";
import { Button } from "@/components/ui/button";
import { OverlayControls } from "@/components/OverlayControls";
import { Maximize2, Minimize2 } from "lucide-react";

const WAKEUP_PROMPT =
  "Waking up in a cozy bedroom, soft morning sunlight streaming through the window, peaceful and serene atmosphere, first person view";

function WakeupResetButton() {
  const { sendCommand, status } = useReactor((state) => ({
    sendCommand: state.sendCommand,
    status: state.status,
  }));

  const handleReset = async () => {
    try {
      await sendCommand("reset", {});
      console.log("Reset command sent");
    } catch (error) {
      console.error("Failed to send reset:", error);
    }
  };

  if (status !== "ready") return null;

  return (
    <Button
      size="xs"
      variant="destructive"
      onClick={handleReset}
      className="backdrop-blur-sm"
    >
      Reset
    </Button>
  );
}

function WakeupPromptSetter() {
  const { sendCommand, status } = useReactor((state) => ({
    sendCommand: state.sendCommand,
    status: state.status,
  }));
  const sentRef = useRef(false);

  useEffect(() => {
    if (status === "ready" && !sentRef.current) {
      sentRef.current = true;
      sendCommand("set_prompt", { prompt: WAKEUP_PROMPT }).catch(console.error);
    }
  }, [status, sendCommand]);

  return null;
}

export default function WakeupPage() {
  const [jwtToken, setJwtToken] = useState<string | undefined>(undefined);
  const [isLocalMode, setIsLocalMode] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [apiKey, setApiKey] = useState("");
  const [isFetching, setIsFetching] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Check for API key in env (for auto-connect when opened from Unity)
  useEffect(() => {
    const key = process.env.NEXT_PUBLIC_REACTOR_API_KEY;
    if (key && key.startsWith("rk_")) {
      setApiKey(key);
    }
  }, []);

  useEffect(() => {
    document.documentElement.classList.add("dark");
  }, []);

  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };
    document.addEventListener("fullscreenchange", handleFullscreenChange);
    return () =>
      document.removeEventListener("fullscreenchange", handleFullscreenChange);
  }, []);

  const toggleFullscreen = useCallback(async () => {
    try {
      if (!document.fullscreenElement) {
        await document.documentElement.requestFullscreen();
        setIsFullscreen(true);
      } else {
        await document.exitFullscreen();
        setIsFullscreen(false);
      }
    } catch (error) {
      console.error("Fullscreen error:", error);
    }
  }, []);

  // Fetch JWT when API key changes
  useEffect(() => {
    if (apiKey.toLowerCase() === "local") {
      setIsLocalMode(true);
      setJwtToken(undefined);
      setError(null);
      return;
    }
    setIsLocalMode(false);
    if (!apiKey) {
      setJwtToken(undefined);
      setError(null);
      return;
    }
    const timeoutId = setTimeout(async () => {
      setIsFetching(true);
      setError(null);
      try {
        const { fetchInsecureJwtToken } = await import("@reactor-team/js-sdk");
        const token = await fetchInsecureJwtToken(apiKey);
        setJwtToken(token);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to fetch token");
        setJwtToken(undefined);
      } finally {
        setIsFetching(false);
      }
    }, 500);
    return () => clearTimeout(timeoutId);
  }, [apiKey]);

  return (
    <div className="h-screen flex flex-col bg-background text-foreground overflow-hidden">
      <ReactorProvider
        modelName="worldcore"
        jwtToken={jwtToken}
        local={isLocalMode}
        autoConnect={!!jwtToken || isLocalMode}
      >
        <WakeupPromptSetter />
        {/* Minimal header - wakeup scene */}
        <header className="shrink-0 flex items-center justify-between p-3 border-b border-border">
          <h1 className="text-lg font-medium">Wake Up</h1>
          <div className="flex items-center gap-2">
            {!jwtToken && !isLocalMode && (
              <input
                type="password"
                value={apiKey}
                onChange={(e) => setApiKey(e.target.value)}
                placeholder="API key (rk_...)"
                className="h-8 px-2 text-sm rounded border border-border bg-background w-40"
              />
            )}
            {error && (
              <span className="text-xs text-destructive">{error}</span>
            )}
          </div>
        </header>

        <main
          className={`flex-1 min-h-0 ${isFullscreen ? "flex flex-col" : "p-3"}`}
        >
          <div
            className={`relative bg-black rounded-lg overflow-hidden border border-border flex-1 min-h-0`}
          >
            <div className="absolute inset-0">
              <ReactorView className="w-full h-full object-contain" />
            </div>
            <OverlayControls className="absolute inset-0" />

            <div className="absolute top-3 left-3 pointer-events-auto">
              <WakeupResetButton />
            </div>

            <div className="absolute top-3 right-3 pointer-events-auto">
              <Button
                size="xs"
                variant="secondary"
                onClick={toggleFullscreen}
                className="backdrop-blur-sm bg-black/40 border-white/10 hover:bg-black/60"
              >
                {isFullscreen ? (
                  <Minimize2 className="w-3.5 h-3.5" />
                ) : (
                  <Maximize2 className="w-3.5 h-3.5" />
                )}
              </Button>
            </div>
          </div>
        </main>
      </ReactorProvider>
    </div>
  );
}
