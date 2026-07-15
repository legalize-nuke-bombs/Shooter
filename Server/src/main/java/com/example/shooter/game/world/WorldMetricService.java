package com.example.shooter.game.world;

import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.time.Instant;

@Service
@Slf4j
public class WorldMetricService {

    private final WorldRepository worldRepository;
    private volatile WorldMetricRepresentation cache;

    public WorldMetricService(WorldRepository worldRepository) {
        this.worldRepository = worldRepository;
        this.cache = null;
    }

    public WorldMetricRepresentation get() {
        return cache;
    }

    @Scheduled(fixedDelay = 60 * 1000)
    public void updateCache() {
        log.info("updating world metric cache 📦...");
        Instant now = Instant.now();
        long since24h = now.minus(Duration.ofHours(24)).getEpochSecond();
        long since3d = now.minus(Duration.ofHours(24 * 3)).getEpochSecond();
        cache = new WorldMetricRepresentation(
                worldRepository.count(),
                worldRepository.countCreatedSince(since24h),
                worldRepository.countAccessedSince(since3d)
        );
    }
}
