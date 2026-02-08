"use client";

import { useState, useCallback, useEffect } from "react";
import { useReactor } from "@reactor-team/js-sdk";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Upload, ImageIcon, Check } from "lucide-react";

interface ImageUploaderProps {
  className?: string;
}

// Target dimensions matching the model's expected input.
// Images are normalized to exactly this size (letter/pillar-boxed) and
// re-encoded as JPEG to keep the payload under the WebRTC data channel limit.
const TARGET_WIDTH = 640;
const TARGET_HEIGHT = 360;
const INITIAL_JPEG_QUALITY = 0.7;
// WebRTC data channel safe limit – the base64 payload (plus JSON wrapper)
// must stay under this. 200 KB is well within the 256 KB SCTP limit.
const MAX_BASE64_BYTES = 200_000;

// Example starting images - using picsum.photos which has reliable CORS support
const EXAMPLE_IMAGES = [
  {
    id: "forest",
    title: "Forest",
    url: "https://picsum.photos/seed/forest/640/360",
  },
  {
    id: "mountain",
    title: "Mountain",
    url: "https://picsum.photos/seed/mountain/640/360",
  },
  {
    id: "city",
    title: "City",
    url: "https://picsum.photos/seed/city/640/360",
  },
];

/**
 * Normalize an image to exactly TARGET_WIDTH × TARGET_HEIGHT (letter/pillar-boxed
 * with a black background), then JPEG-encode it. If the resulting base64 exceeds
 * MAX_BASE64_BYTES the quality is reduced iteratively until it fits.
 */
function resizeImage(dataUrl: string): Promise<string> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => {
      const canvas = document.createElement("canvas");
      canvas.width = TARGET_WIDTH;
      canvas.height = TARGET_HEIGHT;
      const ctx = canvas.getContext("2d");
      if (!ctx) {
        reject(new Error("Failed to get canvas context"));
        return;
      }

      // Black background for letter/pillar-boxing
      ctx.fillStyle = "#000000";
      ctx.fillRect(0, 0, TARGET_WIDTH, TARGET_HEIGHT);

      // Fit the image inside the canvas preserving aspect ratio
      const aspectRatio = img.width / img.height;
      const targetAspect = TARGET_WIDTH / TARGET_HEIGHT;
      let drawWidth: number, drawHeight: number, offsetX: number, offsetY: number;

      if (aspectRatio > targetAspect) {
        // Image is wider → pillar-box (black bars top/bottom)
        drawWidth = TARGET_WIDTH;
        drawHeight = TARGET_WIDTH / aspectRatio;
        offsetX = 0;
        offsetY = (TARGET_HEIGHT - drawHeight) / 2;
      } else {
        // Image is taller → letter-box (black bars left/right)
        drawHeight = TARGET_HEIGHT;
        drawWidth = TARGET_HEIGHT * aspectRatio;
        offsetX = (TARGET_WIDTH - drawWidth) / 2;
        offsetY = 0;
      }

      ctx.imageSmoothingEnabled = true;
      ctx.imageSmoothingQuality = "high";
      ctx.drawImage(img, 0, 0, img.width, img.height, offsetX, offsetY, drawWidth, drawHeight);

      // Encode as JPEG, reducing quality iteratively until the base64 fits
      let quality = INITIAL_JPEG_QUALITY;
      let result = canvas.toDataURL("image/jpeg", quality);

      while (result.length > MAX_BASE64_BYTES && quality > 0.1) {
        quality -= 0.1;
        result = canvas.toDataURL("image/jpeg", quality);
      }

      console.log(
        `[ImageUploader] Normalized image: ${TARGET_WIDTH}x${TARGET_HEIGHT}, ` +
        `quality=${quality.toFixed(1)}, size=${(result.length / 1024).toFixed(1)}KB`
      );
      resolve(result);
    };
    img.onerror = () => reject(new Error("Failed to load image for resizing"));
    img.src = dataUrl;
  });
}

