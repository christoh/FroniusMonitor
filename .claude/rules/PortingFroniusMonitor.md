---
paths:
  - FroniusMonitor/**
  - HomeAutomationClient/**
  - HomeAutomationServer/**
---

# Porting FroniusMonitor.csproj

This applies to anything that covers porting FroniusMonitor.csproj to the new Avalonia architecture.

## Goals
- We want to port FroniusMonitor.csproj to a new project.
- The architecture is quite different. FroniusMonitor.csproj is a WPF application directly accessing the devices. The new architecture is a client server application consisting of HomeAutomationServer.csproj that communicates with the home devices and HomeAutomationClient.csproj that runs on the client's PC, phone or browser. It is an Avalonia cross platform application. So HomeAutomationClient.csproj has "surrounding" projects HomeAutomationClient.*.csproj. This is the standard Avalonia project structure.
- The HomeAutomationClient.csproj is the main project that contains the UI and the logic to communicate with the server. The HomeAutomationServer.csproj is the main project that contains the logic to communicate with the devices and provide data to the clients.
- Since it includes the Web platform (WebAssembly), we have several constraints like async/await is mapped to cooperative multitasking. You cannot use blocking calls like Thread.Sleep() or Task.Wait() in the UI thread. You have to use async/await and Task.Delay() instead. Also, the application must be single window. You cannot have multiple windows like in WPF.