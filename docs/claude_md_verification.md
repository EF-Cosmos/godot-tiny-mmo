# CLAUDE.md Verification Report

This document lists the discrepancies found between `CLAUDE.md` and the actual codebase as of 2026-01-05.

## 1. Missing Microservices
*   **Claim**: `CLAUDE.md` lists `Game.GameMapService` in the "Directory Structure" and "Microservices Details" sections.
*   **Reality**: The folder `GameBackend/src/Game.GameMapService` does not exist. This service appears to be planned (Phase 5 in Migration Plan) but not yet implemented.

## 2. Incorrect Directory Paths
*   **Claim**: `CLAUDE.md` lists `source/common/microservices/` in the "Directory Structure".
*   **Reality**: The actual path is `source/common/network/microservices/`.

## 3. Unimplemented Features (Orleans & Redis)
*   **Claim**: `CLAUDE.md` states that the project uses "Microsoft Orleans 8.2.0" for distributed computing.
*   **Reality**: Neither `Game.GameService` nor `Game.AuthService` contains any Orleans configuration or dependencies in their `Program.cs` files. They are standard ASP.NET Core Web APIs.
*   **Claim**: `CLAUDE.md` states that `Game.RoomService` and `Game.ChatService` use Redis.
*   **Reality**: 
    *   `Game.RoomService` uses an in-memory `GameServerManager` singleton.
    *   `Game.ChatService` uses SignalR without an explicit Redis backplane configuration in `Program.cs`.

## 4. API Endpoint Mismatches
*   **Claim**: The "Authentication Flow" section lists endpoints like `/v1/login`, `/v1/guest`, `/v1/world/characters`.
*   **Reality**: The actual code in `source/common/network/gateway_api.gd` uses endpoints matching the Ocelot configuration:
    *   `/api/auth/login`
    *   `/api/auth/guest`
    *   `/api/game/world/characters`
    *   `/api/game/worlds`

## 5. Legacy Architecture Descriptions
*   **Claim**: The "Three Server Model (Godot Native)" section describes the **Gateway Server** as an "HTTP REST API handling authentication".
*   **Reality**: In the new hybrid architecture, the **API Gateway (Ocelot)** handles HTTP REST API authentication. The **Godot Gateway Server** (`source/server/gateway`) acts as a connection broker for game sessions (ENet/TCP), forwarding clients to the appropriate World Server.

## 6. Minor Discrepancies
*   **Claim**: `CLAUDE.md` implies `Game.Shared` contains `ConsulExtensions.cs` directly under `Consul/`.
*   **Reality**: Verified as correct.
*   **Claim**: `CLAUDE.md` implies `Game.Shared` contains `User.MemoryPack.cs`.
*   **Reality**: Verified as correct.

## Recommendations for CLAUDE.md Update
1.  Remove `Game.GameMapService` from the "Directory Structure" and "Microservices Details" sections, or mark it as "Planned".
2.  Update the path `source/common/microservices/` to `source/common/network/microservices/`.
3.  Remove references to "Orleans" until it is actually implemented.
4.  Clarify that Redis is currently optional or not yet fully integrated in Room/Chat services code.
5.  Update the "Authentication Flow" endpoints to match `gateway_api.gd` (`/api/auth/...`, `/api/game/...`).
6.  Update the description of the Godot Gateway Server to reflect its role in the hybrid architecture (connection broker vs REST API).
