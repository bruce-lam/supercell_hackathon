"use client";

import { useState } from "react";
import { useReactor, useReactorMessage } from "@reactor-team/js-sdk";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { Sparkles, Loader2, ChevronDown } from "lucide-react";

interface PromptInputProps {
  className?: string;
  openaiKey?: string;
}

// State message from model
interface ModelState {
  chunk_index: number;
  current_prompt: string | null;
  current_keyboard_action: string;
  current_camera_action: string;
  current_movement_speed: number;
  current_rotation_speed: number;
  pending_keyboard_action: string;
  pending_camera_action: string;
  pending_movement_speed: number;
  pending_rotation_speed: number;
}

interface StateMessage {
  type: "state";
  data: ModelState;
}

// Example prompts for quick selection
const EXAMPLE_PROMPTS = [
  "A serene forest path with sunlight filtering through the trees",
  "A bustling city street at night with neon lights",
  "An underwater coral reef with colorful fish swimming",
  "A snowy mountain landscape with a frozen lake",
  "A medieval castle courtyard with stone walls",
  "A futuristic space station corridor with glowing panels",
];

export function PromptInput({ className = "", openaiKey = "" }: PromptInputProps) {
  const { sendCommand, status } = useReactor((state) => ({
    sendCommand: state.sendCommand,
    status: state.status,
  }));

  const [prompt, setPrompt] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [isEnhancing, setIsEnhancing] = useState(false);
  const [enhanceError, setEnhanceError] = useState<string | null>(null);
  const [showExamples, setShowExamples] = useState(false);
  
  // Track current prompt from model state
  const [currentPrompt, setCurrentPrompt] = useState<string | null>(null);

  // Listen for state messages from the model
  useReactorMessage((message: StateMessage) => {
    if (message?.type === "state" && message.data) {
      // Clear input and current prompt display on model reset
      if (message.data.chunk_index === 0 && message.data.current_prompt === null) {
        setPrompt("");
        setCurrentPrompt(null);
        return;
      }
      setCurrentPrompt(message.data.current_prompt);
    }
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!prompt.trim() || status !== "ready") return;

    setIsSending(true);
    try {
      await sendCommand("set_prompt", { prompt });
      console.log("Prompt sent:", prompt);
      // Clear the input after sending
      setPrompt("");
    } catch (error) {
      console.error("Failed to send prompt:", error);
    } finally {
      setIsSending(false);
    }
  };

  const handleEnhance = async () => {
    if (!prompt.trim()) return;
    
    setIsEnhancing(true);
    setEnhanceError(null);
    
    try {
      const response = await fetch("/api/enhance-prompt", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ 
          prompt: prompt.trim(),
          // Pass current prompt as context for enhancement
          previousPrompt: currentPrompt,
          // Pass client-provided OpenAI key if available
          openaiApiKey: openaiKey || undefined,
        }),
      });
      
      const data = await response.json();
      
      if (!response.ok) {
        throw new Error(data.error || "Failed to enhance prompt");
      }
      
      setPrompt(data.enhancedPrompt);
    } catch (error) {
      console.error("[PromptInput] Enhancement error:", error);
      setEnhanceError(error instanceof Error ? error.message : "Failed to enhance");
    } finally {
      setIsEnhancing(false);
    }
  };

  const handleExampleSelect = (examplePrompt: string) => {
    setPrompt(examplePrompt);
    setShowExamples(false);
  };

  const isDisabled = status !== "ready";
  const hasEnhanceKey = !!openaiKey;
  
  // Use current prompt as placeholder, or fallback to default
  const placeholderText = currentPrompt 
    ? `Current: ${currentPrompt.length > 50 ? currentPrompt.slice(0, 50) + "..." : currentPrompt}`
    : "Describe the scene...";

  return (
    <div
      className={cn(
        "flex flex-col gap-2",
        isDisabled && "opacity-50 pointer-events-none",
        className
      )}
    >
      <div className="flex items-center gap-2">
        <span className="text-xs font-medium text-foreground">Scene Prompt</span>
        <button
          type="button"
          onClick={() => setShowExamples(!showExamples)}
          className="flex items-center gap-1 text-[10px] text-muted-foreground hover:text-foreground transition-colors"
        >
          Examples
          <ChevronDown className={cn("w-3 h-3 transition-transform", showExamples && "rotate-180")} />
        </button>
        {enhanceError && (
          <span className="text-[10px] text-destructive">{enhanceError}</span>
        )}
      </div>

      {/* Example prompts dropdown */}
      {showExamples && (
        <div className="p-2 rounded-md bg-muted border border-border max-h-32 overflow-y-auto">
          <div className="space-y-0.5">
            {EXAMPLE_PROMPTS.map((examplePrompt, index) => (
              <button
                key={index}
                onClick={() => handleExampleSelect(examplePrompt)}
                className="w-full text-left px-2 py-1 text-xs text-muted-foreground hover:text-foreground hover:bg-background rounded transition-colors truncate"
              >
                {examplePrompt}
              </button>
            ))}
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit} className="flex gap-2">
        <Input
          type="text"
          value={prompt}
          onChange={(e) => setPrompt(e.target.value)}
          placeholder={placeholderText}
          disabled={isDisabled}
          className="flex-1 h-10"
        />
        
        {/* Enhance button */}
        <Button
          type="button"
          variant={hasEnhanceKey ? "secondary" : "ghost"}
          size="default"
          onClick={handleEnhance}
          disabled={isDisabled || !prompt.trim() || isEnhancing || !hasEnhanceKey}
          className={cn("gap-1 shrink-0 h-10", !hasEnhanceKey && "opacity-40")}
          title={hasEnhanceKey ? "Enhance prompt with AI" : "Add your OpenAI key above to enable"}
        >
          {isEnhancing ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Sparkles className="h-4 w-4" />
          )}
          <span className="hidden sm:inline">{isEnhancing ? "..." : "Enhance"}</span>
        </Button>
        
        <Button
          type="submit"
          disabled={isDisabled || !prompt.trim() || isSending}
          size="default"
          variant="default"
          className="h-10"
        >
          {isSending ? "..." : "Set Prompt"}
        </Button>
      </form>
    </div>
  );
}
