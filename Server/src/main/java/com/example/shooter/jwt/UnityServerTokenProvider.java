package com.example.shooter.jwt;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

@Component
public class UnityServerTokenProvider extends JwtAbstractTokenProvider {
    public UnityServerTokenProvider(
            @Value("${unity-server.secret}") String secret,
            @Value("${unity-server.expiration-ms}") long expirationMs) {
        super(secret, expirationMs);
    }
}
