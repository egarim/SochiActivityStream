# Chat.Abstractions

Pure interfaces and DTOs for the Chat Service — zero external dependencies.

## Overview

This package defines the contracts for a real-time messaging system supporting:
- **Direct Conversations** — 1:1 private chats with deduplication
- **Group Conversations** — Multi-user chats with roles (Owner/Admin/Member)
- **Messages** — Send, edit, delete (for self/everyone), reply-to threading
- **Read Receipts** — Per-participant tracking with unread counts

## Key Types

### DTOs
- `ConversationDto` — Conversation with participants, last message, unread count
- `MessageDto` — Message with sender, body, media, reply-to
- `ConversationParticipantDto` — Participant with role, read state
- `ReadReceiptDto` — Read receipt for a participant

### Interfaces
- `IChatService` — Main service for conversations and messages
- `IChatNotifier` — Real-time event dispatcher (integrates with Realtime Hub)
- `IConversationStore` — Storage for conversations
- `IMessageStore` — Storage for messages

## Multi-Tenant

All entities are scoped by `TenantId` for isolation.
