# 🤖 Multi-LLM AI Application

A flexible, provider-agnostic .NET 10 application that supports multiple Large Language Models (LLMs) through a unified interface using Microsoft.Extensions.AI.

## ✨ Features

- 🔄 **Provider-Agnostic**: Switch between LLM providers without code changes
- 🚀 **Multiple Providers**: Support for OpenAI, Azure OpenAI, Groq (extensible for Gemini, HuggingFace)
- 🎯 **Unified Interface**: Single `IChatClient` interface for all providers
- 🔐 **Secure by Design**: API keys never committed to Git
- 📦 **Easy Configuration**: Hierarchical config loading with fallbacks
- 🌍 **Environment Ready**: Support for environment variable overrides
- 📝 **Extensible**: Simple pattern to add new providers

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- API keys for your chosen LLM provider(s)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/abilgaiyan/myAIApp.git

cd myAIApp
