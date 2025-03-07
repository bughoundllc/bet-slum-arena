# NET-SLUM Agent Information

This document provides high-level guidance and context for AI agents working with the NET-SLUM project.

## Project Context

NET-SLUM is a Unity-based platform for competitive games with betting functionality. The project follows a modular architecture where different game types can be implemented within a unified framework.

## Core Concepts

### Game Flow

The project follows a specific flow pattern:
1. **Initialization** - The MainController initializes the appropriate ShowRunner
2. **Game Loading** - The ShowRunner loads the selected game
3. **Betting Period** - A timed period for placing bets
4. **Match Execution** - The game match is run with competitors
5. **Results & Payouts** - Match ends, winner determined, cycle restarts

### Modularity

The project is designed with modularity in mind:
- New game types can be added by creating a new game-specific MatchRunner
- The ShowRunner handles the common flow regardless of game type
- Data structures (Competitor, CompetitorStat) are reused across games

## Best Practices

Based on analysis of the existing codebase:

### Code Structure

1. **Namespace Convention**: Use `bet_slum` as the root namespace, with sub-namespaces for specific game types (e.g., `bet_slum.CombatArena`).

2. **Inheritance Pattern**: Follow the pattern of extending abstract base classes:
   - Create game-specific MatchRunners that inherit from the base MatchRunner
   - Implement required methods like `InitializeMatchEnvironment` and `StartMatch`

3. **Component Organization**: Keep game-specific code within its respective folder in the Games directory.

### Unity-Specific

1. **Scene Management**: Games are loaded as scenes through the ShowRunner.

2. **Prefab Usage**: Game agents/entities appear to be implemented as prefabs that are instantiated at runtime.

3. **SerializeField Annotations**: Use `[SerializeField]` for inspector-configurable fields following the existing pattern.

4. **UnityEvents**: Use UnityEvents for communication between components (as seen in the CombatArena implementation).

### Network Communication

1. **API Access**: Use the NetworkController static methods for all API communications.

2. **Mock/Live Modes**: Support both mock (local) and live (networked) modes for testing and production.

### Code Consistency

1. **Async Pattern**: The codebase uses a custom `Awaitable<T>` pattern for async operations. Follow this pattern for consistency.

2. **Logging**: Use `Debug.Log` statements with prefixes indicating the source (e.g., "SR:" for ShowRunner, "MR:" for MatchRunner).

3. **Comments**: Add TODO comments for areas that need future attention or synchronization with external systems.

## Connection to External Systems

The project appears to connect to an external service:
- The connection is managed through the NetworkController
- There's a reference to a "bet-slum-shared lib" suggesting shared code with other projects
- The system supports authentication via session tokens

## Common Pitfalls

1. **Game Selection**: The game type is set in the MainController and should match one of the available scene names.

2. **Competitor Structure**: The code assumes specific structures for competitors (e.g., one competitor per team in CombatArena).

3. **Network Configuration**: Be careful when switching between development and production network endpoints.

## When Making Changes

1. **Test Both Modes**: Always test both mock and live modes when making changes to the core flow.

2. **Scene References**: If adding new games, ensure scene names match the game type enum in MainController.

3. **Data Synchronization**: Be aware of the note about synchronizing with the "bet-slum-shared lib" in Competitor.cs.

## Project Evolution Notes

This section will track important changes or corrections to assumptions about the project.

- Initial documentation created based on code analysis (not yet corrected by user feedback) 