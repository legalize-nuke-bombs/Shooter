package com.example.shooter.game.world;

import com.example.shooter.jwt.UnityServerTokenProvider;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClient;

import java.util.ArrayList;
import java.util.List;
import java.util.Queue;
import java.util.concurrent.ConcurrentLinkedQueue;

@Service
@Slf4j
public class WorldUnityHookService {

    private static final int MAX_BATCH_SIZE = 100;
    private static final long MIN_BACKOFF_MS = 200;
    private static final long MAX_BACKOFF_MS = 5000;

    private final Queue<UnityHook> tasks = new ConcurrentLinkedQueue<>();
    private final UnityServerTokenProvider unityServerTokenProvider;
    private final RestClient restClient;

    private long backoffMs;
    private long backoffUntil;

    public WorldUnityHookService(
            UnityServerTokenProvider unityServerTokenProvider,
            @Value("${unity-server.hook-url}") String hookUrl) {
        this.unityServerTokenProvider = unityServerTokenProvider;
        this.restClient = RestClient.builder().baseUrl(hookUrl).build();
    }

    public void registerTask(UnityHook task) {
        tasks.add(task);
    }

    @Scheduled(fixedDelay = 100)
    public void drain() {
        if (tasks.isEmpty() || System.currentTimeMillis() < backoffUntil) return;

        List<UnityHook> batch = new ArrayList<>();
        UnityHook task;
        while (batch.size() < MAX_BATCH_SIZE && (task = tasks.poll()) != null) {
            batch.add(task);
        }

        try {
            restClient.post()
                    .header("Authorization", "Bearer " + unityServerTokenProvider.generateToken("hook"))
                    .contentType(MediaType.APPLICATION_JSON)
                    .body(new UnityHookBatch(batch))
                    .retrieve()
                    .toBodilessEntity();
            backoffMs = 0;
            log.info("delivered {} unity hooks", batch.size());
        }
        catch (Exception e) {
            tasks.addAll(batch);
            backoffMs = Math.min(Math.max(backoffMs * 2, MIN_BACKOFF_MS), MAX_BACKOFF_MS);
            backoffUntil = System.currentTimeMillis() + backoffMs;
            log.warn("unity hook delivery failed, {} tasks requeued, retry in {} ms: {}", batch.size(), backoffMs, e.getMessage());
        }
    }
}
