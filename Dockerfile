# syntax=docker/dockerfile:1

# Pinned to linux/amd64 (Render's runtime architecture) on every stage so the
# build is deterministic regardless of the host machine building it — without
# this, a build run on an Apple Silicon machine defaults to arm64 for the base
# images while the migration bundle below is explicitly built for linux-x64,
# producing a broken image (verified: the bundle fails to run under an arm64
# final image with "failed to open elf at /lib64/ld-linux-x86-64.so.2").
ARG TARGET_PLATFORM=linux/amd64

# ── build: restore + publish the API ────────────────────────────────────────
FROM --platform=$TARGET_PLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so restore is cached across builds that only change source.
COPY src/TwoGather.Domain/TwoGather.Domain.csproj src/TwoGather.Domain/
COPY src/TwoGather.Application/TwoGather.Application.csproj src/TwoGather.Application/
COPY src/TwoGather.Infrastructure/TwoGather.Infrastructure.csproj src/TwoGather.Infrastructure/
COPY src/TwoGather.Api/TwoGather.Api.csproj src/TwoGather.Api/
RUN dotnet restore src/TwoGather.Api/TwoGather.Api.csproj

COPY src/ src/
RUN dotnet publish src/TwoGather.Api/TwoGather.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── bundle: self-contained EF Core migration bundle ─────────────────────────
# A migration bundle is a standalone native executable (it embeds its own
# runtime), so — unlike the `dotnet ef` CLI — it can run without the SDK or
# project source present. See DEPLOYMENT.md for how/where to run it; it is
# never invoked automatically by the final image's entrypoint.
FROM build AS bundle
RUN dotnet tool install --global dotnet-ef --version 10.0.5
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project src/TwoGather.Infrastructure/TwoGather.Infrastructure.csproj \
    --startup-project src/TwoGather.Api/TwoGather.Api.csproj \
    --configuration Release \
    --self-contained \
    -r linux-x64 \
    -o /app/bundle/efbundle

# ── final: runtime-only image ────────────────────────────────────────────────
FROM --platform=$TARGET_PLATFORM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
COPY --from=bundle /app/bundle/efbundle .

# Render's Docker runtime detects the container's listening port itself; the
# app binds to whatever $PORT Render injects at runtime (see Program.cs). This
# EXPOSE is only a fallback for `docker run` without Render (e.g. local testing)
# — it is not the actual production port.
EXPOSE 8080

ENTRYPOINT ["dotnet", "TwoGather.Api.dll"]
