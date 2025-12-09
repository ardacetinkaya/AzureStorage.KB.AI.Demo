# Fellow

Welcome to **Fellow**! 🚀

This is a learning playground designed to explore how to build AI-enhanced applications using the .NET platform and Azure services. The goal is to understand how to integrate custom data into AI chat experiences, making them smarter and more context-aware.

The project is mainly a chat application that can "talk" about your documents which are uploaded to Azure Blob Storage.


## 🎯 Project Goal

The main objective of this repository is to learn and experiment with:
- **.NET APIs** for AI development.
- **Azure Search** for indexing and retrieving custom data with AI.
  - Using Azure Blob Storage as **Knowledge sources**
  - Using **Knowledge Base** to answer questions
- Building a **Chat Interface** that can "talk" to your documents.
  - Do simple short-circuiting of LLM calls to answer questions directly from MCP tooling

It's a "work in progress" because learning is a never-ending journey! 🌟

## 🚀 Getting Started

- Some Azure resources are required to run this project. 
  - Azure Search
  - Azure Blob Storage
  - Azure Foundry for LLM services
- **dotnet** AI Chat Web App template is customized for this project.

## 📂 What's Inside?

The project is split into two main parts:

- **`Fellow.Services`**: The backend brain. It handles data ingestion, reading documents (like PDFs), and managing the "knowledge" that the AI uses.
- **`Fellow.Web.UI`**: The frontend interface. A web-based chat application where you can interact with the AI.

## 🛠️ Technologies Used

- **.NET**
- **Azure AI Search**
- **Blazor** (Web UI)

