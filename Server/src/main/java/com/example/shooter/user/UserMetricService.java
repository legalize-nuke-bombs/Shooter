package com.example.shooter.user;

import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.time.Instant;

@Service
@Slf4j
public class UserMetricService {

    private final UserRepository userRepository;
    private volatile UserMetricRepresentation cache;

    public UserMetricService(UserRepository userRepository) {
        this.userRepository = userRepository;
        this.cache = null;
    }

    public UserMetricRepresentation get() {
        return cache;
    }

    @Scheduled(fixedDelay = 60 * 1000)
    public void updateCache() {
        log.info("updating user metric cache 📦...");
        long since24h = Instant.now().minus(Duration.ofHours(24)).getEpochSecond();
        cache = new UserMetricRepresentation(
                userRepository.countAll(),
                userRepository.countSince(since24h)
        );
    }

}