export function ImageUploader({ className = "" }: ImageUploaderProps) {
  const { sendCommand, status } = useReactor((state) => ({
    sendCommand: state.sendCommand,
    status: state.status,
  }));

  const [uploadedImage, setUploadedImage] = useState<string | null>(null);
  const [selectedExampleId, setSelectedExampleId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSent, setIsSent] = useState(false);

  // Reset all state when disconnected so UI is clean on reconnect
  useEffect(() => {
    if (status !== "ready") {
      setUploadedImage(null);
      setSelectedExampleId(null);
      setIsLoading(false);
      setError(null);
      setIsSent(false);
    }
  }, [status]);

  // Resize then store
  const storeResized = useCallback(async (dataUrl: string) => {
    const resized = await resizeImage(dataUrl);
    setUploadedImage(resized);
    setIsSent(false);
  }, []);

  // Handle image file upload and convert to base64
  const handleImageUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setError(null);
    setIsLoading(false); // Cancel any in-progress example load
    const reader = new FileReader();
    reader.onloadend = async () => {
      try {
        const base64String = reader.result as string;
        await storeResized(base64String);
        setSelectedExampleId(null);
      } catch {
        setError("Failed to process image");
      }
    };
    reader.onerror = () => {
      setError("Failed to read file");
    };
    reader.readAsDataURL(file);

    // Reset input so the same file can be re-selected
    event.target.value = "";
  };

  // Handle selecting an example image
  const handleExampleSelect = async (imageUrl: string, imageId: string) => {
    try {
      setIsLoading(true);
      setError(null);
      setSelectedExampleId(imageId);
      
      // Fetch the image
      const response = await fetch(imageUrl);
      if (!response.ok) {
        throw new Error(`Failed to fetch image: ${response.status}`);
      }
      
      const blob = await response.blob();

      const reader = new FileReader();
      reader.onloadend = async () => {
        try {
          const base64String = reader.result as string;
          await storeResized(base64String);
        } catch {
          setError("Failed to process image");
          setSelectedExampleId(null);
        } finally {
          setIsLoading(false);
        }
      };
      reader.onerror = () => {
        setError("Failed to read image");
        setIsLoading(false);
        setSelectedExampleId(null);
      };
      reader.readAsDataURL(blob);
    } catch (err) {
      console.error("Failed to load example image:", err);
      setError(err instanceof Error ? err.message : "Failed to load image");
      setIsLoading(false);
      setSelectedExampleId(null);
    }
  };

  // Send the uploaded image to the model as the starting frame
  const handleSetStartingImage = async () => {
    if (!uploadedImage) return;

    setError(null);
    const base64Data = uploadedImage.split(",")[1];

    try {
      await sendCommand("set_starting_image", {
        image_base64: base64Data,
      });
      setIsSent(true);
      console.log("Starting image set successfully");
    } catch (err) {
      console.error("Failed to set starting image:", err);
      setError("Failed to set image");
    }
  };

  const isDisabled = status !== "ready";

  return (
    <div
      className={cn(
        "flex flex-col gap-2",
        isDisabled && "opacity-50 pointer-events-none",
        className
      )}
    >
      <div className="flex items-center gap-2">
        <span className="text-xs font-medium text-foreground">Starting Image</span>
        {isLoading && (
          <div className="w-3 h-3 border-2 border-primary border-t-transparent rounded-full animate-spin" />
        )}
        {error && (
          <span className="text-[10px] text-destructive">{error}</span>
        )}
      </div>

      <div className="flex items-center gap-2">
        {/* Example images - horizontal scroll */}
        <div className="flex gap-1.5 overflow-x-auto">
          {EXAMPLE_IMAGES.map((example) => (
            <button
              key={example.id}
              onClick={() => handleExampleSelect(example.url, example.id)}
              disabled={isLoading}
              className={cn(
                "relative shrink-0 w-14 h-10 overflow-hidden rounded border-2 transition-all",
                selectedExampleId === example.id
                  ? "border-primary ring-2 ring-primary/30 scale-105"
                  : "border-transparent hover:border-muted-foreground/50",
                isLoading && "opacity-50 cursor-wait"
              )}
            >
              <img
                src={example.url}
                alt={example.title}
                className="w-full h-full object-cover"
                crossOrigin="anonymous"
              />
              {/* Checkmark overlay on selected */}
              {selectedExampleId === example.id && (
                <div className="absolute inset-0 bg-primary/20 flex items-center justify-center">
                  <div className="w-5 h-5 rounded-full bg-primary flex items-center justify-center">
                    <Check className="w-3 h-3 text-primary-foreground" />
                  </div>
                </div>
              )}
            </button>
          ))}
        </div>

        {/* Upload button - always clickable */}
        <label className="shrink-0 w-10 h-10 rounded border border-dashed border-border hover:border-muted-foreground/50 flex items-center justify-center cursor-pointer transition-colors">
          <Upload className="w-4 h-4 text-muted-foreground" />
          <input
            type="file"
            accept="image/*"
            onChange={handleImageUpload}
            className="hidden"
          />
        </label>

        {/* Separator + Preview + Set button when image is ready */}
        {uploadedImage && (
          <>
            <div className="w-px h-8 bg-border shrink-0" />
            <div className={cn(
              "shrink-0 w-14 h-10 rounded overflow-hidden border-2 transition-all",
              isSent ? "border-green-500 ring-2 ring-green-500/30" : "border-primary ring-2 ring-primary/30"
            )}>
              <img
                src={uploadedImage}
                alt="Preview"
                className="w-full h-full object-cover"
              />
            </div>
            {isSent ? (
              <div className="flex items-center gap-1.5 text-green-500 shrink-0">
                <Check className="w-4 h-4" />
                <span className="text-xs font-medium uppercase">Set</span>
              </div>
            ) : (
              <Button
                size="xs"
                variant="default"
                onClick={handleSetStartingImage}
                disabled={isDisabled || isLoading}
                className="shrink-0 h-8"
              >
                <ImageIcon className="w-3 h-3 mr-1" />
                Apply
              </Button>
            )}
          </>
        )}
      </div>
    </div>
  );
}
