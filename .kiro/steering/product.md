# Product Overview

CanvasSnap is a desktop screenshot application designed for capturing game canvas elements from web browsers without interfering with game processes.

## Core Capabilities

- **Safe Screen Capture**: OS-level screen capture API only, no game process interference
- **Selective Region Capture**: User-defined rectangular area capture (primarily targeting 1200×720px game canvas)
- **Privacy Masking**: Black-out specific regions to hide sensitive user information
- **Hotkey Activation**: Global hotkey for instant capture without switching windows
- **Cross-Platform**: macOS-first, Windows support planned

## Target Use Cases

- **Online Game Players**: Capture gameplay moments without triggering anti-cheat systems
- **Streamers & Content Creators**: Quick screenshot capture during live play
- **Game Documentation**: Record game state and progress safely

## Value Proposition

Traditional screenshot tools risk detection by anti-cheat systems. CanvasSnap uses only OS-standard screen capture APIs, ensuring:

- **Zero Game Interference**: No process injection, memory access, or DOM manipulation
- **Anti-Cheat Safe**: Screen-level capture only, transparent to game processes
- **Privacy Protection**: Built-in masking for sensitive on-screen information
- **Instant Workflow**: Hotkey-driven capture with automatic file management

## Key Constraints

- **Non-Invasive Design**: Absolute prohibition on browser/game process interaction
- **Coordinate-Based**: Fixed screen coordinates (does not track window movement)
- **Local-Only**: No network transmission, local storage only

---
_Created: 2025-11-27_
