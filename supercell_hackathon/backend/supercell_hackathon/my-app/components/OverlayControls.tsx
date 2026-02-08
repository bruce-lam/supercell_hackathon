"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import { useReactor, useReactorMessage } from "@reactor-team/js-sdk";
import { cn } from "@/lib/utils";

// Keyboard actions for WASD movement
type KeyboardAction = "w" | "s" | "a" | "d" | "still";

// Camera actions for mouse look
type CameraAction = "left" | "right" | "up" | "down" | "still";

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

interface KeyButtonProps {
  label: string;
  subLabel?: string;
  isPending: boolean;  // User has pressed (pending state)
  isActive: boolean;   // Currently affecting generation
  variant: "keyboard" | "camera";
  onPress: () => void;
  onRelease: () => void;
  disabled?: boolean;
  className?: string;
}

// Interactive button component - shows pending vs active states
function KeyButton({
  label,
  subLabel,
  isPending,
  isActive,
  variant,
  onPress,
  onRelease,
  disabled,
  className = "",
}: KeyButtonProps) {
  const [isPressed, setIsPressed] = useState(false);
  const pointerIdRef = useRef<number | null>(null);

  const handlePointerDown = (e: React.PointerEvent) => {
    if (disabled) return;
    e.preventDefault();
    e.currentTarget.setPointerCapture(e.pointerId);
    pointerIdRef.current = e.pointerId;
    setIsPressed(true);
    onPress();

    if ("vibrate" in navigator) {
      navigator.vibrate(10);
    }
  };

  const handlePointerUp = (e: React.PointerEvent) => {
    if (disabled || pointerIdRef.current !== e.pointerId) return;
    e.preventDefault();
    setIsPressed(false);
    pointerIdRef.current = null;
    onRelease();
  };

  const handlePointerCancel = (e: React.PointerEvent) => {
    if (disabled || pointerIdRef.current !== e.pointerId) return;
    setIsPressed(false);
    pointerIdRef.current = null;
    onRelease();
  };

  const baseClasses =
    "w-12 h-12 rounded-lg font-bold text-xs transition-all duration-150 select-none cursor-pointer flex flex-col items-center justify-center touch-none backdrop-blur-sm";

  // Determine the visual state:
  // 1. Active (currently affecting) - strongest color, solid
  // 2. Pending (pressed but not yet active) - medium color, semi-transparent
  // 3. Inactive - default dark state
  
  let stateClasses: string;
  
  if (isActive) {
    // Currently affecting generation - STRONG color
    if (variant === "keyboard") {
      stateClasses = "bg-primary text-primary-foreground shadow-lg ring-2 ring-primary/50";
    } else {
      stateClasses = "bg-accent text-accent-foreground shadow-lg ring-2 ring-accent/50";
    }
  } else if (isPending) {
    // Pending (user pressed, waiting to take effect) - TRANSPARENT/lighter
    if (variant === "keyboard") {
      stateClasses = "bg-primary/40 text-white/90 border-2 border-primary/60";
    } else {
      stateClasses = "bg-accent/40 text-white/90 border-2 border-accent/60";
    }
  } else {
    // Inactive - default dark
    stateClasses = "bg-black/40 text-white/70 border border-white/20 hover:bg-black/50";
  }

  const pressedScale = isPressed ? "scale-90" : "";

  return (
    <button
      type="button"
      className={cn(
        baseClasses,
        stateClasses,
        pressedScale,
        className
      )}
      onPointerDown={handlePointerDown}
      onPointerUp={handlePointerUp}
      onPointerCancel={handlePointerCancel}
      disabled={disabled}
      style={{ touchAction: "none" }}
    >
      <span className="text-sm font-bold">{label}</span>
      {subLabel && <span className="text-[8px] opacity-70">{subLabel}</span>}
    </button>
  );
}

interface OverlayControlsProps {
  className?: string;
}

