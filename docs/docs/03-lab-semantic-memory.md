# Lab 3: Knowledge Retrieval with Semantic Memory

> **Duration**: ~20 minutes

In this lab, you will upgrade the agent from static knowledge to **semantic memory** using vector embeddings and similarity search.

You will enable your agent to understand the _meaning_ behind words, not just exact text matches. By the end of this lab, you will:

- ✅ Understand semantic memory and how it differs from keyword search
- ✅ Learn how vector embeddings capture meaning
- ✅ Implement vector search in Cosmos DB

---

## Understanding Semantic Memory

**Semantic memory** in AI agents refers to the system's ability to understand and retrieve information based on _meaning_ rather than exact words.

In human cognition, semantic memory lets you know:

- "Beach" relates to "ocean", "sand", "coastal"
- "Expensive" contrasts with "budget-friendly"

In AI systems, we achieve this through **vector embeddings** - numerical representations that capture semantic relationships. 

---

## Instructions

### Step 1: Understand the Embedding Process

Open `scripts/seed-cosmosdb/Program.cs` and review how the model is used to generate embeddings for each destination.

### Step 2: Implement Vector Search

Open `src/backend/Tools/DestinationSearchTool.cs` and review the semantic search implementation. This tool takes a user query, generates an embedding, and performs a vector search in Cosmos DB to find the most semantically similar destinations.

### Step 3: Update Agent Configuration

Configure the agent to use the new semantic search tool. 

Open `src/backend/Agents/ContosoTravelAgentBuilder.cs` and update the tools list to include the semantic search tool in the agent configuration.

```csharp
Tools = [
        AIFunctionFactory.Create(
            _destinationSearchTool.SearchDestinations, 
            name: "SearchDestinations",
            description: "Search for travel destinations using semantic understanding")]
```

### Step 4: Update the Prompt Instructions

1. Remove the hardcoded knowledge from the `AgentInstructions` constant in `ContosoTravelAgentBuilder.cs` in `## DESTINATION HIGHLIGHTS:` section.

2. Help the agent understand when to use the search tool by adding a `## TOOL USAGE` section to the `AgentInstructions` constant.

    ```csharp
    ## TOOL USAGE
    - **SearchDestinations**: Use this tool when travelers ask for destination recommendations or mention interests/activities
    (e.g., "beaches", "adventure", "hiking", "wine", "family-friendly", "cultural")
    ```

---

## Test Your Implementation

Refer to the **[Running the Application Locally](00-setup_instructions.md#running-the-application-locally)** section in the Environment Setup guide to start the application.

### Test Scenarios: Semantic Understanding

Try these queries to see how semantic search understands meaning beyond exact keywords.

**Example 1**

```
I'm interested in beaches. Can you suggest any destinations?
```

*Expected:* The agent returns coastal destinations with beaches, oceanfront locations, and seaside activities - understanding the semantic relationship between "beaches" and related concepts.

---

**Example 2** 

```
I'd love somewhere with mountains and hiking. What do you recommend?
```

*Expected:* The agent understands the intent and returns destinations with mountainous terrain, hiking trails, and outdoor adventure activities.

---

## Next Steps

👉 **[Finishing Up](./finishing-up.md)**

---
