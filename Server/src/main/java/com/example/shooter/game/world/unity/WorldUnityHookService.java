package com.example.shooter.game.world.unity;

import com.example.shooter.exception.ApiException;
import com.example.shooter.exception.ErrorCode;
import com.example.shooter.jwt.JwtTokenProvider;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.http.client.BufferingClientHttpRequestFactory;
import org.springframework.http.client.SimpleClientHttpRequestFactory;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClient;

@Service
@Slf4j
public class WorldUnityHookService {

    private final JwtTokenProvider jwtTokenProvider;
    private final RestClient restClient;

    public WorldUnityHookService(JwtTokenProvider jwtTokenProvider, @Value("${unity-server.hook-url}") String hookUrl) {
        this.jwtTokenProvider = jwtTokenProvider;
        SimpleClientHttpRequestFactory requestFactory = new SimpleClientHttpRequestFactory();
        requestFactory.setConnectTimeout(1000);
        requestFactory.setReadTimeout(2000);
        this.restClient = RestClient.builder()
                .baseUrl(hookUrl)
                .requestFactory(new BufferingClientHttpRequestFactory(requestFactory))
                .build();
    }

    public void deliver(UnityHook hook) {
        if (!trySend(hook)) {
            throw new ApiException(ErrorCode.GAME_SERVER_UNAVAILABLE);
        }
    }

    public void tryDeliver(UnityHook hook) {
        trySend(hook);
    }

    private boolean trySend(UnityHook hook) {
        try {
            UnityHookResponse resp = restClient.post()
                    .header("Authorization", "Bearer " + jwtTokenProvider.generateToken("hook"))
                    .contentType(MediaType.APPLICATION_JSON)
                    .body(hook)
                    .retrieve()
                    .body(UnityHookResponse.class);
            if (resp == null || !resp.isAccepted()) {
                throw new IllegalStateException("unity server did not ack the hook");
            }
            log.info("delivered unity hook {} user {} world {}", hook.getAction(), hook.getUserId(), hook.getWorldId());
            return true;
        }
        catch (Exception e) {
            log.error("delivery of unity hook {} user {} world {} failed: {}", hook.getAction(), hook.getUserId(), hook.getWorldId(), e.getMessage());
            return false;
        }
    }
}