export function OverlayControls({ className }: OverlayControlsProps) {
  const { status, sendCommand } = useReactor((state) => ({
    status: state.status,
    sendCommand: state.sendCommand,
  }));

  // Track physically pressed keys (or touched buttons) - this is what user is inputting
  const [pressedKeys, setPressedKeys] = useState<Set<string>>(new Set());

  // Track pending actions sent to model (derived from pressed keys)
  const [pendingKeyboardAction, setPendingKeyboardAction] = useState<KeyboardAction>("still");
  const [pendingCameraAction, setPendingCameraAction] = useState<CameraAction>("still");

  // Track ACTIVE actions from model state (what's actually affecting generation)
  const [activeKeyboardAction, setActiveKeyboardAction] = useState<string>("still");
  const [activeCameraAction, setActiveCameraAction] = useState<string>("still");

  // Listen for state messages from the model
  useReactorMessage((message: StateMessage) => {
    if (message?.type === "state" && message.data) {
      const state = message.data;
      setActiveKeyboardAction(state.current_keyboard_action);
      setActiveCameraAction(state.current_camera_action);
    }
  });

  // Update set of pressed keys
  const updatePressedKeys = useCallback((key: string, isPressed: boolean) => {
    setPressedKeys((prev) => {
      const newSet = new Set(prev);
      if (isPressed) {
        newSet.add(key);
      } else {
        newSet.delete(key);
      }
      return newSet;
    });
  }, []);

  // Derive Keyboard Action from pressed keys (WASD)
  useEffect(() => {
    let newAction: KeyboardAction = "still";

    if (pressedKeys.has("w")) {
      newAction = "w";
    } else if (pressedKeys.has("s")) {
      newAction = "s";
    } else if (pressedKeys.has("a")) {
      newAction = "a";
    } else if (pressedKeys.has("d")) {
      newAction = "d";
    }

    if (newAction !== pendingKeyboardAction) {
      setPendingKeyboardAction(newAction);
      if (status === "ready") {
        void sendCommand("set_keyboard_action", { action: newAction });
      }
    }
  }, [pressedKeys, pendingKeyboardAction, sendCommand, status]);

  // Derive Camera Action from pressed keys (IJKL)
  useEffect(() => {
    let newAction: CameraAction = "still";

    const i = pressedKeys.has("i");
    const j = pressedKeys.has("j");
    const k = pressedKeys.has("k");
    const l = pressedKeys.has("l");

    if (i) newAction = "up";
    else if (k) newAction = "down";
    else if (j) newAction = "left";
    else if (l) newAction = "right";

    if (newAction !== pendingCameraAction) {
      setPendingCameraAction(newAction);
      if (status === "ready") {
        void sendCommand("set_camera_action", { action: newAction });
      }
    }
  }, [pressedKeys, pendingCameraAction, sendCommand, status]);

  // Keyboard Event Listeners
  useEffect(() => {
    if (status !== "ready") return;

    const handleKeyDown = (e: KeyboardEvent) => {
      // Ignore if typing in an input field
      if (
        document.activeElement?.tagName === "INPUT" ||
        document.activeElement?.tagName === "TEXTAREA"
      ) {
        return;
      }

      const key = e.key.toLowerCase();
      if (["w", "a", "s", "d", "i", "j", "k", "l"].includes(key)) {
        updatePressedKeys(key, true);
      }
    };

    const handleKeyUp = (e: KeyboardEvent) => {
      // Ignore if typing in an input field
      if (
        document.activeElement?.tagName === "INPUT" ||
        document.activeElement?.tagName === "TEXTAREA"
      ) {
        return;
      }

      const key = e.key.toLowerCase();
      if (["w", "a", "s", "d", "i", "j", "k", "l"].includes(key)) {
        updatePressedKeys(key, false);
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    window.addEventListener("keyup", handleKeyUp);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      window.removeEventListener("keyup", handleKeyUp);
    };
  }, [status, updatePressedKeys]);

  // Helper to check if a key's action is currently active (affecting generation)
  const isKeyboardKeyActive = (key: string): boolean => {
    const keyToAction: Record<string, string> = { w: "w", s: "s", a: "a", d: "d" };
    return activeKeyboardAction === keyToAction[key];
  };

  const isCameraKeyActive = (key: string): boolean => {
    const keyToAction: Record<string, string> = { i: "up", k: "down", j: "left", l: "right" };
    return activeCameraAction === keyToAction[key];
  };

  const isDisabled = status !== "ready";

  if (isDisabled) return null;

  return (
    <div className={cn("pointer-events-none", className)}>
      {/* WASD - Bottom Left */}
      <div className="absolute bottom-4 left-4 pointer-events-auto">
        <div className="flex flex-col items-center gap-1">
          <KeyButton
            label="W"
            isPending={pressedKeys.has("w")}
            isActive={isKeyboardKeyActive("w")}
            variant="keyboard"
            onPress={() => updatePressedKeys("w", true)}
            onRelease={() => updatePressedKeys("w", false)}
            disabled={isDisabled}
          />
          <div className="flex gap-1">
            <KeyButton
              label="A"
              isPending={pressedKeys.has("a")}
              isActive={isKeyboardKeyActive("a")}
              variant="keyboard"
              onPress={() => updatePressedKeys("a", true)}
              onRelease={() => updatePressedKeys("a", false)}
              disabled={isDisabled}
            />
            <KeyButton
              label="S"
              isPending={pressedKeys.has("s")}
              isActive={isKeyboardKeyActive("s")}
              variant="keyboard"
              onPress={() => updatePressedKeys("s", true)}
              onRelease={() => updatePressedKeys("s", false)}
              disabled={isDisabled}
            />
            <KeyButton
              label="D"
              isPending={pressedKeys.has("d")}
              isActive={isKeyboardKeyActive("d")}
              variant="keyboard"
              onPress={() => updatePressedKeys("d", true)}
              onRelease={() => updatePressedKeys("d", false)}
              disabled={isDisabled}
            />
          </div>
        </div>
      </div>

      {/* IJKL Camera - Bottom Right */}
      <div className="absolute bottom-4 right-4 pointer-events-auto">
        <div className="flex flex-col items-center gap-1">
          <KeyButton
            label="I"
            subLabel="UP"
            isPending={pressedKeys.has("i")}
            isActive={isCameraKeyActive("i")}
            variant="camera"
            onPress={() => updatePressedKeys("i", true)}
            onRelease={() => updatePressedKeys("i", false)}
            disabled={isDisabled}
          />
          <div className="flex gap-1">
            <KeyButton
              label="J"
              subLabel="LEFT"
              isPending={pressedKeys.has("j")}
              isActive={isCameraKeyActive("j")}
              variant="camera"
              onPress={() => updatePressedKeys("j", true)}
              onRelease={() => updatePressedKeys("j", false)}
              disabled={isDisabled}
            />
            <KeyButton
              label="K"
              subLabel="DOWN"
              isPending={pressedKeys.has("k")}
              isActive={isCameraKeyActive("k")}
              variant="camera"
              onPress={() => updatePressedKeys("k", true)}
              onRelease={() => updatePressedKeys("k", false)}
              disabled={isDisabled}
            />
            <KeyButton
              label="L"
              subLabel="RIGHT"
              isPending={pressedKeys.has("l")}
              isActive={isCameraKeyActive("l")}
              variant="camera"
              onPress={() => updatePressedKeys("l", true)}
              onRelease={() => updatePressedKeys("l", false)}
              disabled={isDisabled}
            />
          </div>
        </div>
      </div>
    </div>
  );
}
