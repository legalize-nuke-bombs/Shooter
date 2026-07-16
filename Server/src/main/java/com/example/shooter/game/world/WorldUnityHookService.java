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

    private final Queue<UnityHook> tasks = new ConcurrentLinkedQueue<>();
    private final UnityServerTokenProvider unityServerTokenProvider;
    private final RestClient restClient;

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
            log.info("delivered {} unity hooks", batch.size());
        }
        catch (Exception e) {
            tasks.addAll(batch);
            log.warn("unity hook delivery failed, {} tasks requested: {}", batch.size(), e.getMessage());
        }
    }
}
