<p align="center">
  <img width="50%" src="https://github.com/KumaGameEngine/KumaEngine/blob/master/assets/logo.png?raw=true" />
</p>

<p align="center">The Kuma game engine is a bare bones lua game engine written in c#</p>

<p align="center">
    <img alt="GitHub forks" src="https://img.shields.io/github/forks/KumaGameEngine/KumaEngine?style=for-the-badge">
    <img alt="GitHub Issues or Pull Requests" src="https://img.shields.io/github/issues-closed-raw/KumaGameEngine/KumaEngine?style=for-the-badge">
    <img alt="GitHub Issues or Pull Requests" src="https://img.shields.io/github/issues-pr-closed-raw/KumaGameEngine/KumaEngine?style=for-the-badge">
</p>

# Why?
Most game engines today contain a huge amount of bloat. 
Bloat that most of the times ends up slowing games down.

Kuma only provides you with the essentials, letting you implement what you need.

# How do i use Kuma?
kuma provides a `CLI` that can be used to run a debug player and create projects.

## building the cli
clone the repo, then run

```bash
cd RocketCLI
dotnet publish
```

the kumaCLI executable is in `./RocketCLI/bin/Release/net9.0`.

## AI Stament
The Kuma game engine is fully made by humans, and i would like for it to stay that way. All pull requets containing LLM generated code will be rejected.

> [!WARNING]
> the game engine is currently in a primitive state, it is usable but a lot of features are missing. and the advertized performance boosts are not there.