# WorldCore - Interactive World Model Demo

A real-time interactive world model demo built with the Reactor JS SDK. Control the generation with WASD movement, camera rotation, custom prompts, and starting images.

## Features

- **Floating WASD Controls**: Movement controls (bottom-left) overlaid on the video
- **Floating Camera Controls (IJKL)**: Camera rotation (bottom-right) overlaid on the video
- **Simultaneous Control**: Use keyboard and camera controls at the same time
- **Speed Adjustment**: Compact sliders in the overlay for movement and rotation speeds
- **AI Prompt Enhancement**: Use GPT-4o-mini to enhance your scene descriptions
- **Custom Starting Image**: Upload or select an example image to start from
- **Live Prompt Changes**: Change the scene description during generation
- **Reset**: Restart generation while preserving settings

## Getting Started

### Prerequisites

- Node.js 18+
- pnpm (recommended) or npm

### Installation

```bash
pnpm install
```

### Environment Variables

Copy `.env.example` to `.env.local` and fill in your keys:

```bash
cp .env.example .env.local
```

### Development

```bash
pnpm dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

### API Key

Enter your Reactor API key in the header, or type `local` to connect to a locally running model.

## UI Layout

```
┌─────────────────────────────────────────────────────────┐
│ WorldCore            [logo]        [API Key] [Connect] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   [Reset]        [Move: ▬▬▬] [Rotate: ▬▬▬]             │
│                                                         │
│                    VIDEO STREAM                         │
│                                                         │
│   ┌───┐                                        ┌───┐   │
│   │ W │                                        │ I │   │
│   ├───┼───┼───┐                        ┌───┼───┼───┤   │
│   │ A │ S │ D │                        │ J │ K │ L │   │
│   └───┴───┴───┘                        └───┴───┴───┘   │
│                                                         │
├─────────────────────────────────────────────────────────┤
│ [Scene Prompt _______________] [Enhance] [Set]  [Images]│
└─────────────────────────────────────────────────────────┘
```

## Controls

### Keyboard Controls

| Key | Action |
|-----|--------|
| W | Move forward |
| S | Move backward |
| A | Strafe left |
| D | Strafe right |
| I | Look up |
| K | Look down |
| J | Turn left |
| L | Turn right |

### Touch Controls

All controls can be used via touch on mobile devices. Tap and hold the on-screen buttons for continuous input.

## Architecture

This demo uses:

- **Next.js 15** - React framework
- **@reactor-team/js-sdk** - Reactor SDK for WebRTC streaming
- **@reactor-team/ui** - Reactor design system
- **Tailwind CSS 4** - Styling

## Model Commands

The WorldCore model (`worldcore`) exposes the following commands:

- `set_keyboard_action` - Set WASD movement (w/s/a/d/still)
- `set_camera_action` - Set camera rotation (left/right/up/down/still)
- `set_movement_speed` - Set movement speed (0.0-1.0)
- `set_rotation_speed` - Set rotation speed (0.0-1.0)
- `set_prompt` - Set the scene description
- `set_starting_image` - Set a starting image (base64)
- `reset` - Restart generation

## License

Copyright (c) 2026 Reactor Technologies, Inc. All rights reserved.
