# Agents That Remember: Designing Memory-Driven AI Systems

![aigenius](media/cover.png)

[![GitHub Repository](https://img.shields.io/badge/GitHub-Repository-181717?logo=github&style=for-the-badge)](https://github.com/binarytrails-ai/agentcamp-workshop)

---

## Session Overview

The real power of an AI agent isn't in the model. It's in how well it manages context.

Large language models are inherently stateless. Without carefully designed memory and context strategies, agents lose track of goals, struggle with long-running tasks, and produce generic, non-personalized responses.

In this hands-on workshop, we explore why context is the foundation of intelligent agent behavior — and how to design it intentionally.

### What You'll Learn

We'll explore the core memory types that shape agent behavior:

- **Short-term memory** — working context for current reasoning loops
- **Episodic memory** — past interactions and task history
- **Semantic memory** — structured knowledge about concepts and relationships
- **Long-term memory** — persistent user data across sessions

Through progressive labs, we'll evolve a simple stateless agent into a memory-driven system that maintains goal continuity, adapts over time, and produces more consistent, personalized decisions.

This session is not about adding complexity — it's about designing stronger foundations.

By the end, you will walk away with:

- A clear understanding of different memory types and when to use each
- Practical patterns for structuring, storing, and retrieving memory effectively
- Architectural strategies for handling context window limits
- A reference implementation approach you can apply to your own agent builds

---

## Prerequisites

This session is designed for practitioners who have:

- Some experience working with AI models (OpenAI, Azure OpenAI, etc.)
- Basic programming knowledge (C# or similar languages)
- Familiarity with REST APIs and web applications
- Curiosity about AI agent architecture and memory systems

---

## What You'll Build

This repository provides a reference implementation of an AI-powered travel assistant built with the Microsoft Agent Framework. Using this as a running example, you'll learn how to design and implement different memory types to create a more intelligent, and context-aware agent.

This workshop consists of the following labs introducing different memory types and capabilities to the travel assistant:

1. [Lab 1: Personalization using Long-Term Memory](./01-lab-long-term-memory.md) (~20 minutes)
2. [Lab 2: Conversation Recall with Episodic Memory](./02-lab-episodic-memory.md) (~20 minutes)
3. [Lab 3: Knowledge Retrieval with Semantic Memory](./03-lab-semantic-memory.md) (~20 minutes)

---

## Architecture Overview

This reference implementation demonstrates a production-ready AI travel assistant with a modern, cloud-native architecture designed to support multiple memory types:

![Architecture Diagram](media/architecture.png)

### Components

- **Frontend (Container App)** - Interactive user interface built with CopilotKit for seamless agent conversations and real-time interactions
- **Backend API (Container App)** - .NET 10 ASP.NET Core API that hosts the Travel Assistant agent, publishes via AG-UI protocol, and manages execution, state, and tool interactions
- **Cosmos DB** - Azure Cosmos DB for all application data storage
- **Azure AI Foundry** - Provides access to Azure OpenAI models for natural language understanding and generation
- **Observability** - OpenTelemetry for distributed tracing and Azure Monitor for centralized logging and monitoring of agent interactions

---

## Getting Started

Follow the instructions on the **[Environment Setup Guide](./00-setup_instructions.md)** to set up your development environment.

When you're ready, move on to [Lab 1: Personalization using Long-Term Memory](./01-lab-long-term-memory.md) to begin building your memory-driven agent.

Happy coding!

---