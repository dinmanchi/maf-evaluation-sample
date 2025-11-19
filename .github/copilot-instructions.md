# GitHub Copilot Instructions - MAF Evaluation Sample

This repository contains modular GitHub Copilot instructions to assist with developing Microsoft Agent Framework (MAF) applications in .NET with Aspire.

## 📋 Instruction Files

### Core Project Files
1. **[00-project-overview.md](copilot-instructions/00-project-overview.md)**
   - Repository purpose and goals
   - Current project structure
   - Future expansion plans
   - Key technologies overview

2. **[01-maf-conventions.md](copilot-instructions/01-maf-conventions.md)**
   - Microsoft Agent Framework patterns
   - AsIChatClient() bridge usage
   - Tool creation with AIFunctionFactory
   - Data capture with streaming API
   - Configuration and error handling

3. **[02-aspire-conventions.md](copilot-instructions/02-aspire-conventions.md)**
   - .NET Aspire project structure
   - AppHost orchestration patterns
   - ServiceDefaults integration
   - Dashboard usage and monitoring
   - Best practices for cloud-native apps

4. **[03-evaluation-patterns.md](copilot-instructions/03-evaluation-patterns.md)**
   - LLM-as-Judge evaluation methodology
   - Standard evaluation metrics (4 metrics)
   - Structured prompting for evaluations
   - Validation and testing strategies
   - Output formats and best practices

### Future Expansion Files
5. **[04-future-api-projects.md](copilot-instructions/04-future-api-projects.md)**
   - Agentic API patterns (agent-per-endpoint, singleton, orchestration)
   - RESTful and gRPC agent endpoints
   - API configuration with Aspire
   - Evaluation integration in APIs
   - Best practices for production APIs

6. **[05-future-ui-projects.md](copilot-instructions/05-future-ui-projects.md)**
   - Blazor/React UI patterns
   - Agent chat components
   - Streaming response handling
   - Evaluation display components
   - Real-time agent interaction

7. **[06-future-data-layer.md](copilot-instructions/06-future-data-layer.md)**
   - Agent-powered repository pattern
   - Query generation from natural language
   - Data validation agents
   - Agentic RAG (Retrieval-Augmented Generation)
   - Transaction management and safety

## 🎯 When to Use Each File

| Scenario | Relevant Files |
|----------|---------------|
| Creating a new agent | 01-maf-conventions.md |
| Adding evaluation to existing agent | 03-evaluation-patterns.md |
| Setting up Aspire projects | 02-aspire-conventions.md |
| Building RESTful API with agents | 04-future-api-projects.md |
| Creating agent chat UI | 05-future-ui-projects.md |
| Implementing data access with agents | 06-future-data-layer.md |
| Understanding project goals | 00-project-overview.md |

## 🚀 Quick Start for Copilot

When working on this repository, GitHub Copilot will use these instructions to:

1. **Understand the project context** - MAF evaluation sample with Aspire
2. **Follow established patterns** - AsIChatClient() bridge, tool creation, streaming
3. **Apply evaluation best practices** - LLM-as-judge methodology
4. **Plan future features** - API, UI, and data layer guidance
5. **Maintain consistency** - Naming conventions, error handling, testing

## 🔧 Instruction Organization

- **Numbered files (00-06)** for ordered reading
- **Modular approach** - each file is self-contained
- **Code samples** - practical examples in every file
- **Best practices** - production-ready guidance
- **Future-proofing** - plans for repository expansion

## 📦 Technology Stack

- **.NET 10.0** - Target framework
- **Microsoft Agent Framework (MAF)** - Agentic AI
- **.NET Aspire 13.0.0** - Cloud-native orchestration
- **Azure OpenAI** - LLM capabilities
- **OpenTelemetry** - Distributed tracing

## 📖 Additional Resources

- [README.md](../README.md) - Getting started guide
- [IMPLEMENTATION_GUIDE.md](../docs/IMPLEMENTATION_GUIDE.md) - Detailed implementation
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Contribution guidelines
- [LESSONS_LEARNED.md](../docs/LESSONS_LEARNED.md) - Technical insights

## 🎓 Learning Path

1. Start with **00-project-overview.md** to understand the goals
2. Read **01-maf-conventions.md** for agent basics
3. Review **02-aspire-conventions.md** for Aspire patterns
4. Study **03-evaluation-patterns.md** for quality assurance
5. Explore future files (04-06) for expansion ideas

---

**Note**: These instructions are designed to be used by GitHub Copilot to provide context-aware suggestions. They evolve with the repository as new patterns and practices are established.
